using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NeonRushSmokeTest
{
    private const string ActiveKey = "NeonRush.Smoke.Active";
    private const string StartKey = "NeonRush.Smoke.Start";
    private const string PhaseKey = "NeonRush.Smoke.Phase";
    private const string FailedKey = "NeonRush.Smoke.Failed";
    private const string FailsKey = "NeonRush.Smoke.Fails";
    private const string CoinsKey = "NeonRush.Smoke.Coins";
    private const string DistKey = "NeonRush.Smoke.Dist";
    private const string FinishedKey = "NeonRush.Smoke.Finished";

    public static void Run()
    {
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(FailedKey, false);
        SessionState.SetString(FailsKey, "");
        SessionState.SetFloat(StartKey, -1f);
        SessionState.SetInt(PhaseKey, 0);
        SessionState.SetInt(CoinsKey, 0);
        SessionState.SetFloat(DistKey, 0f);

        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        Debug.Log("[SMOKE] Memulai tes di Play Mode...");
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
            Debug.Log("[SMOKE] Play Mode aktif, mulai memeriksa...");
        }

        double elapsed = now - start;
        int phase = SessionState.GetInt(PhaseKey, 0);

        if (elapsed > 40)
        {
            Fail("Timeout: smoke test tidak selesai.");
            Finish();
            return;
        }

        GameManager gm = GameManager.Instance;

        if (phase == 0 && elapsed > 2.5)
        {
            Check(gm != null, "GameManager.Instance tersedia");
            if (gm != null)
            {
                Check(gm.State == GameState.Playing, "State = Playing saat mulai");
                Check(gm.Speed > 8f, "Kecepatan naik (Speed > 8): " + gm.Speed);
                Check(gm.Distance > 3f, "Jarak bertambah: " + gm.Distance);
                Check(gm.Score > 0, "Skor bertambah dari jarak: " + gm.Score);
            }

            Check(Object.FindObjectsOfType<Coin>().Length > 0, "Koin ter-spawn di lintasan");
            Check(Object.FindObjectsOfType<Hazard>().Length > 0, "Rintangan ter-spawn di lintasan");
            Check(Object.FindObjectsOfType<TileSpawner>().Length == 1, "TileSpawner aktif");

            GameObject tiles = GameObject.Find("Tiles");
            Check(tiles != null && tiles.transform.childCount > 0, "Tile jalan ter-spawn");

            int coinsBefore = gm != null ? gm.Coins : 0;
            SessionState.SetInt(CoinsKey, coinsBefore);
            if (gm != null) gm.CollectCoin(10);
            SessionState.SetInt(PhaseKey, 1);
        }
        else if (phase == 1 && elapsed > 2.7)
        {
            if (gm != null)
            {
                Check(gm.Coins == SessionState.GetInt(CoinsKey, 0) + 10, "CollectCoin menambah 10 koin");
                Check(gm.State == GameState.Playing, "Masih Playing sebelum pause");
                gm.Pause();
            }
            SessionState.SetInt(PhaseKey, 2);
        }
        else if (phase == 2 && elapsed > 2.9)
        {
            if (gm != null)
            {
                Check(gm.State == GameState.Paused, "State = Paused setelah Pause()");
                Check(Mathf.Approximately(Time.timeScale, 0f), "Time.timeScale = 0 saat pause");
                SessionState.SetFloat(DistKey, gm.Distance);
            }
            SessionState.SetInt(PhaseKey, 3);
        }
        else if (phase == 3 && elapsed > 3.15)
        {
            if (gm != null)
            {
                gm.Resume();
                Check(gm.State == GameState.Playing, "State = Playing setelah Resume()");
                Check(gm.Distance <= SessionState.GetFloat(DistKey, 0f) + 0.5f, "Jarak berhenti selama pause");
                gm.EndGame();
            }
            SessionState.SetInt(PhaseKey, 4);
        }
        else if (phase == 4 && elapsed > 3.4)
        {
            if (gm != null)
            {
                Check(gm.State == GameState.GameOver, "State = GameOver setelah EndGame()");
                Check(gm.ScoreSystem != null, "ScoreManager tersedia");
                if (gm.ScoreSystem != null)
                    Check(gm.ScoreSystem.BestScore >= gm.Score, "Best score tersimpan (Best=" + gm.ScoreSystem.BestScore + ")");
            }
            Finish();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log("[SMOKE][OK] " + message);
            return;
        }

        SessionState.SetBool(FailedKey, true);
        SessionState.SetString(FailsKey, SessionState.GetString(FailsKey, "") + "- " + message + "\n");
        Debug.LogError("[SMOKE][FAIL] " + message);
    }

    private static void Fail(string message)
    {
        SessionState.SetBool(FailedKey, true);
        SessionState.SetString(FailsKey, SessionState.GetString(FailsKey, "") + "- " + message + "\n");
        Debug.LogError("[SMOKE][FAIL] " + message);
    }

    private static void Finish()
    {
        if (SessionState.GetBool(FinishedKey, false)) return;
        SessionState.SetBool(FinishedKey, true);
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Time.timeScale = 1f;
        Debug.Log("[SMOKE] Menutup Play Mode...");
        EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode) return;
        if (!SessionState.GetBool(FinishedKey, false)) return;

        SessionState.SetBool(FinishedKey, false);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        bool failed = SessionState.GetBool(FailedKey, false);
        if (failed)
        {
            Debug.LogError("SMOKE TEST GAGAL:\n" + SessionState.GetString(FailsKey, ""));
            EditorApplication.delayCall += () => EditorApplication.Exit(1);
            return;
        }

        Debug.Log("SMOKE TEST LULUS: spawn, skor, koin, kecepatan, pause, game over, best score.");
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }
}
