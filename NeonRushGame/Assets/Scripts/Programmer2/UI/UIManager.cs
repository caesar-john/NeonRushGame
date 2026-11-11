using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panel (opsional)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Mode Scene")]
    [Tooltip("Aktifkan jika Main Menu dan Game berada di scene terpisah.")]
    [SerializeField] private bool useSeparateScenes = true;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameScene = "Game";

    [Header("Lainnya")]
    [SerializeField] private bool manageCursor = true;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager != null) gameManager.StateChanged += OnStateChanged;

        OnStateChanged(gameManager != null ? gameManager.State : GameState.Menu);
    }

    private void OnDestroy()
    {
        if (gameManager != null) gameManager.StateChanged -= OnStateChanged;
    }

    private void Update()
    {
        if (gameManager != null && gameManager.IsPlaying && PausePressed())
            gameManager.Pause();
    }

    public void PlayGame()
    {
        AudioManager.PlayUiClick();

        if (useSeparateScenes)
        {
            LoadScene(gameScene);
            return;
        }

        if (gameManager != null)
        {
            gameManager.StartGame();
            return;
        }

        LoadScene(gameScene);
    }

    public void PauseGame()
    {
        AudioManager.PlayUiClick();
        if (gameManager != null) gameManager.Pause();
    }

    public void ResumeGame()
    {
        AudioManager.PlayUiClick();
        if (gameManager != null) gameManager.Resume();
    }

    public void RestartGame()
    {
        AudioManager.PlayUiClick();
        Time.timeScale = 1f;

        if (useSeparateScenes)
        {
            LoadScene(gameScene);
            return;
        }

        if (gameManager != null) gameManager.Restart();
        else ReloadActiveScene();
    }

    public void GoToMainMenu()
    {
        AudioManager.PlayUiClick();
        Time.timeScale = 1f;

        if (useSeparateScenes)
        {
            LoadScene(mainMenuScene);
            return;
        }

        if (gameManager != null) gameManager.Restart();
        else ReloadActiveScene();
    }

    public void QuitGame()
    {
        AudioManager.PlayUiClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnStateChanged(GameState state)
    {
        SetActive(mainMenuPanel, state == GameState.Menu);
        SetActive(hudPanel, state == GameState.Playing || state == GameState.Paused);
        SetActive(pausePanel, state == GameState.Paused);
        SetActive(gameOverPanel, state == GameState.GameOver);

        if (manageCursor) Cursor.visible = state != GameState.Playing;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active) target.SetActive(active);
    }

    private static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[UIManager] Nama scene masih kosong.");
            return;
        }

        if (!SceneInBuild(sceneName))
        {
            Debug.LogError("[UIManager] Scene '" + sceneName + "' belum dimasukkan ke Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private static bool SceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, System.StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static void ReloadActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
        else SceneManager.LoadScene(scene.name);
    }
}
