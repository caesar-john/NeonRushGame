using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Smoke test otomatis untuk map desainer (scene "Game"): memastikan adapter
// memasang kamera chase, koin bisa diambil, map terus tersambung, lalu pause,
// game over, dan restart berjalan tanpa error.
public static class NeonRushSmokeTest
{
    private const string ActiveKey = "NeonRush.Smoke.Active";
    private const string StartKey = "NeonRush.Smoke.Start";
    private const string PhaseKey = "NeonRush.Smoke.Phase";
    private const string FailedKey = "NeonRush.Smoke.Failed";
    private const string FailsKey = "NeonRush.Smoke.Fails";
    private const string CoinsKey = "NeonRush.Smoke.Coins";
    private const string DistKey = "NeonRush.Smoke.Dist";
    private const string SegKey = "NeonRush.Smoke.Segments";
    private const string ExceptionsKey = "NeonRush.Smoke.Exceptions";
    private const string AudioWarningsKey = "NeonRush.Smoke.AudioWarnings";
    private const string MissingScriptKey = "NeonRush.Smoke.MissingScripts";
    private const string BestBeforeKey = "NeonRush.Smoke.BestBefore";
    private const string FinishedKey = "NeonRush.Smoke.Finished";

    private const string ScenePath = "Assets/Scenes/Game.unity";
    private const string SceneName = "Game";
    private const float Timeout = 45f;

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
        SessionState.SetFloat(SegKey, 0f);
        SessionState.SetString(ExceptionsKey, "");
        SessionState.SetInt(AudioWarningsKey, 0);
        SessionState.SetInt(MissingScriptKey, 0);
        SessionState.SetInt(BestBeforeKey, 0);

        EditorSceneManager.OpenScene(ScenePath);
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
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (message == null) return;
        if (message.StartsWith("[SMOKE]")) return;

        if (type == LogType.Exception)
        {
            SessionState.SetString(ExceptionsKey, SessionState.GetString(ExceptionsKey, "") + message + "\n");
            return;
        }

        if (message.Contains("audio listeners"))
        {
            SessionState.SetInt(AudioWarningsKey, SessionState.GetInt(AudioWarningsKey, 0) + 1);
            return;
        }

        if (message.Contains("referenced script") && message.Contains("is missing"))
        {
            SessionState.SetInt(MissingScriptKey, SessionState.GetInt(MissingScriptKey, 0) + 1);
        }
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

        if (elapsed > Timeout)
        {
            Fail("Timeout: smoke test tidak selesai.");
            Finish();
            return;
        }

        GameManager gm = GameManager.Instance;

