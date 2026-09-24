using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// EquipCanvas 같이 씬 전환 후에도 유지돼야 하는 GameObject에 붙임.
/// 동일 이름의 중복 인스턴스가 새로 로드되면 자기 자신을 파괴해 1개만 살아남도록 함.
/// excludedScenes에 등록된 씬(예: StartScene)에서는 자동 비활성화되어 UI가 보이지 않음.
/// (파괴가 아닌 비활성화이므로 다른 씬으로 돌아오면 다시 표시됨)
/// </summary>
public class SkillEquipPersist : MonoBehaviour
{
    private static SkillEquipPersist instance;

    [Tooltip("이 씬들에서는 이 UI가 자동으로 비활성화됩니다.")]
    [SerializeField] private string[] excludedScenes = { "StartScene" };

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        transform.SetParent(null, false);
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyVisibility(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyVisibility(scene.name);
    }

    private void ApplyVisibility(string sceneName)
    {
        bool shouldHide = IsExcludedScene(sceneName);
        if (gameObject.activeSelf == shouldHide) gameObject.SetActive(!shouldHide);
    }

    private bool IsExcludedScene(string sceneName)
    {
        if (excludedScenes == null) return false;
        foreach (var s in excludedScenes)
            if (s == sceneName) return true;
        return false;
    }
}
