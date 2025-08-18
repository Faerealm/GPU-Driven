using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneSwitcher : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string clawSceneName = "ClawMachineScene";

    private bool isClawActive = false;
    private bool scenesLoaded = false;

    private Scene mainScene;
    private Scene clawScene;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (!scenesLoaded)
        {
            StartCoroutine(LoadBothScenesOnce());
        }
    }

    private IEnumerator LoadBothScenesOnce()
    {
        scenesLoaded = true;

        if (!SceneManager.GetSceneByName(mainSceneName).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        }
        mainScene = SceneManager.GetSceneByName(mainSceneName);

        if (!SceneManager.GetSceneByName(clawSceneName).isLoaded)
        {
            yield return SceneManager.LoadSceneAsync(clawSceneName, LoadSceneMode.Additive);
        }
        clawScene = SceneManager.GetSceneByName(clawSceneName);

        // 默认只启用主场景
        SetSceneActive(mainScene, true);
        SetSceneActive(clawScene, false);
        SceneManager.SetActiveScene(mainScene);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchScene();
        }
    }

    private void SwitchScene()
    {
        isClawActive = !isClawActive;

        SetSceneActive(mainScene, !isClawActive);
        SetSceneActive(clawScene, isClawActive);

        SceneManager.SetActiveScene(isClawActive ? clawScene : mainScene);
    }

    void SetSceneActive(Scene scene, bool active)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (!go.CompareTag("Vegetation") )
            {
                go.SetActive(active);
            }
        }
    }

}