        if (phase == 0 && elapsed > 2.0)
        {
            CheckStartup(gm);

            if (gm != null && gm.Player != null)
            {
                SessionState.SetInt(CoinsKey, gm.Coins);
                TeleportToNearestCoin(gm.Player);
            }

            SessionState.SetInt(PhaseKey, 1);
        }
        else if (phase == 1 && elapsed > 2.6)
        {
            if (gm != null)
            {
                int before = SessionState.GetInt(CoinsKey, 0);
                Check(gm.Coins > before, "Koin terambil otomatis saat pemain menyentuh (koin " + before + " -> " + gm.Coins + ")");
            }

            if (gm != null && gm.Player != null) TeleportToNearestHazard(gm.Player);
            SessionState.SetInt(PhaseKey, 2);
        }
        else if (phase == 2 && elapsed > 3.2)
        {
            if (gm != null)
            {
                Check(gm.State == GameState.GameOver, "Hazard memicu Game Over: " + gm.State);
                Check(Mathf.Approximately(Time.timeScale, 0f), "Time.timeScale = 0 setelah menabrak hazard");

                gm.Restart();
            }

            SessionState.SetInt(PhaseKey, 3);
        }
        else if (phase == 3 && elapsed > 4.0)
        {
            DestroyHazards();

            gm = GameManager.Instance;
            Check(gm != null, "GameManager tersedia setelah restart dari Game Over hazard");
            if (gm != null)
            {
                Check(gm.State == GameState.Playing, "State = Playing setelah restart dari Game Over hazard");

                CheckTiling();

                if (gm.Player != null)
                {
                    SessionState.SetFloat(DistKey, gm.Distance);
                    gm.Player.position += Vector3.left * 2000f;
                }
            }

            SessionState.SetInt(PhaseKey, 4);
        }
        else if (phase == 4 && elapsed > 4.8)
        {
            CheckSegmentsMoved();

            if (gm != null)
            {
                SessionState.SetFloat(DistKey, gm.Distance);
                gm.Pause();
            }

            SessionState.SetInt(PhaseKey, 5);
        }
        else if (phase == 5 && elapsed > 5.3)
        {
            if (gm != null)
            {
                Check(gm.State == GameState.Paused, "State = Paused setelah Pause()");
                Check(Mathf.Approximately(Time.timeScale, 0f), "Time.timeScale = 0 saat pause");
                Check(gm.Distance <= SessionState.GetFloat(DistKey, 0f) + 0.5f, "Jarak berhenti selama pause");

                gm.Resume();
                Check(gm.State == GameState.Playing, "State = Playing setelah Resume()");
                gm.EndGame();
            }

            SessionState.SetInt(PhaseKey, 6);
        }
        else if (phase == 6 && elapsed > 5.8)
        {
            if (gm != null)
            {
                Check(gm.State == GameState.GameOver, "State = GameOver setelah EndGame()");
                Check(gm.ScoreSystem != null, "ScoreManager tersedia");
                if (gm.ScoreSystem != null)
                    Check(gm.ScoreSystem.BestScore >= gm.Score, "Best score tersimpan: " + gm.ScoreSystem.BestScore);
            }

            if (gm != null) gm.Restart();
            SessionState.SetInt(PhaseKey, 7);
        }
        else if (phase == 7 && elapsed > 7.1)
        {
            GameManager fresh = GameManager.Instance;
            Check(fresh != null, "GameManager tersedia setelah restart final");
            if (fresh != null)
            {
                Check(fresh.State == GameState.Playing, "State = Playing setelah restart final");
                Check(fresh.Coins <= 2, "Koin kembali kecil setelah restart: " + fresh.Coins);
                Check(fresh.Score < 50, "Skor kembali kecil setelah restart: " + fresh.Score);
            }
            Check(Mathf.Approximately(Time.timeScale, 1f), "Time.timeScale = 1 setelah restart final");
            Check(AdapterCount() == 1, "Tepat satu NeonGameSceneAdapter setelah restart: " + AdapterCount());
            Check(PickupCount() > 0, "Koin dipasang lagi setelah restart: " + PickupCount());
            Check(HazardCount() > 0, "Hazard dipasang lagi setelah restart: " + HazardCount());

            string exceptions = SessionState.GetString(ExceptionsKey, "");
            Check(string.IsNullOrEmpty(exceptions), "Tidak ada exception selama tes" + (string.IsNullOrEmpty(exceptions) ? "" : ": " + exceptions));
            Check(SessionState.GetInt(AudioWarningsKey, 0) == 0, "Tidak ada peringatan '2 audio listeners'");

            int missingScripts = SessionState.GetInt(MissingScriptKey, 0);
            if (missingScripts > 0)
                Debug.LogWarning("[SMOKE][INFO] Ada " + missingScripts + " warning skrip hilang dari aset map desainer (bukan error).");

            SessionState.SetInt(PhaseKey, 6);
            Finish();
        }
    }

    private static void CheckStartup(GameManager gm)
    {
        Check(gm != null, "GameManager.Instance tersedia");
        if (gm != null)
        {
            Check(gm.State == GameState.Playing, "State = Playing saat mulai");
            Check(gm.Player != null, "Player terdeteksi");
            Check(gm.Distance > 3f, "Jarak bertambah: " + gm.Distance);
            Check(gm.Score > 0, "Skor bertambah dari jarak: " + gm.Score);
        }

        Check(AdapterCount() == 1, "NeonGameSceneAdapter aktif: " + AdapterCount());
        Check(SceneManager.GetSceneByName("GameNeonCity").isLoaded == false, "Scene lama GameNeonCity tidak ikut termuat");

        int pickups = PickupCount();
        Check(pickups > 300, "Koin map desainer dikonversi: " + pickups);
        Check(SegmentCount() >= 3, "Map diduplikasi untuk lintasan: " + SegmentCount() + " segmen");
        Check(HazardCount() >= 6, "Hazard neon terpasang: " + HazardCount());

        TileSpawner[] spawners = Object.FindObjectsByType<TileSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int enabledSpawners = 0;
        for (int i = 0; i < spawners.Length; i++)
            if (spawners[i] != null && spawners[i].enabled) enabledSpawners++;
        Check(enabledSpawners == 0, "Spawner lama dimatikan");

        GameObject tiles = GameObject.Find("Tiles");
        Check(tiles == null || !tiles.activeInHierarchy, "Tile lama disembunyikan");

        Check(AudioListenerCount() == 1, "Tepat satu AudioListener aktif: " + AudioListenerCount());
        if (gm != null && gm.Player != null)
            Check(gm.Player.GetComponent<NeonSlideSfx>() != null, "Bridge SFX geser terpasang");
        Check(HasMenuButton("PausePanel"), "Tombol MENU tersedia di PausePanel");
        Check(HasMenuButton("GameOverPanel"), "Tombol MENU tersedia di GameOverPanel");

        NeonRunnerCamera rig = Object.FindFirstObjectByType<NeonRunnerCamera>();
        Check(rig != null, "Kamera chase terpasang");
        if (rig != null && gm != null && gm.Player != null)
        {
            Camera cam = rig.GetComponent<Camera>();
            if (cam != null)
            {
                Vector3 viewport = cam.WorldToViewportPoint(gm.Player.position);
                Check(Mathf.Abs(viewport.x - 0.5f) < 0.12f, "Pemain di tengah layar X: " + viewport.x.ToString("0.00"));
                Check(viewport.z > 5f, "Kamera melihat ke depan pemain: " + viewport.z.ToString("0.0"));
            }
        }
    }

    private static void TeleportToNearestCoin(Transform player)
    {
        NeonCoinPickup[] coins = Object.FindObjectsByType<NeonCoinPickup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        NeonCoinPickup nearest = null;
        float best = float.PositiveInfinity;
        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i] == null) continue;
            float distance = (coins[i].transform.position - player.position).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            nearest = coins[i];
        }

        if (nearest == null) return;
        Vector3 target = nearest.transform.position;
        player.position = new Vector3(target.x, player.position.y, target.z);
    }

    private static void TeleportToNearestHazard(Transform player)
    {
        Hazard[] hazards = Object.FindObjectsByType<Hazard>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Hazard nearest = null;
        float best = float.PositiveInfinity;
        for (int i = 0; i < hazards.Length; i++)
        {
            if (hazards[i] == null) continue;
            float distance = (hazards[i].transform.position - player.position).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            nearest = hazards[i];
        }

        if (nearest == null) return;

        MonoBehaviour[] behaviours = player.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;
            string type = behaviours[i].GetType().Name;
            if (type == "PlayerMovement" || type == "PlayerJump") behaviours[i].enabled = false;
        }

        player.position = nearest.transform.position;
    }

    private static int HazardCount()
    {
        Hazard[] hazards = Object.FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < hazards.Length; i++)
            if (hazards[i] != null) count++;
        return count;
    }

    private static void DestroyHazards()
    {
        Hazard[] hazards = Object.FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < hazards.Length; i++)
            if (hazards[i] != null) Object.Destroy(hazards[i].gameObject);
    }

    private static void CheckTiling()
    {
        float sum = SegmentPositionSum();
        Check(Mathf.Abs(sum) > 0.01f, "Segmen map punya posisi: " + sum.ToString("0.0"));
        SessionState.SetFloat(SegKey, sum);
    }

    private static void CheckSegmentsMoved()
    {
        float before = SessionState.GetFloat(SegKey, 0f);
        float now = SegmentPositionSum();
        Check(Mathf.Abs(now - before) > 1f, "Map didaur ulang saat pemain maju jauh (" + before.ToString("0.0") + " -> " + now.ToString("0.0") + ")");
    }

    private static bool HasMenuButton(string panelName)
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded) return false;

        GameObject[] roots = scene.GetRootGameObjects();
        Transform panel = null;
        for (int i = 0; i < roots.Length && panel == null; i++)
        {
            if (roots[i] == null) continue;
            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j] != null && children[j].name == panelName)
                {
                    panel = children[j];
                    break;
                }
            }
        }

        if (panel == null) return false;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            if (buttons[i].name.EndsWith("MenuButton")) return true;

            int calls = buttons[i].onClick.GetPersistentEventCount();
            for (int c = 0; c < calls; c++)
            {
                if (buttons[i].onClick.GetPersistentMethodName(c) == "GoToMainMenu") return true;
            }
        }

        return false;
    }

    private static int AdapterCount()
    {
        NeonGameSceneAdapter[] adapters = Object.FindObjectsByType<NeonGameSceneAdapter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < adapters.Length; i++)
            if (adapters[i] != null) count++;
        return count;
    }

    private static int PickupCount()
    {
        NeonCoinPickup[] pickups = Object.FindObjectsByType<NeonCoinPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < pickups.Length; i++)
            if (pickups[i] != null) count++;
        return count;
    }

    private static int SegmentCount()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded) return 0;

        int count = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i] != null && roots[i].name.StartsWith("Environment")) count++;
        return count;
    }

    private static float SegmentPositionSum()
    {
        Scene scene = SceneManager.GetSceneByName(SceneName);
        if (!scene.IsValid() || !scene.isLoaded) return 0f;

        float sum = 0f;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            if (!roots[i].name.StartsWith("Environment")) continue;
            sum += roots[i].transform.position.x;
        }
        return sum;
    }

    private static int AudioListenerCount()
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == null) continue;
            if (!listeners[i].enabled) continue;
            if (!listeners[i].gameObject.activeInHierarchy) continue;
            count++;
        }
        return count;
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
        Application.logMessageReceived -= OnLog;
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

        PlayerPrefs.SetInt("NeonRush_BestScore", SessionState.GetInt(BestBeforeKey, 0));
        PlayerPrefs.Save();

        bool failed = SessionState.GetBool(FailedKey, false);
        if (failed)
        {
            Debug.LogError("SMOKE TEST GAGAL:\n" + SessionState.GetString(FailsKey, ""));
            EditorApplication.delayCall += () => EditorApplication.Exit(1);
            return;
        }

        Debug.Log("SMOKE TEST LULUS: kamera, koin, tiling, pause, game over, restart.");
        EditorApplication.delayCall += () => EditorApplication.Exit(0);
    }
}
