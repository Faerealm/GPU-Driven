using UnityEngine;

public class PlatformUIManager : MonoBehaviour
{
    [SerializeField]
    private Canvas mobileUICanvas;
    [SerializeField]
    private Canvas desktopUICanvas;

    void Awake()
    {
        SetupPlatformUI();
    }

    private void SetupPlatformUI()
    {
#if UNITY_STANDALONE || UNITY_WEBGL
        if (mobileUICanvas != null) mobileUICanvas.gameObject.SetActive(false);
        if (desktopUICanvas != null) desktopUICanvas.gameObject.SetActive(true);
#elif UNITY_ANDROID || UNITY_IOS
        if (mobileUICanvas != null) mobileUICanvas.gameObject.SetActive(true);
        if (desktopUICanvas != null) desktopUICanvas.gameObject.SetActive(false);
#endif
    }
}