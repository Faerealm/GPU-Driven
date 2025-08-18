using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Manager_ClawMovement : MonoBehaviour
{

    [Header("Player Settings")]
    public bool freePlay = false;
    public int playerCoins = 1;
    private int lastCoins;
    [Header("UI Settings")]
    public Text coinsTextLabel;
    public GameObject UI_OutOfCoinsPopup;
   
    private bool UI_ClawButtonUp = false;
    private bool UI_ClawButtonDown = false;
    private bool UI_ClawButtonLeft = false;
    private bool UI_ClawButtonRight = false;

    [Space(5f)]

    [Header("Claw Settings")]

    public Transform clawHolder;
    public Transform clawHeader;
    public Transform clawParent;
    public float movementSpeed = 1.0f;

    public float dropSpeed = 1.0f;

    [Range(0, 1)]
    public float failRate = 0;
    [HideInInspector]
    public bool canMove = true;

    [HideInInspector]
    public bool stopMovement = false;
    public Transform bottom;
    private float LimitY = 2.074f;
    [Tooltip("If enabled, the claw will automatically go back to the home / inital start position")]
    public bool shouldReturnHomeAutomatically = false;
    [HideInInspector]
    public Vector3 clawHomePosition;

    [HideInInspector]
    public Vector3 clawDropFromPosition;
    public Animation clawHeadAnimation;

    [Header("Claw Movement Boundary Limits")]
    public float clawHeadSizeX = 0.30f;
    public float clawHeadSizeZ = 0.13f;
    private float boundaryX_Left;
    private float boundaryX_Right;
    private float boundaryZ_Back;
    private float boundaryZ_Forward;
    public Transform clawBoundaryX_Left;
    public Transform clawBoundaryX_Right;
    public Transform clawBoundaryZ_Back;
    public Transform clawBoundaryZ_Forward;
    private bool isMovingUp = false;
    private bool isMovingDown = false;
    private bool isMovingLeft = false;
    private bool isMovingRight = false;
    private bool isOpenKey = false;
    private bool isOpenAndDrowKey = false;
    public bool isClawPrize=false;//是否成功抓取奖品
    [Header("Ovverhead Motor Settings")]

    // Motors
    public Transform topMainMotor;
    public Transform overHeadMotorRailSystem;

    [HideInInspector]
    public Vector3 topMainMotorHomePosition;

    [HideInInspector]
    public Vector3 overHeadMotorRailSystemHomePosition;

    [HideInInspector]
    public bool isDroppingBall = false;

    [Header("Misc Settings")]
    public PrizeCatcherDetector_ClawMachine prizeCatcherDetector;

    [Header("Grip Force Settings")]
    public float minGripForce = 10f;
    public float maxGripForce = 100f;
    [Header("Detection Settings")]
    public LayerMask grippableLayer;
    [Header("Claw Components")]
    public ClawFingerCollider[] clawFingers; // 所有爪子碰撞检测器的引用
    [HideInInspector] public GameObject targetObject; // 改为 public 以便 ClawFingerCollider 访问

    void Start()
    {
        LimitY = bottom.transform.position.y;
        lastCoins = playerCoins;
        coinsTextLabel.text = "Coins: " + playerCoins;
        clawHomePosition = clawHolder.transform.position;
        topMainMotorHomePosition = topMainMotor.position;
        overHeadMotorRailSystemHomePosition = overHeadMotorRailSystem.position;

        //设置边界位置，娃娃机内部空间大小范围
        boundaryX_Left = clawBoundaryX_Left.position.x;
        boundaryX_Right = clawBoundaryX_Right.position.x;
        boundaryZ_Back = clawBoundaryZ_Back.position.z;
        boundaryZ_Forward = clawBoundaryZ_Forward.position.z;

        boundaryX_Left += clawHeadSizeX;
        boundaryX_Right -= clawHeadSizeX;
        boundaryZ_Back -= clawHeadSizeZ;
        boundaryZ_Forward += clawHeadSizeZ;
        InitializeComponents();
    }
    private void InitializeComponents()
    {
        // 自动查找所有爪子碰撞检测器
        if (clawFingers == null || clawFingers.Length == 0)
        {
            clawFingers = GetComponentsInChildren<ClawFingerCollider>();
        }
    }
    void Update()
    {
        //Debug.DrawRay(clawHolder.position - Vector3.up*3.02f, Vector3.down * 0.2f, Color.red);
        if (lastCoins != playerCoins)  // 只在数量变化时更新UI
        {
            coinsTextLabel.text = "Coins: " + playerCoins;
            lastCoins = playerCoins;
        };
    }
    #region 按钮控制逻辑
    public void OpenKey(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isOpenKey = ctx.ReadValueAsButton();
    }
    public void SpaceKey(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isOpenAndDrowKey = ctx.ReadValueAsButton();
    }
    public void UpKey(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isMovingUp = ctx.ReadValueAsButton();
    }

    public void DownKey(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isMovingDown = ctx.ReadValueAsButton();
    }

    public void LeftKey(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isMovingLeft = ctx.ReadValueAsButton();
    }

    public void RightKey(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        isMovingRight = ctx.ReadValueAsButton();
    }
    public void UI_OutOfCoinsPopup_PurchaseMore()
    {
        playerCoins = playerCoins + 10;

        UI_OutOfCoinsPopup.SetActive(false);
    }

    public void UI_DropClawButton()
    {
        dropClawButtonInput();
    }

    public void UI_OpenClawButton()
    {
        openClawButtonInput();
    }

    public void UI_MoveClawLeft()
    {
        UI_ClawButtonLeft = true;
    }
    public void UI_MoveClawLeft_Off()
    {
        UI_ClawButtonLeft = false;
    }

    public void UI_MoveClawRight()
    {
        UI_ClawButtonRight = true;
    }
    public void UI_MoveClawRight_Off()
    {
        UI_ClawButtonRight = false; ;
    }

    public void UI_MoveClawUp()
    {
        UI_ClawButtonUp = true;
    }
    public void UI_MoveClawUp_Off()
    {
        UI_ClawButtonUp = false;
    }
    public void UI_MoveClawDown()
    {
        UI_ClawButtonDown = true;
    }
    public void UI_MoveClawDown_Off()
    {
        UI_ClawButtonDown = false;
    }
    private void FixedUpdate()
    {
        if (canMove)
        {
            if (isOpenKey)
            {
                openClawButtonInput();
            }
            if (isOpenAndDrowKey)
            {
                dropClawButtonInput();
            }
            if (isMovingUp || UI_ClawButtonUp)
            {
                clawMoveUp();
            }

            if (isMovingDown || UI_ClawButtonDown)
            {
                clawMoveDown();
            }

            if (isMovingLeft || UI_ClawButtonLeft)
            {
                clawMoveLeft();
            }

            if (isMovingRight || UI_ClawButtonRight)
            {
                clawMoveRight();
            }
        }
    }

    #endregion

    private void SelectTargetObject()
    {
        float searchRadius = 0.06f; // 搜索半径，可以根据需要调整
        Vector3 clawPosition = clawHeader.position;
       // Debug.Log(clawPosition);
        //获取算法：由于服务器知道物品和爪子的位置，可以将物体和爪子的位置进行分区并创建一个哈希表，
        //爪子在某一个位置对应了一个分区，然后计算所在分区内物体里爪子的距离，选择离爪子最近的物体为目标物体
        //这样相较于遍历所有物体，大大降低了时间复杂度
        //由于开发环境限制，这里采用了内置的OverlapSphere（这是根据collider进行的区域检测，不符合安全性要求）
        // 在爪子正下方创建一个球形检测区域

        Collider[] colliders = Physics.OverlapSphere(
            new Vector3(clawPosition.x, LimitY, clawPosition.z),
            searchRadius,
            grippableLayer
        );

        float closestDistance = float.MaxValue;
        GameObject closestObject = null;
        foreach (Collider collider in colliders)
        {
            // 获取物体所有部分的Renderer
            Renderer[] renderers = collider.GetComponentsInChildren<Renderer>();

            // 检查每个部分的几何中心
            foreach (Renderer renderer in renderers)
            {
                Vector3 partCenter = renderer.bounds.center;

                // 计算爪子到这个部分的距离
                float distance = Vector2.Distance(
                    new Vector3(clawPosition.x, clawPosition.y, clawPosition.z),
                    new Vector3(partCenter.x, partCenter.y, partCenter.z)
                );

                // 如果这个部分是最近的，选择整个物体
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = collider.gameObject;
                }
            }

            // 如果没有Renderer，使用物体本身的位置
            if (renderers.Length == 0)
            {
                float distance = Vector2.Distance(
                    new Vector3(clawPosition.x, clawPosition.y,clawPosition.z),
                    new Vector3(collider.transform.position.x, collider.transform.position.y,collider.transform.position.z)
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = collider.gameObject;
                }
            }
        }
        if (closestObject != null)
        {
            targetObject = closestObject;
            isClawPrize = true;
           // Debug.Log($"Selected target: {targetObject.name} at distance: {closestDistance}");
        }
        else
        {
            targetObject = null;
            //Debug.Log("No target found in range");
        }
    }
    #region 四叉树分割代码
    private class QuadtreeNode
    {
        public Rect bounds;
        public List<GameObject> objects = new List<GameObject>();
        public QuadtreeNode[] children = null;
        public int depth;

        public QuadtreeNode(Rect bounds, int depth)
        {
            this.bounds = bounds;
            this.depth = depth;
        }

        public bool IsLeaf => children == null;
    }

    private QuadtreeNode quadtreeRoot;
    private int quadtreeMaxObjects = 4;
    private int quadtreeMaxDepth = 6;

    private void InitQuadtree(Rect area)
    {
        quadtreeRoot = new QuadtreeNode(area, 0);
    }

    private void QuadtreeInsert(GameObject obj)
    {
        Vector2 pos = new Vector2(obj.transform.position.x, obj.transform.position.z);
        QuadtreeInsert(quadtreeRoot, obj, pos);
    }

    private void QuadtreeInsert(QuadtreeNode node, GameObject obj, Vector2 pos)
    {
        if (!node.bounds.Contains(pos))
            return;

        if (node.IsLeaf)
        {
            node.objects.Add(obj);
            if (node.objects.Count > quadtreeMaxObjects && node.depth < quadtreeMaxDepth)
            {
                QuadtreeSubdivide(node);
                for (int i = node.objects.Count - 1; i >= 0; i--)
                {
                    GameObject o = node.objects[i];
                    Vector2 p = new Vector2(o.transform.position.x, o.transform.position.z);
                    foreach (var child in node.children)
                    {
                        if (child.bounds.Contains(p))
                        {
                            QuadtreeInsert(child, o, p);
                            node.objects.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var child in node.children)
            {
                if (child.bounds.Contains(pos))
                {
                    QuadtreeInsert(child, obj, pos);
                    return;
                }
            }
        }
    }

    private void QuadtreeSubdivide(QuadtreeNode node)
    {
        node.children = new QuadtreeNode[4];
        float halfWidth = node.bounds.width / 2f;
        float halfHeight = node.bounds.height / 2f;
        float x = node.bounds.xMin;
        float y = node.bounds.yMin;

        node.children[0] = new QuadtreeNode(new Rect(x, y, halfWidth, halfHeight), node.depth + 1); // 左下
        node.children[1] = new QuadtreeNode(new Rect(x + halfWidth, y, halfWidth, halfHeight), node.depth + 1); // 右下
        node.children[2] = new QuadtreeNode(new Rect(x, y + halfHeight, halfWidth, halfHeight), node.depth + 1); // 左上
        node.children[3] = new QuadtreeNode(new Rect(x + halfWidth, y + halfHeight, halfWidth, halfHeight), node.depth + 1); // 右上
    }

    private List<GameObject> QuadtreeQuery(Rect range)
    {
        List<GameObject> found = new List<GameObject>();
        QuadtreeQuery(quadtreeRoot, range, found);
        return found;
    }

    private void QuadtreeQuery(QuadtreeNode node, Rect range, List<GameObject> found)
    {
        if (!node.bounds.Overlaps(range))
            return;

        foreach (var obj in node.objects)
        {
            Vector2 pos = new Vector2(obj.transform.position.x, obj.transform.position.z);
            if (range.Contains(pos))
                found.Add(obj);
        }

        if (!node.IsLeaf)
        {
            foreach (var child in node.children)
            {
                QuadtreeQuery(child, range, found);
            }
        }
    }

    private void QuadtreeRemove(GameObject obj)
    {
        Vector2 pos = new Vector2(obj.transform.position.x, obj.transform.position.z);
        QuadtreeRemove(quadtreeRoot, obj, pos);
    }

    private bool QuadtreeRemove(QuadtreeNode node, GameObject obj, Vector2 pos)
    {
        if (!node.bounds.Contains(pos))
            return false;

        if (node.objects.Remove(obj))
            return true;

        if (!node.IsLeaf)
        {
            foreach (var child in node.children)
            {
                if (QuadtreeRemove(child, obj, pos))
                    return true;
            }
        }
        return false;
    }
    #endregion
    private void dropClawButtonInput()
    {
        if (freePlay)
        {
            StartCoroutine(dropClaw());

            canMove = false;//爪子执行抓取动作时，禁止玩家控制爪子的移动。
        }
        else if (!freePlay)
        {
            if (playerCoins > 0)
            {
                playerCoins--; 
                //Debug.Log(playerCoins);
                StartCoroutine(dropClaw());
                canMove = false;
            }
            else
            {
                if (!UI_OutOfCoinsPopup.activeInHierarchy)
                    UI_OutOfCoinsPopup.SetActive(true);
            }
        }
    }

    private void openClawButtonInput()
    {
        if (!isDroppingBall)
        {
            StartCoroutine(DropBall());
        }
    }
    #region 前后左右移动
    private void clawMoveUp()
    {
        // 沿 Z 轴正方向移动
        if (clawHolder.transform.position.z < boundaryZ_Back)
        {
            // 移动爪子
            clawHolder.Translate(0f, 0f, movementSpeed * 1 * Time.deltaTime);

            // 移动悬挂在上方的电机系统
            overHeadMotorRailSystem.Translate(0f, 0f, movementSpeed * 1 * Time.deltaTime);
            topMainMotor.Translate(0f, 0f, movementSpeed * 1 * Time.deltaTime);

        }
    }

    private void clawMoveDown()
    {
        // 沿 Z 轴负方向移动
        if (clawHolder.transform.position.z > boundaryZ_Forward)
        {
            clawHolder.Translate(0f, 0f, movementSpeed * -1 * Time.deltaTime);
            overHeadMotorRailSystem.Translate(0f, 0f, movementSpeed * -1 * Time.deltaTime);
            topMainMotor.Translate(0f, 0f, movementSpeed * -1 * Time.deltaTime);

        }
    }

    private void clawMoveLeft()
    {

        // 延x轴移动
        if (clawHolder.transform.position.x > boundaryX_Left)
        {
            clawHolder.Translate(movementSpeed * -1 * Time.deltaTime, 0f, 0f);
            topMainMotor.Translate(movementSpeed * -1 * Time.deltaTime, 0f, 0f);

        }
    }

    private void clawMoveRight()
    {
        // 延x轴负向移动
        if (clawHolder.transform.position.x < boundaryX_Right)
        {
            clawHolder.Translate(movementSpeed * 1 * Time.deltaTime, 0f, 0f);
            topMainMotor.Translate(movementSpeed * 1 * Time.deltaTime, 0f, 0f);

        }
    }
    #endregion

    IEnumerator dropClaw()
    {
        clawDropFromPosition = clawHolder.transform.position;
        OpenClaw();
        SelectTargetObject();
        float clawOffset = 0.3f;
        bool shouldStopDescent = false;  
        while (!stopMovement && !shouldStopDescent)  // 下降过程
        {
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(clawHolder.position, Vector3.down, out hit, 1f);
            float nextY = clawHolder.position.y - dropSpeed * Time.deltaTime;
            if (hitSomething)
            {
                float safeHeight = hit.point.y + clawOffset;
                if (clawHolder.position.y <= safeHeight)
                {
                    shouldStopDescent = true;
                    nextY = safeHeight;
                }
            }
            if (nextY < LimitY + clawOffset) // 确保不会低于最低限制
            {
                nextY = LimitY + clawOffset;
                shouldStopDescent = true;
            }
            clawHolder.position = new Vector3( // 更新位置
                clawHolder.position.x,
                nextY,
                clawHolder.position.z
            );
            yield return null;
        }    
        CloseClaw();    
        bool grabbedObject = false;
        bool hasOpenClaw = false;
        bool hasDebug=false;//调试输出用
        // 上升过程
        float riseTime = 0f;
        float openClawDelay = 1.5f;
        while (clawHolder.position.y < clawDropFromPosition.y)
        {
           
            riseTime += Time.deltaTime;
            bool isHaveObject = false;
            if ( riseTime >= openClawDelay-0.1f)
            {
               isHaveObject = IsGrabbingObject(); 
            }
            if (targetObject != null)
            {
                foreach (var finger in clawFingers)
                {
                    if (finger.IsContactingTarget(targetObject))
                    {
                        grabbedObject = true;
                        break;
                    }
                }
            }
            // 如果抓错了物体且上升了openClawDelay秒，打开爪子          
            if (!grabbedObject&& isHaveObject && riseTime >= openClawDelay)
            {
                if (!hasDebug)
                {
                    hasDebug = true;
                }
                OpenClaw();
                isClawPrize = false;
                hasOpenClaw= true;
            }
            if (hasOpenClaw&&riseTime > openClawDelay+ clawHeadAnimation["Claw_Open_New"].length)
            {
                CloseClaw();
                hasOpenClaw= false;
            }
            clawHolder.Translate(0f, dropSpeed * Time.deltaTime, 0f);
            yield return null;
        }
        canMove = true;
        stopMovement = false;
    }
    
    private bool IsGrabbingObject()
    {
        // 在爪子位置创建一个检测范围
        Vector3 checkPosition = clawHolder.position - Vector3.up * 3.0f; 
        float checkRadius = 0.25f; // 检测半径
       
        // 使用OverlapSphere检测范围内的物体
        Collider[] colliders = Physics.OverlapSphere(checkPosition, checkRadius);

        foreach (Collider col in colliders)
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Item"))
            {
                return true;
            }
        }

        return false;
    }
    #region 动画相关操作
    private void OpenClaw()
    {
        clawHeadAnimation["Claw_Open_New"].speed = 0.8f;
        clawHeadAnimation["Claw_Open_New"].time = 0f;
        clawHeadAnimation.CrossFade("Claw_Open_New");
    }

    private void CloseClaw()
    {     
        //爪子动画闭合
        clawHeadAnimation["Claw_Open_New"].speed = -1.3f;
        clawHeadAnimation["Claw_Open_New"].time = clawHeadAnimation["Claw_Open_New"].length;
        clawHeadAnimation.CrossFade("Claw_Open_New");
        // StartCoroutine(EnablePhysicsAfterClose());
        StartCoroutine(CheckClawContact());
    }
    private IEnumerator CheckClawContact()
    {
        // 等待爪子闭合动画完成
        yield return new WaitForSeconds(clawHeadAnimation["Claw_Open_New"].length);
        // 只要还在抓取状态就持续检测
        while (isClawPrize && targetObject != null)
        {
            bool hasContact = false;
            // 检查所有爪子碰撞体
            foreach (var finger in clawFingers)
            {
                if (finger.IsContactingTarget(targetObject))
                {
                    hasContact = true;
                    break;
                }
            }
            // 如果没有任何爪子接触到目标物体
            if (!hasContact)
            {
                isClawPrize = false;
              
                break;
            }
            if (prizeCatcherDetector.isClawAbovePrizeCatcher&&isClawPrize)
            {
                
                Debug.Log("{targetObject.name} isClawPrize   " + isClawPrize);
                break;
            }
            yield return null;  // 等待下一帧
        }
    }
    IEnumerator WeakClaws()
    {//-1为爪子动画闭合
        clawHeadAnimation["Claw_Open_Weak"].speed = -1f;
        clawHeadAnimation["Claw_Open_Weak"].time = clawHeadAnimation["Claw_Open_Weak"].length;
        clawHeadAnimation.CrossFade("Claw_Open_Weak");

        yield return new WaitForSeconds(Random.Range(2.15f, 2.85f));

        clawHeadAnimation["Claw_Close_From_Weak"].speed = -1f;
        clawHeadAnimation["Claw_Close_From_Weak"].time = clawHeadAnimation["Claw_Close_From_Weak"].length;
        clawHeadAnimation.CrossFade("Claw_Close_From_Weak");

    }
    #endregion

    IEnumerator DropBall()
    {
        isDroppingBall = true;

        OpenClaw();

        yield return new WaitForSeconds(clawHeadAnimation["Claw_Open_New"].length+0.2f);

        CloseClaw();

        isDroppingBall = false;
    }
}
#region test
//if (targetObject != null)
//{
//    targetObject.transform.SetParent(null);
//    Rigidbody rb = targetObject.GetComponent<Rigidbody>();
//    if (rb != null)
//    {
//        rb.isKinematic = false;
//    }
//    Collider[] colliders = targetObject.GetComponentsInChildren<Collider>();
//    foreach (Collider collider in colliders)
//    {
//        collider.enabled = true;
//    }
//}
//if (targetObject != null)
//{
//    Rigidbody rb = targetObject.GetComponent<Rigidbody>();
//    if (rb != null)
//    {
//        rb.isKinematic = true;
//        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>();
//        foreach (Collider collider in colliders)
//        {
//            collider.enabled = false;
//        }
//        // 获取物体的几何中心
//        Vector3 geometricCenter;
//        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>();
//        if (renderers.Length > 0)
//        {
//            Bounds bounds = renderers[0].bounds;
//            for (int i = 1; i < renderers.Length; i++)
//            {
//                bounds.Encapsulate(renderers[i].bounds);
//            }
//            geometricCenter = bounds.center;
//        }
//        else
//        {
//            geometricCenter = targetObject.transform.position;
//        }

//        // 计算需要的偏移量，使几何中心对准爪子中心
//        Vector3 offset = targetObject.transform.position - geometricCenter;
//        Vector3 targetPos = clawHeader.transform.position + offset;

//        // 平滑移动
//        float elapsedTime = 0f;
//        float duration = 0.05f;
//        Vector3 startPos = targetObject.transform.position;

//        while (elapsedTime < duration)
//        {
//            elapsedTime += Time.deltaTime;
//            float t = elapsedTime / duration;

//            targetObject.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, t));
//            yield return null;
//        }

//        // 确保最终位置精确
//        targetObject.transform.position = targetPos;
//        targetObject.transform.SetParent(clawParent);
//        Debug.Log("Target grabbed!");
//    }
//}
#endregion