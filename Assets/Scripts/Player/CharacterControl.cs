using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterControl : MonoBehaviour
{
    private Transform playerTransform;
    private Transform cameraTransform;
    Animator animator;
    CharacterController characterController;
    #region 角色姿态和运动状态
    private enum PlayerMovement
    {
        idle,
        walk,
        run
    };
    private enum PlayerPosture
    {
        stand,
        crouch,
        jumping,
        upStair,
        upHill
    };
    PlayerMovement playerMovement = PlayerMovement.idle;
    PlayerPosture playerPosture = PlayerPosture.stand;
    #endregion
    //角色动画状态的阈值
    float standThreshold = 0f;
    float crouchThreshold = 1f;
    float jumpThreshold = 2f;
    //状态机参数的哈希值
    int postureHash;
    int moveSpeedHash;
    int turnSpeedHash;
    int verticalVelHash;
    // 移动输入值
    Vector2 moveInput;
    #region 玩家状态标志
    bool isRunning;
    bool isCrouch;
    bool isJumping;
    bool canFall;
    bool isGrounded;
    bool isUpstair;
    bool isUpHill;
    #endregion
    //高度差
    float heightDiff;
    //玩家面向
    Vector3 playerInput = Vector3.zero;
    #region 各类速度定义
    //垂直方向上的速度
    float verticalSpeed;
    //移动速度
    private float walkSpeed = 3.0f;
    private float runSpeed = 10.0f;
    private float crouchSpeed = 3f;
    //重力
    float gravity = -9.8f;
    // 速度缓存池定义
    static readonly int CACHE_SIZE = 5;
    Vector3[] velCache = new Vector3[CACHE_SIZE];
    int currentChacheIndex = 0;
    Vector3 averageVel = Vector3.zero;
    //加速度的倍数
    float fallMultiple = 1.5f;
    float jumpMultiple = 8f;
    #endregion
#if UNITY_ANDROID || UNITY_IOS
    private VariableJoystick joystick;
#endif
    void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        joystick = FindObjectOfType<VariableJoystick>();
#endif

        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        playerTransform = transform;
        cameraTransform = Camera.main.transform;
        postureHash = Animator.StringToHash("Motion");
        moveSpeedHash = Animator.StringToHash("MoveSpeed");
        turnSpeedHash = Animator.StringToHash("TurnSpeed");
        verticalVelHash = Animator.StringToHash("JumpSpeed");
        // 获取 stepOffset 的数值
        // stepOffsetValue = characterController.stepOffset;
    }
    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        joystik();
#endif
        CheckGround();
        SwitchPlayerStates();
        CaculateGravity();
        Jump();
        CaculateInputDirection();
        SetUpAnimator();
        CheckDownstair();
        CheckUpstair();
        ChangeCollider();
    }
#if UNITY_ANDROID || UNITY_IOS
    private void joystik()
    {
        if (joystick != null)
        {
            moveInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
    }
    public void JumpButton()
    {
        isJumping = true;
    }
    public void RunButton()
    {
        isRunning = !isRunning;
    }
    public void CrouchButton()
    {
        isCrouch = !isCrouch;
    }
#else
    //获取键盘输入
    public void MoveInput(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
    public void RunInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            isRunning = !isRunning;
    }
    public void CrouchInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            isCrouch = !isCrouch;
    }
    public void JumpInput(InputAction.CallbackContext ctx)
    {
        isJumping = ctx.ReadValueAsButton();
        //  Debug.Log("isJumping: " + isJumping);//测试输出
    }
