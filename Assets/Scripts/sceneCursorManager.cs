using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneCursorManager : MonoBehaviour
{
    public static sceneCursorManager Instance;

    [System.Serializable]
    public struct SceneCursor
    {
        public string sceneName;
        public Texture2D cursorTexture;
        public Vector2 hotspot;
    }

    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Vector2 defaultHotspot = Vector2.zero;
    [SerializeField] private SceneCursor[] customSceneCursors;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCursorForScene(scene.name);
    }

    private void UpdateCursorForScene(string sceneName)
    {
        foreach (var sceneCursor in customSceneCursors)
        {
            if (sceneCursor.sceneName == sceneName)
            {
                Cursor.SetCursor(sceneCursor.cursorTexture, sceneCursor.hotspot, CursorMode.Auto);
                return;
            }
        }

        Cursor.SetCursor(defaultCursor, defaultHotspot, CursorMode.Auto);
    }
}
