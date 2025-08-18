using UnityEngine;

using UnityEngine.Playables;

using UnityEngine.Animations;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Animator))]

public class PlayableCharacterControl : MonoBehaviour
{
    #region playable
    //public AnimationClip standClip01;
    //public AnimationClip standClip02;
    //public AnimationClip standClip03;
    //public AnimationClip crouchClip01;
    //public AnimationClip crouchClip02;
    //public AnimationClip jumpClip;
    // [Range(0f, 1f)]
    private float weight;
    private float standWeight;
    private float crouchWeight;
    public PlayableGraph playableGraph;

    AnimationMixerPlayable standPlayable;
    AnimationMixerPlayable crouchPlayable;
    AnimationMixerPlayable jumpPlayable;
    AnimationLayerMixerPlayable layerMixerPlayable;
    float changeSpeed = 5f;
    #endregion
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
        upHill,
        Fight
    };
    PlayerMovement playerMovement = PlayerMovement.idle;
    PlayerPosture playerPosture = PlayerPosture.stand;
    #endregion
    // 移动输入值
    Vector2 moveInput;
    #region 玩家状态判断
    bool isRunning;
    bool isCrouch;
    bool isJumping;
    bool canFall;
    bool isGrounded;
    bool isUpstair;
    bool isUpHill;
    bool isSwitch;
    bool isFight;
    #endregion
    //玩家面向
    Vector3 playerInput = Vector3.zero;
    #region 各类速度定义
    //垂直方向上的速度
    float verticalSpeed;
    //移动速度
    private float walkSpeed = 1f;
    private float runSpeed = 1f;
    private float crouchSpeed = 1f;
    //加速度的倍数
    private float fallMultiple = 1.5f;
    private float jumpMultiple = 8f;
    //玩家转向速度
    private float rotateSpeed = 1000f;
    //重力
    float gravity = -9.8f;
    // 速度缓存池定义
    static readonly int CACHE_SIZE = 5;
    Vector3[] velCache = new Vector3[CACHE_SIZE];
    int currentChacheIndex = 0;
    Vector3 averageVel = Vector3.zero;
    #endregion
    private float jumpDelay = 0.01f;  // 设置跳跃延迟时间（单位：秒）

    public Transform playerRightHandBone;//手部位置
    Fight fight = new Fight();
    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        playerTransform = transform;
        cameraTransform = Camera.main.transform;
        init();
    }
    private void init()
    {
        // 创建该图和混合器，然后将它们绑定到 Animator。
        playableGraph = PlayableGraph.Create();
        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", GetComponent<Animator>());
        layerMixerPlayable = AnimationLayerMixerPlayable.Create(playableGraph, 4);
        playableOutput.SetSourcePlayable(layerMixerPlayable);
        standPlayable = AnimationMixerPlayable.Create(playableGraph, 3);
        // 从 Resources 文件夹加载动画
        var standClip01 = Resources.Load<AnimationClip>("Animator/Animations/Idle");
        var standClip02 = Resources.Load<AnimationClip>("Animator/Animations/Walk");
        var standClip03 = Resources.Load<AnimationClip>("Animator/Animations/Run");
        var crouchClip01 = Resources.Load<AnimationClip>("Animator/Animations/CrouchIdle");
        var crouchClip02 = Resources.Load<AnimationClip>("Animator/Animations/CrouchWalk");
        var jumpClip = Resources.Load<AnimationClip>("Animator/Animations/Jump");


        //使用Addressables在运行有延迟，角色先僵持之后才进入动画（具体原因未知

        //var standClip01 = await Addressables.LoadAssetAsync<AnimationClip>("Animation/Idle").Task;
        //var standClip02 = await Addressables.LoadAssetAsync<AnimationClip>("Animation/Walking").Task;
        //var standClip03 = await Addressables.LoadAssetAsync<AnimationClip>("Animation/HumanoidRun").Task;
        //var crouchClip01 = await Addressables.LoadAssetAsync<AnimationClip>("Animation/Male Crouch Pose").Task;
        //var crouchClip02 = await Addressables.LoadAssetAsync<AnimationClip>("Animation/Crouched Walking").Task;
        //var jumpClip = await Addressables.LoadAssetAsync<AnimationClip>("Animation/jump").Task;
        // 创建 AnimationClipPlayable 并将它们连接到混合器。
        var standClip001 = AnimationClipPlayable.Create(playableGraph, standClip01);

        var standClip002 = AnimationClipPlayable.Create(playableGraph, standClip02);
        //   clip02Playable.SetSpeed(30f);
        var standClip003 = AnimationClipPlayable.Create(playableGraph, standClip03);
        standClip002.SetSpeed(walkSpeed);
        standClip003.SetSpeed(runSpeed);
        playableGraph.Connect(standClip001, 0, standPlayable, 0);
        playableGraph.Connect(standClip002, 0, standPlayable, 1);
        playableGraph.Connect(standClip003, 0, standPlayable, 2);
        crouchPlayable = AnimationMixerPlayable.Create(playableGraph, 2);
        var crouchClip001 = AnimationClipPlayable.Create(playableGraph, crouchClip01);
        var crouchClip002 = AnimationClipPlayable.Create(playableGraph, crouchClip02);
        crouchClip002.SetSpeed(crouchSpeed);
        playableGraph.Connect(crouchClip001, 0, crouchPlayable, 0);
        playableGraph.Connect(crouchClip002, 0, crouchPlayable, 1);
        jumpPlayable = AnimationMixerPlayable.Create(playableGraph, 1);
        var jumpClip1 = AnimationClipPlayable.Create(playableGraph, jumpClip);
        jumpClip1.SetSpeed(0.8f);
        playableGraph.Connect(jumpClip1, 0, jumpPlayable, 0);
        playableGraph.Connect(standPlayable, 0, layerMixerPlayable, 0);
        playableGraph.Connect(crouchPlayable, 0, layerMixerPlayable, 1);
        playableGraph.Connect(jumpPlayable, 0, layerMixerPlayable, 2);
        //播放图。
        playableGraph.Play();
    }
    void Update()
    {
        CheckGround();
        SwitchPlayerStates();
        CaculateGravity();
        Jump();
        CaculateInputDirection();
        SetPoseture();
        CheckDownstair();
        CheckUpstair();
        ChangeCollider();
        SwitchState();

    }
    public void MoveInput(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
    public void RunInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            isRunning = !isRunning;
        }
    }
    public void CrouchInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            isCrouch = !isCrouch;
        }
    }
    public void JumpInput(InputAction.CallbackContext ctx)
    {
        isJumping = ctx.ReadValueAsButton();
    }
    public void SwitchInput(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            isSwitch = !isSwitch;
        }
    }
    public void FightInput(InputAction.CallbackContext ctx)
    {
        isFight = ctx.ReadValueAsButton();
    }
    void SwitchPlayerStates()
    {
        if (verticalSpeed > 1f || !isGrounded && canFall)//解决下楼梯/斜坡触发跳跃动画的问题
        {
            playerPosture = PlayerPosture.jumping;
        }
        else if (layerMixerPlayable.GetInput(3).IsValid() && isFight)
        {
            playerPosture = PlayerPosture.Fight;
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
        if (moveInput.magnitude == 0)
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

    }
    //射线检测与下方碰撞体的高度差
    void CheckDownstair()
    {
        RaycastHit hit;
        canFall = !Physics.Raycast(transform.position, Vector3.down, out hit, 0.4f);
    }
    void CheckUpstair()
    {
        RaycastHit hit1;
        RaycastHit hit2;
        Vector3 rayStart1 = playerTransform.position + Vector3.up * 0.02f - transform.forward * 0.1f;
        Vector3 rayStart2 = playerTransform.position + Vector3.up * 0.4f - transform.forward * 0.1f;
        float rayLength1 = characterController.radius + .4f;
        float rayLength2 = characterController.radius + .3f;
#if UNITY_EDITOR
        Debug.DrawRay(rayStart1, transform.forward * rayLength1, Color.red);
        Debug.DrawRay(rayStart2, transform.forward * rayLength2, Color.red);
#endif
        //transform.forward角色的面向，使用可使得射线和角色面向平行
        if (Physics.Raycast(rayStart1, transform.forward, out hit1, rayLength1) && !Physics.Raycast(rayStart2, transform.forward, out hit2, rayLength2))
        {
            Vector3 hitNormal = hit1.normal;//获取射线与碰撞体交点的法线向量
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
        Vector3 rayDirection = Vector3.down;
        float rayLength = 0.12f;
#if UNITY_EDITOR
        Debug.DrawRay(rayStart, rayDirection * rayLength, Color.red);
#endif
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, rayLength))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
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
            verticalSpeed = -3;
        }
    }
    //跳跃处理逻辑
    void Jump()
    {
        if (isJumping && isGrounded)
        {
            StartCoroutine(DelayedJump());
            isJumping = false;//防止连续跳跃
        }
    }
    IEnumerator DelayedJump()
    {
        yield return new WaitForSeconds(jumpDelay);// 延迟指定的时间
        verticalSpeed = Mathf.Sqrt(-jumpMultiple * gravity);
    }
    //玩家面向相对于相机的方向
    void CaculateInputDirection()
    {
        Vector3 camForwardProjection = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        playerInput = camForwardProjection * moveInput.y + cameraTransform.right * moveInput.x;
        playerInput = playerTransform.InverseTransformVector(playerInput);
        // 计算目标旋转角度
        float rad = Mathf.Atan2(playerInput.x, playerInput.z);
        // 平滑过渡角度
        playerTransform.Rotate(0, rad * rotateSpeed * Time.deltaTime, 0f);//控制转向速度
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
            characterController.center = new Vector3(0.0f, 0.85f, 0.0f);
            characterController.height = 1.7f;
            characterController.radius = 0.15f;
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (playerPosture == PlayerPosture.upStair || playerPosture == PlayerPosture.upHill)
        {
            // 启用左脚和右脚的 IK
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f); 
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
            // 获取左右脚位置
            Vector3 leftFootPosition = animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            Vector3 rightFootPosition = animator.GetIKPosition(AvatarIKGoal.RightFoot);

            float stairHeight = playerTransform.position.y + 0.3f;//脚抬起
            float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1;
            if (moveInput.magnitude == 0)
            {

                rightFootPosition += transform.forward * 0.3f;//脚向前伸展的长度
                rightFootPosition.y = stairHeight;
            }
            else if (normalizedTime < 0.5f) 
            {
                leftFootPosition += transform.forward * 0.2f;
                leftFootPosition.y = stairHeight;
            }
            else 
            {
                rightFootPosition += transform.forward * 0.2f;
                rightFootPosition.y = stairHeight;
            }
            // 应用新的脚部位置
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPosition);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPosition);
        }
    }
    void SwitchState()
    {
        //// 获取当前端口数量
        //int currentInputCount = layerMixerPlayable.GetInputCount();
        //// 直接增加端口数
        //layerMixerPlayable.SetInputCount(currentInputCount + 1);
        if (!isSwitch && layerMixerPlayable.GetInput(3).IsValid())//GetInput(port).IsValid()来检查端口是否已经被连接
        {
            playableGraph.Disconnect(layerMixerPlayable, 3);
        }
        else if (isSwitch && !layerMixerPlayable.GetInput(3).IsValid())
        {
            fight.init(playableGraph);
            playableGraph.Connect(fight.fightPlayable, 0, layerMixerPlayable, 3);
            // playableGraph.Connect(jumpPlayable, 0, layerMixerPlayable, 2);
        }
    }
    void SetPoseture()
    {
        switch (playerPosture)
        {
            case PlayerPosture.stand:
                weight = Mathf.MoveTowards(weight, 0.0f, 0.5f * changeSpeed * Time.deltaTime);
                layerMixerPlayable.SetInputWeight(0, 1 - weight);
                layerMixerPlayable.SetInputWeight(1, 0);
                layerMixerPlayable.SetInputWeight(3, 0);
                layerMixerPlayable.SetInputWeight(2, weight);
                SetUpStandAnimator();
                break;
            case PlayerPosture.crouch:
                layerMixerPlayable.SetInputWeight(0, 0);
                layerMixerPlayable.SetInputWeight(2, 0);
                layerMixerPlayable.SetInputWeight(3, 0);
                layerMixerPlayable.SetInputWeight(1, 1);
                SetUpCrouchAnimator();
                break;
            case PlayerPosture.jumping:
                weight = Mathf.MoveTowards(weight, 1f, 10f * changeSpeed * Time.deltaTime);
                layerMixerPlayable.SetInputWeight(0, 1 - weight);
                layerMixerPlayable.SetInputWeight(1, 0);
                layerMixerPlayable.SetInputWeight(3, 0);
                layerMixerPlayable.SetInputWeight(2, weight);
                jumpPlayable.SetInputWeight(0, weight);
                break;
            case PlayerPosture.Fight:
                layerMixerPlayable.SetInputWeight(0, 0);
                layerMixerPlayable.SetInputWeight(1, 0);
                layerMixerPlayable.SetInputWeight(2, 0);
                layerMixerPlayable.SetInputWeight(3, 1);
                fight.fightPlayable.SetInputWeight(0, 1);
                break;
        }

    }
    void SetUpStandAnimator()
    {
        if (playerMovement == PlayerMovement.walk)
        {
            standPlayable.SetInputWeight(0, 0);
            standWeight = Mathf.MoveTowards(standWeight, 0.0f, changeSpeed * Time.deltaTime);
            standPlayable.SetInputWeight(2, standWeight);
            standPlayable.SetInputWeight(1, 1.0f - standWeight);
        }
        else if (playerMovement == PlayerMovement.run)
        {
            standPlayable.SetInputWeight(0, 0);
            standWeight = Mathf.MoveTowards(standWeight, 1.0f, changeSpeed * Time.deltaTime);
            standPlayable.SetInputWeight(1, 1.0f - standWeight);
            standPlayable.SetInputWeight(2, standWeight);
        }
        else
        {
            standPlayable.SetInputWeight(1, 0);
            standPlayable.SetInputWeight(2, 0);
            standPlayable.SetInputWeight(0, 1);
        }
    }
    void SetUpCrouchAnimator()
    {
        if (playerMovement == PlayerMovement.walk)
        {
            crouchWeight = Mathf.MoveTowards(crouchWeight, 0.0f, changeSpeed * Time.deltaTime);
            crouchPlayable.SetInputWeight(0, crouchWeight);
            crouchPlayable.SetInputWeight(1, 1 - crouchWeight);
        }
        else
        {
            crouchWeight = Mathf.MoveTowards(crouchWeight, 1.0f, 0.5f * changeSpeed * Time.deltaTime);
            crouchPlayable.SetInputWeight(0, crouchWeight);
            crouchPlayable.SetInputWeight(1, 1 - crouchWeight);
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
    void OnDisable()
    {
        playableGraph.Destroy();
    }

}
