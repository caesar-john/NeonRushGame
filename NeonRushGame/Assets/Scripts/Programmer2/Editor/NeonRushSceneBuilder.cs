using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class NeonRushSceneBuilder
{
    private const string PrefabDir = "Assets/Prefabs";
    private const string MaterialDir = "Assets/Materials";
    private const string SceneDir = "Assets/Scenes";
    private const string AudioDir = "Assets/Resources/Audio";
    private const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";

    private static readonly Color NeonCyan = new Color(0.1f, 0.95f, 1f);
    private static readonly Color NeonMagenta = new Color(1f, 0.2f, 0.9f);
    private static readonly Color NeonGold = new Color(1f, 0.82f, 0.2f);
    private static readonly Color NeonRed = new Color(1f, 0.15f, 0.25f);
    private static readonly Color NeonOrange = new Color(1f, 0.5f, 0.1f);
    private static readonly Color DarkBg = new Color(0.015f, 0.018f, 0.035f);

    private static TMP_FontAsset font;
    private static Material roadMat;
    private static Material lineCyanMat;
    private static Material lineMagentaMat;
    private static Material laserMat;
    private static Material barrierMat;
    private static Material coinMat;
    private static Material playerMat;

    [MenuItem("Neon Rush/1. Import TMP Essentials")]
    public static void ImportTmpEssentials()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath) != null)
        {
            Debug.Log("[NeonRush] TMP Essential Resources sudah ada.");
            return;
        }

        string packagePath = FindTmpEssentialsPackage();
        if (packagePath == null)
        {
            Debug.LogError("[NeonRush] File TMP Essential Resources.unitypackage tidak ditemukan.");
            return;
        }

        AssetDatabase.ImportPackage(packagePath, false);
        AssetDatabase.Refresh();
        Debug.Log("[NeonRush] TMP Essential Resources diimpor dari: " + packagePath);
    }

    [MenuItem("Neon Rush/2. Build Scenes & Prefabs")]
    public static void Build()
    {
        EnsureFolders();
        EnsureTmpFont();
        EnsureLayer("Obstacle", out int obstacleLayer);

        CreateMaterials();
        AudioClip bgm = CreateMusic();
        AudioClip coin = CreateCoinSfx();
        AudioClip jump = CreateJumpSfx();
        AudioClip slide = CreateSlideSfx();
        AudioClip crash = CreateCrashSfx();
        AudioClip button = CreateButtonSfx();

        GameObject tilePrefab = CreateTilePrefab();
        GameObject laserPrefab = CreateLaserPrefab(obstacleLayer);
        GameObject barrierPrefab = CreateBarrierPrefab(obstacleLayer);
        GameObject coinPrefab = CreateCoinPrefab();

        BuildGameScene(tilePrefab, laserPrefab, barrierPrefab, coinPrefab, obstacleLayer, bgm, coin, jump, slide, crash, button);
        BuildMenuScene(bgm, coin, jump, slide, crash, button);

        EditorBuildSettings.scenes = BuildSceneList();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NeonRush] Selesai: prefab, material, audio, dan 2 scene berhasil dibuat.");
    }

    private static EditorBuildSettingsScene[] BuildSceneList()
    {
        System.Collections.Generic.List<EditorBuildSettingsScene> scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(SceneDir + "/MainMenu.unity", true),
            new EditorBuildSettingsScene(SceneDir + "/Game.unity", true)
        };

        string sandbox = SceneDir + "/Sandbox_PLayer.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(sandbox) != null)
            scenes.Add(new EditorBuildSettingsScene(sandbox, true));

        return scenes.ToArray();
    }

    public static void BuildFromCommandLine()
    {
        Build();
        EditorApplication.Exit(0);
    }

    private static string FindTmpEssentialsPackage()
    {
        if (!Directory.Exists("Library/PackageCache")) return null;

        string[] found = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
        return found.Length > 0 ? found[0] : null;
    }

    private static void EnsureTmpFont()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
        if (font != null) return;

        font = TMP_Settings.defaultFontAsset;
        if (font != null) return;

        ImportTmpEssentials();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
        if (font == null) font = TMP_Settings.defaultFontAsset;
        if (font == null) Debug.LogWarning("[NeonRush] Font TMP tidak ditemukan; teks UI bisa tampak kosong.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "Prefabs");
        CreateFolder("Assets", "Materials");
        CreateFolder("Assets", "Scenes");
        CreateFolder("Assets", "Resources");
        CreateFolder("Assets/Resources", "Audio");
    }

    private static void CreateFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void EnsureLayer(string name, out int index)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == name)
            {
                index = i;
                return;
            }
        }

        index = -1;
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = name;
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            Debug.LogError("[NeonRush] Tidak ada slot layer kosong untuk '" + name + "'.");
            index = 8;
            return;
        }

        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateMaterials()
    {
        roadMat = MakeMaterial("Road", new Color(0.06f, 0.07f, 0.12f), Color.black, 0f, 0.15f, 0.55f);
        lineCyanMat = MakeMaterial("NeonCyan", new Color(0.02f, 0.2f, 0.25f), NeonCyan, 3.5f, 0.1f, 0.8f);
        lineMagentaMat = MakeMaterial("NeonMagenta", new Color(0.25f, 0.02f, 0.2f), NeonMagenta, 3.5f, 0.1f, 0.8f);
        laserMat = MakeMaterial("Laser", new Color(0.4f, 0.02f, 0.05f), NeonRed, 5f, 0.1f, 0.9f);
        barrierMat = MakeMaterial("Barrier", new Color(0.3f, 0.1f, 0.02f), NeonOrange, 4f, 0.2f, 0.7f);
        coinMat = MakeMaterial("Coin", new Color(0.9f, 0.6f, 0.05f), NeonGold, 3f, 0.8f, 0.9f);
        playerMat = MakeMaterial("Player", new Color(0.05f, 0.25f, 0.35f), NeonCyan, 1.2f, 0.4f, 0.7f);
    }

    private static Material MakeMaterial(string name, Color color, Color emission, float emissionIntensity, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");

        Material mat = new Material(shader);
        mat.name = name;

        string materialPath = MaterialDir + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(materialPath) != null)
            AssetDatabase.DeleteAsset(materialPath);

        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (emissionIntensity > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission * emissionIntensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        AssetDatabase.CreateAsset(mat, materialPath);
        return mat;
    }

    private static GameObject CreateTilePrefab()
    {
        GameObject root = new GameObject("RoadTile");

        CreatePrimitive(PrimitiveType.Cube, "Road", root.transform, new Vector3(0f, -0.15f, 0f), new Vector3(9f, 0.3f, 30f), roadMat);

        CreatePrimitive(PrimitiveType.Cube, "LaneLineL", root.transform, new Vector3(-1.5f, 0.01f, 0f), new Vector3(0.06f, 0.02f, 30f), lineCyanMat);
        CreatePrimitive(PrimitiveType.Cube, "LaneLineR", root.transform, new Vector3(1.5f, 0.01f, 0f), new Vector3(0.06f, 0.02f, 30f), lineCyanMat);

        CreatePrimitive(PrimitiveType.Cube, "RailL", root.transform, new Vector3(-4.55f, 0.3f, 0f), new Vector3(0.15f, 0.6f, 30f), lineMagentaMat);
        CreatePrimitive(PrimitiveType.Cube, "RailR", root.transform, new Vector3(4.55f, 0.3f, 0f), new Vector3(0.15f, 0.6f, 30f), lineMagentaMat);

        for (int z = -10; z <= 10; z += 10)
            CreatePrimitive(PrimitiveType.Cube, "FloorStrip", root.transform, new Vector3(0f, 0.005f, z), new Vector3(9f, 0.02f, 0.08f), lineCyanMat);

        return SavePrefab(root, "RoadTile");
    }

    private static GameObject CreateLaserPrefab(int obstacleLayer)
    {
        GameObject root = new GameObject("Obstacle_Laser");
        root.layer = obstacleLayer;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 1f, 0f);
        collider.size = new Vector3(2.7f, 0.9f, 0.5f);

        root.AddComponent<Hazard>();

        CreatePrimitive(PrimitiveType.Cube, "Beam", root.transform, new Vector3(0f, 1f, 0f), new Vector3(2.6f, 0.12f, 0.15f), laserMat);
        CreatePrimitive(PrimitiveType.Cube, "PostL", root.transform, new Vector3(-1.28f, 0.6f, 0f), new Vector3(0.12f, 1.2f, 0.12f), laserMat);
        CreatePrimitive(PrimitiveType.Cube, "PostR", root.transform, new Vector3(1.28f, 0.6f, 0f), new Vector3(0.12f, 1.2f, 0.12f), laserMat);

        SetLayerRecursive(root, obstacleLayer);
        return SavePrefab(root, "Obstacle_Laser");
    }

    private static GameObject CreateBarrierPrefab(int obstacleLayer)
    {
        GameObject root = new GameObject("Obstacle_Barrier");
        root.layer = obstacleLayer;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 0.6f, 0f);
        collider.size = new Vector3(2.7f, 1.2f, 0.9f);

        root.AddComponent<Hazard>();

        CreatePrimitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(2.6f, 1.2f, 0.8f), barrierMat);
        CreatePrimitive(PrimitiveType.Cube, "TopStripe", root.transform, new Vector3(0f, 1.22f, 0f), new Vector3(2.62f, 0.08f, 0.82f), lineCyanMat);

        SetLayerRecursive(root, obstacleLayer);
        return SavePrefab(root, "Obstacle_Barrier");
    }

    private static GameObject CreateCoinPrefab()
    {
        GameObject root = new GameObject("Coin");

        SphereCollider collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        root.AddComponent<Coin>();

        GameObject visual = CreatePrimitive(PrimitiveType.Sphere, "Visual", root.transform, Vector3.zero, new Vector3(0.55f, 0.55f, 0.55f), coinMat);
        visual.transform.localRotation = Quaternion.identity;

        return SavePrefab(root, "Coin");
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

        return go;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }

    private static GameObject SavePrefab(GameObject root, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildGameScene(GameObject tilePrefab, GameObject laserPrefab, GameObject barrierPrefab, GameObject coinPrefab,
        int obstacleLayer, AudioClip bgm, AudioClip coin, AudioClip jump, AudioClip slide, AudioClip crash, AudioClip button)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        ConfigureLighting();

        Camera defaultCamera = Camera.main;
        if (defaultCamera != null) UnityEngine.Object.DestroyImmediate(defaultCamera.gameObject);

        GameObject gmObject = new GameObject("GameManager");
        GameManager gameManager = gmObject.AddComponent<GameManager>();
        DifficultyManager difficulty = gmObject.AddComponent<DifficultyManager>();
        ScoreManager score = gmObject.AddComponent<ScoreManager>();
        PoolManager pool = gmObject.AddComponent<PoolManager>();

        GameObject player = CreatePlayer();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = player.transform.position + new Vector3(0f, 4.5f, -8.5f);
        cameraObject.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = DarkBg;
        camera.farClipPlane = 400f;
        camera.fieldOfView = 62f;
        cameraObject.AddComponent<AudioListener>();
        AddUrpCameraData(cameraObject);

        FollowPlayer followPlayer = cameraObject.AddComponent<FollowPlayer>();
        followPlayer.player = player.transform;
        followPlayer.offset = new Vector3(0f, 4.5f, -8.5f);

        GameObject floorObject = new GameObject("FloorCollider", typeof(BoxCollider));
        floorObject.transform.position = new Vector3(0f, 0.3f, 120f);
        BoxCollider floorCollider = floorObject.GetComponent<BoxCollider>();
        floorCollider.size = new Vector3(9f, 0.4f, 600f);

        GameObject systemsObject = new GameObject("GameSystems");
        TileSpawner tileSpawner = systemsObject.AddComponent<TileSpawner>();
        ObstacleSpawner obstacleSpawner = systemsObject.AddComponent<ObstacleSpawner>();
        CoinSpawner coinSpawner = systemsObject.AddComponent<CoinSpawner>();

        GameObject tileParentObject = new GameObject("Tiles");
        tileParentObject.transform.SetParent(systemsObject.transform, false);

        GameObject audioObject = new GameObject("AudioManager");
        AudioManager audioManager = audioObject.AddComponent<AudioManager>();

        Canvas canvas = CreateCanvas("Canvas", out GameObject canvasObject);
        UIManager uiManager = canvasObject.AddComponent<UIManager>();
        HUDController hud = canvasObject.AddComponent<HUDController>();

        GameObject hudPanel = CreatePanel(canvas.transform, "HUDPanel", new Color(0f, 0f, 0f, 0f), false);
        TMP_Text scoreText = CreateText(hudPanel.transform, "ScoreText", "SKOR  0", 42f, TextAlignmentOptions.TopLeft, NeonCyan,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(520f, 64f));
        TMP_Text distanceText = CreateText(hudPanel.transform, "DistanceText", "0 m", 42f, TextAlignmentOptions.Top, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(420f, 64f));
        TMP_Text coinText = CreateText(hudPanel.transform, "CoinText", "KOIN  x0", 42f, TextAlignmentOptions.TopRight, NeonGold,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -26f), new Vector2(520f, 64f));

        GameObject pausePanel = CreatePanel(canvas.transform, "PausePanel", new Color(0f, 0f, 0f, 0.82f), true);
        CreateText(pausePanel.transform, "Title", "PAUSED", 96f, TextAlignmentOptions.Center, NeonCyan,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1000f, 140f));
        Button resumeButton = CreateButton(pausePanel.transform, "ResumeButton", "LANJUT", new Vector2(0f, 0f), uiManager.ResumeGame);
        CreateButton(pausePanel.transform, "PauseMenuButton", "MAIN MENU", new Vector2(0f, -110f), uiManager.GoToMainMenu);

        GameObject gameOverPanel = CreatePanel(canvas.transform, "GameOverPanel", new Color(0f, 0f, 0f, 0.85f), true);
        CreateText(gameOverPanel.transform, "Title", "GAME OVER", 100f, TextAlignmentOptions.Center, NeonRed,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1100f, 140f));
        TMP_Text finalScoreText = CreateText(gameOverPanel.transform, "FinalScore", "SKOR  0", 52f, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(900f, 70f));
        TMP_Text finalDistanceText = CreateText(gameOverPanel.transform, "FinalDistance", "0 m", 44f, TextAlignmentOptions.Center, NeonCyan,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(900f, 60f));
        TMP_Text finalCoinText = CreateText(gameOverPanel.transform, "FinalCoin", "KOIN  x0", 44f, TextAlignmentOptions.Center, NeonGold,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(900f, 60f));
        TMP_Text bestScoreText = CreateText(gameOverPanel.transform, "BestScore", "TERBAIK  0", 34f, TextAlignmentOptions.Center, NeonMagenta,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -75f), new Vector2(900f, 50f));
        CreateButton(gameOverPanel.transform, "RestartButton", "RESTART", new Vector2(0f, -190f), uiManager.RestartGame);
        CreateButton(gameOverPanel.transform, "GameOverMenuButton", "MAIN MENU", new Vector2(0f, -300f), uiManager.GoToMainMenu);

        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        CreateEventSystem();

        SetPrivateObject(gameManager, "player", player.transform);
        SetPrivateObject(gameManager, "difficulty", difficulty);
        SetPrivateObject(gameManager, "scoreManager", score);
        SetPrivateEnum(gameManager, "startState", 1);

        SetPrivateObject(difficulty, "player", player.transform);

        SetPrivateObject(tileSpawner, "player", player.transform);
        SetPrivateObjectArray(tileSpawner, "tilePrefabs", new UnityEngine.Object[] { tilePrefab });
        SetPrivateObject(tileSpawner, "tileParent", tileParentObject.transform);
        SetPrivateFloat(tileSpawner, "tileLength", 30f);
        SetPrivateInt(tileSpawner, "tilesAhead", 5);
        SetPrivateInt(tileSpawner, "tilesBehind", 2);

        SetPrivateObject(obstacleSpawner, "player", player.transform);
        SetPrivateFloat(obstacleSpawner, "laneWidth", 3f);
        SetPrivateInt(obstacleSpawner, "maxBlockedLanes", 2);
        SetPrivateFloat(obstacleSpawner, "firstObstacleDistance", 35f);
        SetPrivateFloat(obstacleSpawner, "spawnAheadDistance", 90f);
        SetPrivateFloat(obstacleSpawner, "minGap", 18f);
        SetPrivateFloat(obstacleSpawner, "maxGap", 32f);
        SetPrivateObstacleTypes(obstacleSpawner, new[] { laserPrefab, barrierPrefab });

        SetPrivateObject(coinSpawner, "player", player.transform);
        SetPrivateObject(coinSpawner, "coinPrefab", coinPrefab);
        SetPrivateInt(coinSpawner, "obstacleLayerMask", 1 << obstacleLayer);
        SetPrivateFloat(coinSpawner, "laneWidth", 3f);
        SetPrivateFloat(coinSpawner, "coinHeight", 1.1f);
        SetPrivateIntArray(coinSpawner, "patterns", 3);
        SetPrivatePattern(coinSpawner, 0, 0, 1f);
        SetPrivatePattern(coinSpawner, 1, 1, 1f);
        SetPrivatePattern(coinSpawner, 2, 2, 1f);
        SetPrivateInt(coinSpawner, "minCoins", 5);
        SetPrivateInt(coinSpawner, "maxCoins", 8);
        SetPrivateFloat(coinSpawner, "coinSpacing", 2.5f);
        SetPrivateFloat(coinSpawner, "firstPatternDistance", 25f);
        SetPrivateFloat(coinSpawner, "spawnAheadDistance", 80f);

        SetPrivateObject(hud, "scoreText", scoreText);
        SetPrivateObject(hud, "distanceText", distanceText);
        SetPrivateObject(hud, "coinText", coinText);
        SetPrivateObject(hud, "finalScoreText", finalScoreText);
        SetPrivateObject(hud, "finalDistanceText", finalDistanceText);
        SetPrivateObject(hud, "finalCoinText", finalCoinText);
        SetPrivateObject(hud, "bestScoreText", bestScoreText);

        SetPrivateObject(uiManager, "mainMenuPanel", null);
        SetPrivateObject(uiManager, "hudPanel", hudPanel);
        SetPrivateObject(uiManager, "pausePanel", pausePanel);
        SetPrivateObject(uiManager, "gameOverPanel", gameOverPanel);
        SetPrivateBool(uiManager, "useSeparateScenes", true);
        SetPrivateString(uiManager, "mainMenuScene", "MainMenu");
        SetPrivateString(uiManager, "gameScene", "Game");

        SetPrivateObject(audioManager, "bgmClip", bgm);
        SetPrivateObject(audioManager, "coinClip", coin);
        SetPrivateObject(audioManager, "jumpClip", jump);
        SetPrivateObject(audioManager, "slideClip", slide);
        SetPrivateObject(audioManager, "crashClip", crash);
        SetPrivateObject(audioManager, "buttonClip", button);
        SetPrivateFloat(audioManager, "bgmVolume", 0.35f);
        SetPrivateFloat(audioManager, "sfxVolume", 0.9f);
        SetPrivateBool(audioManager, "dontDestroyOnLoad", true);

        EditorSceneManager.SaveScene(scene, SceneDir + "/Game.unity");
    }

    private static void BuildMenuScene(AudioClip bgm, AudioClip coin, AudioClip jump, AudioClip slide, AudioClip crash, AudioClip button)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        ConfigureLighting();

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DarkBg;
            camera.transform.position = new Vector3(0f, 4.5f, -16f);
            camera.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
            camera.farClipPlane = 400f;
            camera.fieldOfView = 60f;
        }

        BuildMenuBackdrop();

        GameObject audioObject = new GameObject("AudioManager");
        AudioManager audioManager = audioObject.AddComponent<AudioManager>();
        SetPrivateObject(audioManager, "bgmClip", bgm);
        SetPrivateObject(audioManager, "coinClip", coin);
        SetPrivateObject(audioManager, "jumpClip", jump);
        SetPrivateObject(audioManager, "slideClip", slide);
        SetPrivateObject(audioManager, "crashClip", crash);
        SetPrivateObject(audioManager, "buttonClip", button);
        SetPrivateFloat(audioManager, "bgmVolume", 0.35f);
        SetPrivateFloat(audioManager, "sfxVolume", 0.9f);
        SetPrivateBool(audioManager, "dontDestroyOnLoad", true);

        Canvas canvas = CreateCanvas("Canvas", out GameObject canvasObject);
        UIManager uiManager = canvasObject.AddComponent<UIManager>();

        GameObject menuPanel = CreatePanel(canvas.transform, "MenuPanel", new Color(0f, 0f, 0f, 0.35f), true);

        CreateText(menuPanel.transform, "Title", "NEON RUSH", 150f, TextAlignmentOptions.Center, NeonCyan,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(1600f, 200f));
        CreateText(menuPanel.transform, "Subtitle", "ENDLESS RUNNER  •  NEON CITY 2095", 38f, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1400f, 60f));
        CreateText(menuPanel.transform, "Sdg", "SDG 11  •  SDG 7  •  SDG 9", 30f, TextAlignmentOptions.Center, NeonMagenta,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1200f, 50f));

        CreateButton(menuPanel.transform, "PlayButton", "PLAY", new Vector2(0f, -90f), uiManager.PlayGame);
        CreateButton(menuPanel.transform, "QuitButton", "KELUAR", new Vector2(0f, -200f), uiManager.QuitGame);

        CreateText(menuPanel.transform, "Controls",
            "A / D  pindah jalur     SPASI  lompat     S  slide     ESC  pause", 26f,
            TextAlignmentOptions.Center, new Color(0.75f, 0.8f, 0.9f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1600f, 40f));

        CreateEventSystem();

        SetPrivateObject(uiManager, "mainMenuPanel", menuPanel);
        SetPrivateObject(uiManager, "hudPanel", null);
        SetPrivateObject(uiManager, "pausePanel", null);
        SetPrivateObject(uiManager, "gameOverPanel", null);
        SetPrivateBool(uiManager, "useSeparateScenes", true);
        SetPrivateString(uiManager, "mainMenuScene", "MainMenu");
        SetPrivateString(uiManager, "gameScene", "Game");

        EditorSceneManager.SaveScene(scene, SceneDir + "/MainMenu.unity");
    }

    private static void BuildMenuBackdrop()
    {
        GameObject root = new GameObject("Backdrop");

        CreatePrimitive(PrimitiveType.Cube, "Road", root.transform, new Vector3(0f, -0.15f, 80f), new Vector3(9f, 0.3f, 220f), roadMat);
        CreatePrimitive(PrimitiveType.Cube, "LineL", root.transform, new Vector3(-1.5f, 0.01f, 80f), new Vector3(0.06f, 0.02f, 220f), lineCyanMat);
        CreatePrimitive(PrimitiveType.Cube, "LineR", root.transform, new Vector3(1.5f, 0.01f, 80f), new Vector3(0.06f, 0.02f, 220f), lineCyanMat);
        CreatePrimitive(PrimitiveType.Cube, "RailL", root.transform, new Vector3(-4.55f, 0.4f, 80f), new Vector3(0.15f, 0.8f, 220f), lineMagentaMat);
        CreatePrimitive(PrimitiveType.Cube, "RailR", root.transform, new Vector3(4.55f, 0.4f, 80f), new Vector3(0.15f, 0.8f, 220f), lineMagentaMat);

        for (int i = 0; i < 8; i++)
        {
            float z = 5f + i * 20f;
            Material mat = (i % 2 == 0) ? lineCyanMat : lineMagentaMat;
            CreatePrimitive(PrimitiveType.Cube, "PillarL", root.transform, new Vector3(-5.6f, 2f, z), new Vector3(0.35f, 4f, 0.35f), mat);
            CreatePrimitive(PrimitiveType.Cube, "PillarR", root.transform, new Vector3(5.6f, 2f, z), new Vector3(0.35f, 4f, 0.35f), mat);
            CreatePrimitive(PrimitiveType.Cube, "Arch", root.transform, new Vector3(0f, 4f, z), new Vector3(11.5f, 0.25f, 0.25f), mat);
        }

        GameObject lightA = new GameObject("NeonLightCyan");
        lightA.transform.position = new Vector3(-4f, 3.5f, 18f);
        Light la = lightA.AddComponent<Light>();
        la.type = LightType.Point;
        la.color = NeonCyan;
        la.range = 28f;
        la.intensity = 6f;

        GameObject lightB = new GameObject("NeonLightMagenta");
        lightB.transform.position = new Vector3(4f, 3.5f, 42f);
        Light lb = lightB.AddComponent<Light>();
        lb.type = LightType.Point;
        lb.color = NeonMagenta;
        lb.range = 28f;
        lb.intensity = 6f;
    }

    private static Canvas CreateCanvas(string name, out GameObject canvasObject)
    {
        canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color, bool blockRaycast)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = blockRaycast;

        return go;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment,
        Color color, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        if (font != null) text.font = font;

        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityAction action)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(440f, 86f);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.05f, 0.09f, 0.15f, 0.95f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = NeonCyan;
        colors.pressedColor = NeonMagenta;
        colors.selectedColor = NeonCyan;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText(go.transform, "Label", label, 38f, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 86f));

        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);

        return button;
    }

    private static GameObject CreatePlayer()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject player;

        if (prefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.transform.rotation = Quaternion.identity;
        }
        else
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
            player.transform.position = new Vector3(0f, 1f, 0f);

            Renderer renderer = player.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = playerMat;

            Rigidbody body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            player.AddComponent<PlayerAutoRunner>();
        }

        if (player.GetComponent<PlayerSpeedBridge>() == null)
            player.AddComponent<PlayerSpeedBridge>();

        return player;
    }

    private static void AddUrpCameraData(GameObject cameraObject)
    {
        System.Type urpType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

        if (urpType == null)
        {
            foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                urpType = assembly.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
                if (urpType != null) break;
            }
        }

        if (urpType != null && cameraObject.GetComponent(urpType) == null)
            cameraObject.AddComponent(urpType);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void ConfigureLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.09f, 0.1f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.015f, 0.02f, 0.045f);
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 190f;

        Light sun = UnityEngine.Object.FindObjectOfType<Light>();
        if (sun != null)
        {
            sun.type = LightType.Directional;
            sun.color = new Color(0.65f, 0.8f, 1f);
            sun.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            sun.shadows = LightShadows.Soft;
        }
    }

    private static void SetPrivateObject(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateObjectArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateObstacleTypes(ObstacleSpawner target, GameObject[] prefabs)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty array = so.FindProperty("obstacleTypes");
        if (array == null) { Debug.LogWarning("[NeonRush] obstacleTypes tidak ditemukan."); return; }
        array.arraySize = prefabs.Length;

        for (int i = 0; i < prefabs.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
            element.FindPropertyRelative("weight").floatValue = 1f;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateIntArray(UnityEngine.Object target, string field, int size)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.arraySize = size;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivatePattern(UnityEngine.Object target, int index, int pattern, float weight)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty("patterns");
        if (property == null || index >= property.arraySize) return;
        SerializedProperty element = property.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("pattern").enumValueIndex = pattern;
        element.FindPropertyRelative("weight").floatValue = weight;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateFloat(UnityEngine.Object target, string field, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateInt(UnityEngine.Object target, string field, int value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateBool(UnityEngine.Object target, string field, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateString(UnityEngine.Object target, string field, string value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateEnum(UnityEngine.Object target, string field, int value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogWarning("[NeonRush] Field tidak ditemukan: " + field); return; }
        property.enumValueIndex = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private const int SampleRate = 44100;

    private static AudioClip CreateCoinSfx()
    {
        int count = (int)(SampleRate * 0.18f);
        float[] samples = new float[count];
        double phase = 0;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float frequency = t < 0.07f ? 1318.5f : 1760f;
            phase += 2.0 * Math.PI * frequency / SampleRate;
            float envelope = Mathf.Exp(-t * 16f) * Mathf.Min(1f, t * 400f);
            samples[i] = (float)Math.Sin(phase) * envelope * 0.45f;
        }

        return SaveWav("Coin", samples);
    }

    private static AudioClip CreateJumpSfx()
    {
        int count = (int)(SampleRate * 0.22f);
        float[] samples = new float[count];
        double phase = 0;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float frequency = Mathf.Lerp(320f, 940f, t / 0.22f);
            phase += 2.0 * Math.PI * frequency / SampleRate;
            float envelope = Mathf.Exp(-t * 9f) * Mathf.Min(1f, t * 300f);
            samples[i] = (float)Math.Sin(phase) * envelope * 0.5f;
        }

        return SaveWav("Jump", samples);
    }

    private static AudioClip CreateSlideSfx()
    {
        int count = (int)(SampleRate * 0.3f);
        float[] samples = new float[count];
        System.Random random = new System.Random(7);
        float lowPass = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            lowPass += (noise - lowPass) * 0.08f;
            float envelope = Mathf.Exp(-t * 8f) * Mathf.Min(1f, t * 60f);
            samples[i] = lowPass * envelope * 0.7f;
        }

        return SaveWav("Slide", samples);
    }

    private static AudioClip CreateCrashSfx()
    {
        int count = (int)(SampleRate * 0.5f);
        float[] samples = new float[count];
        System.Random random = new System.Random(13);
        double phase = 0;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            phase += 2.0 * Math.PI * 70.0 / SampleRate;
            float envelope = Mathf.Exp(-t * 6f);
            samples[i] = (noise * 0.55f + (float)Math.Sin(phase) * 0.45f) * envelope * 0.75f;
        }

        return SaveWav("Crash", samples);
    }

    private static AudioClip CreateButtonSfx()
    {
        int count = (int)(SampleRate * 0.07f);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float envelope = Mathf.Exp(-t * 60f) * Mathf.Min(1f, t * 800f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * 1000f * t) * envelope * 0.35f;
        }

        return SaveWav("Button", samples);
    }

    private static AudioClip CreateMusic()
    {
        float[] roots = { 110f, 87.31f, 130.81f, 98f };
        float[][] chords =
        {
            new[] { 110f, 130.81f, 164.81f },
            new[] { 87.31f, 110f, 130.81f },
            new[] { 130.81f, 164.81f, 196f },
            new[] { 98f, 123.47f, 146.83f }
        };

        const float barLength = 2f;
        int total = (int)(SampleRate * barLength * 4);
        float[] samples = new float[total];

        for (int bar = 0; bar < 4; bar++)
        {
            int start = (int)(bar * barLength * SampleRate);
            int length = (int)(barLength * SampleRate);
            float[] chord = chords[bar];

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float barEnvelope = Mathf.Min(1f, t / 0.04f) * Mathf.Min(1f, (barLength - t) / 0.04f);

                float pad = 0f;
                for (int c = 0; c < chord.Length; c++)
                    pad += Mathf.Sin(2f * Mathf.PI * chord[c] * t) * 0.035f;

                float stepLength = 0.25f;
                int step = (int)(t / stepLength);
                float noteTime = t - step * stepLength;
                float[] arpNotes = { chord[0] * 2f, chord[1] * 2f, chord[2] * 2f, chord[1] * 2f };
                float note = arpNotes[step % 4];
                float noteEnvelope = Mathf.Exp(-noteTime * 18f) * Mathf.Min(1f, noteTime * 400f);
                float arp = Mathf.Sin(2f * Mathf.PI * note * t) * noteEnvelope * 0.05f;

                float bass = Mathf.Sin(2f * Mathf.PI * roots[bar] * 0.5f * t) * 0.055f;

                samples[start + i] = (pad + arp + bass) * barEnvelope;
            }
        }

        float peak = 0.0001f;
        for (int i = 0; i < samples.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
        float gain = 0.7f / peak;
        for (int i = 0; i < samples.Length; i++) samples[i] *= gain;

        return SaveWav("Bgm", samples);
    }

    private static AudioClip SaveWav(string fileName, float[] samples)
    {
        string path = AudioDir + "/" + fileName + ".wav";
        int dataSize = samples.Length * 2;

        using (FileStream stream = new FileStream(path, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                writer.Write(value);
            }
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