#endif
    void SwitchPlayerStates()
    {
        if (verticalSpeed > 1f || !isGrounded && canFall)//解决下楼梯/斜坡触发跳跃动画的问题//上楼梯不可跳跃
        {
            playerPosture = PlayerPosture.jumping;
        }
        else if (isUpstair)
        {
            playerPosture = PlayerPosture.upStair;
        }
        else if (isUpHill)
        {
            playerPosture = PlayerPosture.upHill;
        }

        else if (isCrouch)
        {
            playerPosture = PlayerPosture.crouch;
        }
        else
        {
            playerPosture = PlayerPosture.stand;
        }

        if (moveInput.magnitude == 0)//输入向量的模为零，即没有任何输入
        {
            playerMovement = PlayerMovement.idle;
        }
        else if (isRunning)
        {
            playerMovement = PlayerMovement.run;
        }
        else
        {
            playerMovement = PlayerMovement.walk;
        }
        //Debug.Log(verticalSpeed);
        //Debug.Log(playerPosture);
        // Debug.Log(playerMovement);
        // Debug.Log(playerMovement);
    }
    //射线检测与下方碰撞体的高度差
    void CheckDownstair()
    {
        // 从角色的当前位置向下发射射线
        RaycastHit hit;
        float rayLength = 0.5f;
        //Debug.DrawRay(transform.position, Vector3.down * rayLength, Color.red);
        canFall = !Physics.Raycast(transform.position, Vector3.down, out hit, rayLength);
        // Debug.Log(canFall);
    }
    void CheckUpstair()
    {
        RaycastHit hit1;
        RaycastHit hit2;
        Vector3 rayStart1 = playerTransform.position + Vector3.up * 0.1f;
        Vector3 rayStart2 = playerTransform.position + Vector3.up * 0.3f + transform.forward * 0.35f;
        float rayLength1 = characterController.radius + .2f;
        float rayLength2 = 0.2f;
        Debug.DrawRay(rayStart1, transform.forward * rayLength1, Color.red);
        Debug.DrawRay(rayStart2, Vector3.down * rayLength2, Color.red);
        //transform.forward角色的面向，使用可使得射线和角色面向平行
        if (Physics.Raycast(rayStart1, transform.forward, out hit1, rayLength1) && Physics.Raycast(rayStart2, Vector3.down, out hit2, rayLength2))
        {
            float playerFootY = playerTransform.position.y;
            float stairTopY = hit2.point.y;
            heightDiff = stairTopY - playerFootY;
            Vector3 hitNormal = hit1.normal;//获取射线与碰撞体交点的法线向量，判断是爬楼梯还是爬坡（爬坡就不需要向上的速度进行平滑操作）
            // 与 Vector3.up 的夹角
            float angle = Vector3.Angle(hitNormal, Vector3.up);//计算夹角

            if (angle < 45f && angle > 10f)//45f表示45°
            {
                isUpHill = true;
            }
            else
            {
                isUpstair = true;
            }
        }
        else
        {
            isUpstair = false;
            isUpHill = false;
        }
    }
    void CheckGround()//地面检测
    {
        Vector3 rayStart = playerTransform.position + Vector3.up * 0.1f;
        // 射线方向朝下
        Vector3 rayDirection = Vector3.down;
        // 射线长度
        float rayLength = 0.11f;
        // 显示射线
        //  Debug.DrawRay(rayStart, rayDirection * rayLength, Color.red);
        // 使用射线检测地面
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayLength))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
        // differenceHeight = hit.point.y;
    }
    void CaculateGravity()//计算重力
    {
        if (playerPosture == PlayerPosture.upStair && playerMovement != PlayerMovement.idle)
        {
            verticalSpeed = 1f;
            //速度不影响爬单个台阶的最大高度只影响爬上单个台阶的时间
            //可以爬的台阶的最大高度为min(max(地面检测射线的长度-起始点,canFall的长度）,双射线检测的射线2起点减去射线1起点)
        }
        else if (!isGrounded)
        {
            verticalSpeed += gravity * fallMultiple * Time.deltaTime;
        }
        else
        {
            //  verticalSpeed = 0f;//竖直方向上的速度设置为0会导致角色下完楼梯，角色悬空问题
            verticalSpeed = -2;

        }
    }

    //跳跃处理逻辑
    void Jump()
    {
        if (isJumping && isGrounded)
        {
            verticalSpeed = Mathf.Sqrt(-jumpMultiple * gravity);
            isJumping = false;//防止连续跳跃
        }

    }
    //玩家面向相对于相机的方向
    void CaculateInputDirection()
    {
        Vector3 camForwardProjection = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        playerInput = camForwardProjection * moveInput.y + cameraTransform.right * moveInput.x;
        playerInput = playerTransform.InverseTransformVector(playerInput);
    }
    //动态调整collider
    void ChangeCollider()
    {
        if (isCrouch)
        {
            characterController.center = new Vector3(0.04f, 0.493f, 0.04f);
            characterController.height = 1.03f;
            characterController.radius = 0.35f;

        }
        else
        {
            characterController.center = new Vector3(0.0f, 0.925f, 0.0f);
            characterController.height = 1.7f;
            characterController.radius = 0.15f;
        }
        //else if (playerPosture == PlayerPosture.upStair)
        //{
        //    characterController.center = new Vector3(0.0f, 0.92f, 0.0f);
        //    characterController.height = 1.55f;
        //    characterController.radius = 0.15f;
        //}

    }
    void OnAnimatorIK(int layerIndex)
    {
        if (playerPosture == PlayerPosture.upStair || playerPosture == PlayerPosture.upHill)
        {
            // 启用左脚和右脚的 IK
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f); // 权重为 1，表示完全控制
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
            // 获取左右脚位置
            Vector3 leftFootPosition = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 rightFootPosition = animator.GetIKPosition(AvatarIKGoal.RightFoot);
            float diff = heightDiff * 1.7f;
            float stairHeight = playerTransform.position.y + diff;//脚抬起

            float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
            if (moveInput.magnitude == 0)
            {
                rightFootPosition += transform.forward * diff;//脚向前伸展的长度
                rightFootPosition.y = stairHeight;
            }
            else if (normalizedTime < 0.5f)
            {
                leftFootPosition.y = stairHeight;
            }
            else  // 在动画的后半段，右脚抬高
            {
                rightFootPosition.y = stairHeight;

            }
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPosition);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPosition);
        }

    }
    void SetUpAnimator()
    {
        switch (playerPosture)
        {
            case PlayerPosture.stand:
                animator.SetFloat(postureHash, standThreshold, 0.3f, Time.deltaTime);
                switch (playerMovement)
                {
                    case PlayerMovement.idle:
                        animator.SetFloat(moveSpeedHash, 0.0f, 0.1f, Time.deltaTime);
                        break;
                    case PlayerMovement.walk:
                        animator.SetFloat(moveSpeedHash, playerInput.magnitude * walkSpeed, 0.1f, Time.deltaTime);
                        break;
                    case PlayerMovement.run:
                        animator.SetFloat(moveSpeedHash, playerInput.magnitude * runSpeed, 0.1f, Time.deltaTime);
                        break;
                }
                break;
            case PlayerPosture.crouch:
                animator.SetFloat(postureHash, crouchThreshold, 0.1f, Time.deltaTime);
                switch (playerMovement)
                {
                    case PlayerMovement.idle:
                        animator.SetFloat(moveSpeedHash, 0.0f, 0.1f, Time.deltaTime);
                        break;
                    case PlayerMovement.walk:
                        animator.SetFloat(moveSpeedHash, playerInput.magnitude * crouchSpeed, 0.1f, Time.deltaTime);
                        break;
                }
                break;
            case PlayerPosture.jumping:
                animator.SetFloat(postureHash, jumpThreshold, 0.3f, Time.deltaTime);
                animator.SetFloat(verticalVelHash, verticalSpeed, 0.1f, Time.deltaTime);
                break;
            case PlayerPosture.upStair://不设置upStair，角色则会自动爬坡（因为upStair中没有animator.SetFloat进行动画或者动画的parameter切换，所以导致一直播放爬坡前的动画）
                animator.SetFloat(postureHash, standThreshold, 0.3f, Time.deltaTime);
                switch (playerMovement)
                {
                    case PlayerMovement.idle:
                        animator.SetFloat(moveSpeedHash, 0.0f, 0.1f, Time.deltaTime);
                        break;
                    case PlayerMovement.walk:
                        animator.SetFloat(moveSpeedHash, playerInput.magnitude * walkSpeed, 0.1f, Time.deltaTime);
                        break;
                    case PlayerMovement.run:
                        animator.SetFloat(moveSpeedHash, playerInput.magnitude * runSpeed, 0.1f, Time.deltaTime);
                        break;
                }
                break;
            case PlayerPosture.upHill:
                animator.SetFloat(postureHash, standThreshold, 0.3f, Time.deltaTime);
                switch (playerMovement)
                {
                    case PlayerMovement.idle:
                        animator.SetFloat(moveSpeedHash, 0.0f, 0.1f, Time.deltaTime);
                        break;
                    case PlayerMovement.walk:
                        animator.SetFloat(moveSpeedHash, playerInput.magnitude * walkSpeed, 0.1f, Time.deltaTime);
                        break;
                    case PlayerMovement.run:
                        animator.SetFloat(moveSpeedHash, playerInput.magnitude * runSpeed, 0.1f, Time.deltaTime);
                        break;
                }
                break;
        }
        if (playerPosture != PlayerPosture.jumping)
        {
            float rad = Mathf.Atan2(playerInput.x, playerInput.z);
            animator.SetFloat(turnSpeedHash, rad, 0.1f, Time.deltaTime);
            playerTransform.Rotate(0, rad * 200 * Time.deltaTime, 0f);//控制转向速度
        }

    }
    Vector3 AverageVel(Vector3 newVel)
    {
        velCache[currentChacheIndex] = newVel;
        currentChacheIndex++;
        currentChacheIndex %= CACHE_SIZE;
        Vector3 average = Vector3.zero;
        foreach (Vector3 vel in velCache)
        {
            average += vel;
        }
        return average / CACHE_SIZE;
    }
    private void OnAnimatorMove()
    {
        if (characterController.enabled)
        {
            if (playerPosture != PlayerPosture.jumping)
            {
                Vector3 playerDeltaMovement = animator.deltaPosition;
                playerDeltaMovement.y = verticalSpeed * Time.deltaTime;
                characterController.Move(playerDeltaMovement);
                averageVel = AverageVel(animator.velocity);//计算前5帧平均速度
            }
            else
            {
                averageVel.y = verticalSpeed;
                Vector3 playerDeltaMovement = averageVel * Time.deltaTime;
                characterController.Move(playerDeltaMovement);
            }
        }
    }
}