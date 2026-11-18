using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu utama belum punya label skor terbaik. Skrip ini menambahkannya saat
// runtime (tanpa mengubah scene milik rekan) dengan membaca PlayerPrefs yang
// sudah dipakai ScoreManager.
[AddComponentMenu("Neon Rush/Integration/Main Menu Best Score")]
public class NeonMainMenuBestScore : MonoBehaviour
{
    private const string SceneName = "MainMenu";
    private const string BestScoreKey = "NeonRush_BestScore";
    private const string LabelName = "BestScoreText";
    private const string MenuPanelName = "MenuPanel";
    private const string TemplateName = "Subtitle";

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
        if (FindAnyObjectByType<NeonMainMenuBestScore>() != null) return;

        GameObject host = new GameObject("NeonMainMenuBestScore");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<NeonMainMenuBestScore>();
    }

    private void Start()
    {
        GameObject menuPanel = FindSceneObject(MenuPanelName);
        if (menuPanel == null)
        {
            Debug.LogWarning("[NeonMainMenuBestScore] '" + MenuPanelName + "' tidak ditemukan di scene " + SceneName + ".", this);
            return;
        }

        if (menuPanel.transform.Find(LabelName) != null) return;

        TMP_Text template = FindTemplate(menuPanel.transform);
        if (template == null)
        {
            Debug.LogWarning("[NeonMainMenuBestScore] Tidak ada teks TMP untuk contoh font.", this);
            return;
        }

        GameObject labelObject = new GameObject(LabelName, typeof(RectTransform));
        labelObject.transform.SetParent(menuPanel.transform, false);
        labelObject.transform.SetAsLastSibling();

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = template.font;
        label.fontSharedMaterial = template.fontSharedMaterial;
        label.fontSize = 40f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.1f, 0.95f, 1f);
        label.raycastTarget = false;
        label.text = "TERBAIK  " + PlayerPrefs.GetInt(BestScoreKey, 0);

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -300f);
        rect.sizeDelta = new Vector2(900f, 70f);
        rect.localScale = Vector3.one;
    }

    private static TMP_Text FindTemplate(Transform parent)
    {
        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == TemplateName) return texts[i];
        }

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null) return texts[i];
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
