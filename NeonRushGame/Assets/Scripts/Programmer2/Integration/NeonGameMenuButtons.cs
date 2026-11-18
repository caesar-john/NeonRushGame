using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Panel Pause/GameOver di scene "Game" hanya punya Resume/Restart. Skrip ini
// menambahkan tombol MENU saat runtime bila belum ada, memakai tombol yang
// sudah ada sebagai contoh gaya agar tidak perlu mengedit scene rekan.
[AddComponentMenu("Neon Rush/Integration/Game Menu Buttons")]
public class NeonGameMenuButtons : MonoBehaviour
{
    private const string SceneName = "Game";
    private const string PausePanelName = "PausePanel";
    private const string GameOverPanelName = "GameOverPanel";
    private const string MethodName = "GoToMainMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name != SceneName) return;
        if (FindAnyObjectByType<NeonGameMenuButtons>() != null) return;

        GameObject host = new GameObject("NeonGameMenuButtons");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<NeonGameMenuButtons>();
    }

    private void Start()
    {
        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui == null)
        {
            Debug.LogWarning("[NeonGameMenuButtons] UIManager tidak ditemukan.", this);
            return;
        }

        EnsureMenuButton(PausePanelName, "ResumeButton", "PauseMenuButton", -110f, ui);
        EnsureMenuButton(GameOverPanelName, "RestartButton", "GameOverMenuButton", -300f, ui);
    }

    private static void EnsureMenuButton(string panelName, string sourceName, string newName, float y, UIManager ui)
    {
        GameObject panel = FindSceneObject(panelName);
        if (panel == null) return;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && HasMenuCall(buttons[i])) return;
        }

        Transform source = FindDeep(panel.transform, sourceName);
        if (source == null || source.GetComponent<Button>() == null)
        {
            Debug.LogWarning("[NeonGameMenuButtons] Tombol contoh '" + sourceName + "' tidak ditemukan di " + panelName + ".", panel);
            return;
        }

        GameObject clone = Instantiate(source.gameObject, source.parent);
        clone.name = newName;
        clone.SetActive(true);

        RectTransform rect = clone.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        Button button = clone.GetComponent<Button>();
        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(ui.GoToMainMenu);
        }

        TMP_Text label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "MENU";
    }

    private static bool HasMenuCall(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == MethodName) return true;
        }

        return false;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == name) return children[i];
        }

        return null;
    }

    private static GameObject FindSceneObject(string name)
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded) return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            if (roots[i].name == name) return roots[i];

            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j] != null && children[j].name == name) return children[j].gameObject;
            }
        }

        return null;
    }
}
