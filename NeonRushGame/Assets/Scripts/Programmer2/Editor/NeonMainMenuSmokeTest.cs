using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Smoke test scene MainMenu: memastikan label skor terbaik ditambahkan saat
// runtime dan menampilkan nilai dari PlayerPrefs.
public static class NeonMainMenuSmokeTest
{
    private const string ActiveKey = "NeonRush.MenuSmoke.Active";
    private const string FinishedKey = "NeonRush.MenuSmoke.Finished";
    private const string FailedKey = "NeonRush.MenuSmoke.Failed";
    private const string FailsKey = "NeonRush.MenuSmoke.Fails";
    private const string StartKey = "NeonRush.MenuSmoke.Start";
    private const string BestBeforeKey = "NeonRush.MenuSmoke.BestBefore";
    private const string ExceptionsKey = "NeonRush.MenuSmoke.Exceptions";

    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SceneName = "MainMenu";
    private const string BestScoreKey = "NeonRush_BestScore";
    private const int ExpectedBest = 4321;
    private const float Timeout = 30f;

    public static void Run()
    {
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(FailedKey, false);
        SessionState.SetString(FailsKey, "");
        SessionState.SetString(ExceptionsKey, "");
        SessionState.SetFloat(StartKey, -1f);
        SessionState.SetInt(BestBeforeKey, PlayerPrefs.GetInt(BestScoreKey, 0));

        PlayerPrefs.SetInt(BestScoreKey, ExpectedBest);
        PlayerPrefs.Save();

        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("[MENU-SMOKE] Memulai tes scene MainMenu...");
        Hook();
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Bootstrap()
    {
        if (SessionState.GetBool(ActiveKey, false)) Hook();
    }

    private static void Hook()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (message == null || message.StartsWith("[MENU-SMOKE]")) return;
        if (type == LogType.Exception)
            SessionState.SetString(ExceptionsKey, SessionState.GetString(ExceptionsKey, "") + message + "\n");
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (SessionState.GetBool(FinishedKey, false)) return;
        if (!EditorApplication.isPlaying) return;

        double now = EditorApplication.timeSinceStartup;
        float start = SessionState.GetFloat(StartKey, -1f);
        if (start < 0f)
        {
            start = (float)now;
            SessionState.SetFloat(StartKey, start);
            return;
        }

        if (now - start > Timeout)
        {
            Fail("Timeout: tes MainMenu tidak selesai.");
            Finish();
            return;
        }

        if (now - start < 1.5) return;

        GameObject labelObject = FindSceneObject("BestScoreText");
        Check(labelObject != null, "Label BestScoreText ditambahkan ke MenuPanel");
        if (labelObject != null)
        {
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            Check(label != null, "Label memakai TextMeshProUGUI");
            if (label != null)
            {
                Check(label.text.Contains(ExpectedBest.ToString()), "Best score tampil: '" + label.text + "'");
                Check(label.rectTransform.parent != null && label.rectTransform.parent.name == "MenuPanel",
                      "Label berada di MenuPanel");
            }
        }

        string exceptions = SessionState.GetString(ExceptionsKey, "");
        Check(string.IsNullOrEmpty(exceptions), "Tidak ada exception" + (string.IsNullOrEmpty(exceptions) ? "" : ": " + exceptions));

        Finish();
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

    private static void Check(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log("[MENU-SMOKE][OK] " + message);
            return;
        }

        Fail(message);
    }

    private static void Fail(string message)
    {
        SessionState.SetBool(FailedKey, true);
        SessionState.SetString(FailsKey, SessionState.GetString(FailsKey, "") + "- " + message + "\n");
        Debug.LogError("[MENU-SMOKE][FAIL] " + message);
    }

    private static void Finish()
    {
        if (SessionState.GetBool(FinishedKey, false)) return;
        SessionState.SetBool(FinishedKey, true);
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived -= OnLog;
        Debug.Log("[MENU-SMOKE] Menutup Play Mode...");
        EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;
        if (!SessionState.GetBool(FinishedKey, false)) return;

        SessionState.SetBool(FinishedKey, false);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        PlayerPrefs.SetInt(BestScoreKey, SessionState.GetInt(BestBeforeKey, 0));
        PlayerPrefs.Save();

        if (SessionState.GetBool(FailedKey, false))
        {
            Debug.LogError("MENU SMOKE GAGAL:\n" + SessionState.GetString(FailsKey, ""));
            EditorApplication.delayCall += () => EditorApplication.Exit(1);
            return;
        }

        Debug.Log("MENU SMOKE LULUS: label skor terbaik tampil di MainMenu.");
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }
}
