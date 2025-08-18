using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class CameraSwitch : MonoBehaviour
{
    bool isPlayerIn;
    public PlayableDirector director;
    public GameObject player;
    public CinemachineVirtualCamera virtualCamera;
    CinemachineTrackedDolly cinemachineTrackedDolly;
    //时间记录
    float waitTimer01;
    float waitTimer02;

    PlayableCharacterControl playableCharacterControl;
    void Start()
    {
        // GameObject player = GameObject.Find("Audrey");  // 通过名字查找
        playableCharacterControl = player.GetComponent<PlayableCharacterControl>();//实例化继承了MonoBehaviour不可用new
        cinemachineTrackedDolly =virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        cinemachineTrackedDolly.m_PathPosition =0f;
    }

    void Update()
    {
        if (director.state == PlayState.Playing)
        {
            CinemachinePosition();
        }
        if (isPlayerIn)
        {
            return;
        }
        FindPlayer();
        
    }
    //检测玩家
    void FindPlayer()
    {
        RaycastHit hit01;
        RaycastHit hit02;
        float rayLength = 8f;
        Vector3 rayStart01 = transform.position+transform.forward*5;
        //transform.up
        Vector3 rayStart02 = transform.position+transform.right*4+transform.forward;
        bool hit01Detected = Physics.Raycast(rayStart01, -transform.forward, out hit01, rayLength);
        bool hit02Detected = Physics.Raycast(rayStart02, -transform.right, out hit02, rayLength);
  
        //Debug.DrawRay(rayStart01, -transform.forward * rayLength, Color.red);
        //Debug.DrawRay(rayStart02, -transform.right * rayLength, Color.red);
        if (hit01Detected || hit02Detected)
        {
            if ((hit01.collider != null && hit01.collider.name == "Kate") ||
        (hit02.collider != null && hit02.collider.name == "Kate")) { 
                if (!isPlayerIn)
                {
                    isPlayerIn = true;
                    director.Play();
                    DisablePlayerMovement();
                    Invoke("EnablePlayerMovement", (float)director.duration);
                }
            }
        }
    }
    // 禁用玩家移动
    void DisablePlayerMovement()
    {
        // 禁用 CharacterController
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // 停止 Animator 动画（冻结在当前帧）
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 0;
        }
        // 暂停 Playable 动画
        if (playableCharacterControl.playableGraph.IsValid())
        {
            playableCharacterControl.playableGraph.GetRootPlayable(0).SetSpeed(0); // 设置播放速度为 0，暂停动画
        }
    }

    // 恢复玩家移动
    void EnablePlayerMovement()
    {
        // 启用 CharacterController
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // 恢复 Animator 动画速度
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 1;
        }
        // 恢复 Playable 动画速度
        if (playableCharacterControl.playableGraph.IsValid())
        {
            playableCharacterControl.playableGraph.GetRootPlayable(0).SetSpeed(1);
        }
    }
    //相机位置控制
    void CinemachinePosition()
    {
        if (cinemachineTrackedDolly.m_PathPosition <= 0.4f)
            cinemachineTrackedDolly.m_PathPosition += 0.04f * Time.deltaTime;
        else if (cinemachineTrackedDolly.m_PathPosition <= 3f)
            cinemachineTrackedDolly.m_PathPosition += 0.4f * Time.deltaTime;
        else if (cinemachineTrackedDolly.m_PathPosition > 3f&& cinemachineTrackedDolly.m_PathPosition<=6f)
        {
           
            if (waitTimer01 < 2.5f)
            {
                cinemachineTrackedDolly.m_PathPosition = 5.5f;
                waitTimer01 += Time.deltaTime;
                return;
            }
            if (waitTimer02<2.5f)
            {
                cinemachineTrackedDolly.m_PathPosition = 4.2f;
                waitTimer02 += Time.deltaTime;
                return;
            }
            cinemachineTrackedDolly.m_PathPosition =0f;
        }
    }
}
