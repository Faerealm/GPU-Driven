using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CameraDistance : MonoBehaviour
{
    private float currentDistance = 6.0f;
    private float minDistance = 1.0f;
    private float maxDistance = 10.0f;
    private CinemachineFramingTransposer framingTransposer;
    private CinemachineInputProvider inputProvide;
#if UNITY_ANDROID || UNITY_IOS
    private CinemachineVirtualCamera virtualCamera;
#endif
    private VariableJoystick joystick;
    private float joystickAreaMaxX;
    private float joystickAreaMaxY;
    private float rotationSpeed = 0.2f;

    void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.antiAliasing = 0;
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.lodBias = 0.3f;
        QualitySettings.particleRaycastBudget = 4;
        QualitySettings.softParticles = false;
#else
        Application.targetFrameRate = -1;
        Cursor.lockState = CursorLockMode.Locked;
#endif
    }
    void Start()
    {
        joystick = FindObjectOfType<VariableJoystick>();
        if (joystick != null)
        {
            // 获取摇杆区域的边界
            RectTransform joystickRect = joystick.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            joystickRect.GetWorldCorners(corners);
            joystickAreaMaxX = corners[2].x;
            joystickAreaMaxY = corners[2].y;
        }
        framingTransposer = GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineFramingTransposer>();
        inputProvide = GetComponent<CinemachineInputProvider>();

#if UNITY_ANDROID || UNITY_IOS
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        var lens = virtualCamera.m_Lens;
        lens.FieldOfView = 50f;
        //lens.FarClipPlane = 250f;
        virtualCamera.m_Lens = lens;
#endif

    }
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
#if UNITY_ANDROID || UNITY_IOS
        TouchControl();
#else
        Distance();
#endif
    }
    void TouchControl()
    {
        // 确保当前设备支持触摸
        if (Touchscreen.current == null)
            return;
        // 获取所有触摸点
        var touches = Touchscreen.current.touches;
        int touchCount = touches.Count;
        if (touchCount == 0) return;

        // 过滤掉在摇杆区域内的触摸
        List<TouchControl> validTouches = new List<TouchControl>();
        for (int i = 0; i < touchCount; i++)
        {
            TouchControl touch = touches[i];
            // 只处理进行中的触摸
            var phase = touch.phase.ReadValue();
            //去除phase == UnityEngine.InputSystem.TouchPhase.None未激活的触摸点
            if (phase == UnityEngine.InputSystem.TouchPhase.None || phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                continue;
            //Unity 的新输入系统中，触摸可能处于 Began、Moved、Stationary、Ended 或 Canceled 状态。旧的触摸状态未被及时清除可能会导致单指起到双指效果。 
            if (joystick.Horizontal != 0 || joystick.Vertical != 0)
            {
                if (i == 0) continue;
            }
            Vector2 pos = touch.position.ReadValue();
            if (pos.x > joystickAreaMaxX || pos.y > joystickAreaMaxY)
            {
                validTouches.Add(touch);
            }
            //validTouches.Add(touch);
        }
        if (validTouches.Count == 2)
        {
            // 双指缩放
            TouchControl touchZero = validTouches[0];
            TouchControl touchOne = validTouches[1];

            Vector2 touchZeroPos = touchZero.position.ReadValue();
            Vector2 touchOnePos = touchOne.position.ReadValue();

            Vector2 touchZeroPrevPos = touchZeroPos - touchZero.delta.ReadValue();
            Vector2 touchOnePrevPos = touchOnePos - touchOne.delta.ReadValue();

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZeroPos - touchOnePos).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            currentDistance = Mathf.Clamp(currentDistance + deltaMagnitudeDiff * 0.01f, minDistance, maxDistance);
            framingTransposer.m_CameraDistance = currentDistance;
        }
        else if (validTouches.Count == 1)
        {
            // 单指旋转
            TouchControl touch = validTouches[0];
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 touchDeltaPosition = touch.delta.ReadValue();
                float rotationX = touchDeltaPosition.y * rotationSpeed;
                float rotationY = touchDeltaPosition.x * rotationSpeed;

                var virtualCamera = GetComponent<CinemachineVirtualCamera>();
                if (virtualCamera != null)
                {
                    var aim = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
                    if (aim != null)
                    {
                        aim.m_HorizontalAxis.Value += rotationY;
                        aim.m_VerticalAxis.Value -= rotationX;
                    }
                }
            }
        }
    }
    void Distance()
    {
        float scrollSensitivity = 0.002f; // 滑轮输入值倍数
        float cameraValue = inputProvide.GetAxisValue(2) * scrollSensitivity;
        currentDistance = Mathf.Clamp(currentDistance + cameraValue, minDistance, maxDistance);
        // Debug.Log("相机距离：" +currentDistance);
        framingTransposer.m_CameraDistance = currentDistance;
    }
}
