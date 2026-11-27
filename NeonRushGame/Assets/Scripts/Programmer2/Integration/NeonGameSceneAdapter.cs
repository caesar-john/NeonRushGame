using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Map desainer di scene "Game" berjalan di sumbu X negatif. Di scene itu
// penanda koin masih berupa GameObject kosong, lantai masih memakai ukuran
// map lama (+Z), dan TileSpawner lama masih aktif. Skrip ini merapikan
// semuanya saat runtime tanpa mengubah scene milik rekan.
[AddComponentMenu("Neon Rush/Integration/Game Scene Adapter")]
public class NeonGameSceneAdapter : MonoBehaviour
{
    private const string SceneName = "Game";
    private const string LegacySceneName = "GameNeonCity";
    private const string EnvironmentName = "Environment";
    private const string CoinMarkerName = "coin";
    private const string CoinVisualPrefix = "Coin_Gold_Lightning";
    private const string FloorName = "FloorCollider";
    private const string TilesName = "Tiles";

    private const float CoinHeight = 1.6f;
    private const float CoinSkipSpawnSqr = 9f;
    private const float CoinCollectRadius = 5f;
    private const float PlayerJumpForce = 10f;

    private const float FloorLength = 20000f;
    private const float FloorWidth = 90f;
    private const float FloorThickness = 0.4f;
    private const float FloorCenterY = 0.3f;

    private const string HazardName = "NeonHazard";
    private const float HazardStartOffset = 120f;
    private const float HazardSpacing = 70f;
    private const float HazardHeight = 2.6f;
    private const float HazardLength = 4f;
    private const float HazardDepth = 8f;
    private const float HazardLaneDistance = 12f;

    private const int SegmentCopies = 2;
    private const float SegmentBehindBuffer = 40f;

    private sealed class Segment
    {
        public Transform root;
        public float minX;
        public float maxX;
    }

    private static bool bootstrapped;

    private readonly List<Segment> segments = new List<Segment>();
    private float segmentLength;
    private int rearIndex;
    private int frontIndex;
    private Transform followPlayer;
    private bool tiling;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (bootstrapped) return;
        bootstrapped = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (scene.name != SceneName) return;
        if (FindAnyObjectByType<NeonGameSceneAdapter>() != null) return;

        GameObject host = new GameObject("NeonGameSceneAdapter");
        // Kalau scene Game dimuat additif dari scene lain, GameObject baru
        // otomatis masuk ke scene aktif. Pindahkan ke scene Game agar tidak
        // ikut hancur saat scene lama dibuang.
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<NeonGameSceneAdapter>();
    }

    private void Awake()
    {
        // Dijalankan segera saat scene Game termuat, sebelum alur lama sempat
        // mengubah kamera/lantai (urutan Start bisa lebih lambat).
        TakeOver();
        DisableLegacySpawners(gameObject.scene);
    }

    private void Start()
    {
        // Semua pencarian objek dibatasi ke scene Game. GameObject.Find global
        // bisa mengambil objek milik scene GameNeonCity yang sedang di-unload,
        // sehingga koin/map yang dipakai justru map lama yang lalu hancur.
        Scene scene = gameObject.scene;

        GameObject environment = FindSceneObject(scene, EnvironmentName);
        if (environment == null)
        {
            Debug.LogWarning("[NeonGameSceneAdapter] Root 'Environment' tidak ditemukan di scene " + SceneName + ".", this);
            return;
        }

        EnsureFloor(scene);
        RunProgress.SetAxis(0);

        followPlayer = ResolvePlayer();
        Vector3 spawnPoint = followPlayer != null ? followPlayer.position : Vector3.zero;

        if (followPlayer != null)
        {
            NeonRunnerCamera.Install(followPlayer);
            TunePlayer(followPlayer);

            // Pasang efek suara kejadian pemain tanpa mengubah skrip milik P1.
            if (followPlayer.GetComponent<NeonPlayerSfx>() == null)
                followPlayer.gameObject.AddComponent<NeonPlayerSfx>();

            // SFX geser menunggu status slide dari skrip P1 (tanpa membuat mekanik slide).
            if (followPlayer.GetComponent<NeonSlideSfx>() == null)
                followPlayer.gameObject.AddComponent<NeonSlideSfx>();

            Debug.Log("[NeonGameSceneAdapter] Player awal " + spawnPoint + ", rotasi " + followPlayer.eulerAngles +
                      ". Kamera chase dipasang.", this);
        }

        DisableDecorativeHazards(scene);
        BuildCoins(environment.transform, spawnPoint);
        BuildHazards(environment.transform, spawnPoint);
        BuildNeonRoadAssets(environment.transform, spawnPoint);
        NeonAtmosphereRig.Install(scene);
        BeginTiling(environment.transform);
    }

    // Collider pemain di scene desainer sangat tinggi (mengikuti model karakter
    // yang di-scale 3x). Akibatnya lompatan standar (jumpForce 8) tidak pernah
    // cukup untuk melewati rintangan. Nilai lompat dinaikkan di sini supaya
    // karakter benar-benar bisa melompati hazard tanpa mengubah scene.
    private static void TunePlayer(Transform player)
    {
        if (player == null) return;

        PlayerJump jump = player.GetComponent<PlayerJump>();
        if (jump != null)
        {
            jump.JumpForce = PlayerJumpForce;
            Debug.Log("[NeonGameSceneAdapter] Kekuatan lompat diatur ke " + PlayerJumpForce + ".", player);
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints |= RigidbodyConstraints.FreezeRotation;
        }

        AlignColliderToFloor(player);
    }

    // Collider pemain menjulur ke bawah permukaan jalan: dasar collider ada di
    // y = 0, sedangkan permukaan jalan (dan telapak kaki karakter) di y = 0.5.
    // Karena collider selalu terbenam, fisika mendorong pemain ke atas terus
    // menerus dan karakter terlihat melompat-lompat sendiri saat berlari.
    // Di sini collider digeser naik sampai dasarnya pas menempel di jalan,
    // tanpa mengubah posisi karakter maupun nilai di scene.
    private static void AlignColliderToFloor(Transform player)
    {
        Collider collider = player.GetComponent<Collider>();
        if (collider == null) return;

        Physics.SyncTransforms();

        float floorTop = FloorCenterY + FloorThickness * 0.5f;
        float penetration = floorTop - collider.bounds.min.y;
        if (penetration <= 0.001f) return;

        if (collider is BoxCollider box)
            box.center += Vector3.up * penetration;
        else if (collider is CapsuleCollider capsule)
            capsule.center += Vector3.up * penetration;
        else if (collider is SphereCollider sphere)
            sphere.center += Vector3.up * penetration;

        Physics.SyncTransforms();

        Debug.Log("[NeonGameSceneAdapter] Collider pemain dinaikkan " + penetration.ToString("0.##") +
                  " agar tidak terbenam di jalan (penyebab karakter melompat sendiri).", player);
    }

    // Hazard yang ditempel ke properti dekorasi (barrier, mobil, laser hiasan)
    // membuat pemain langsung Game Over hanya karena menyentuh pinggir jalan.
    // Hazard buatan adapter ini selalu bernama "NeonHazard ..." sehingga
    // komponen lain bisa dibedakan dan dimatikan.
    private static void DisableDecorativeHazards(Scene scene)
    {
        Hazard[] hazards = FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int disabled = 0;

        for (int i = 0; i < hazards.Length; i++)
        {
            Hazard hazard = hazards[i];
            if (hazard == null) continue;
            if (hazard.gameObject.scene != scene) continue;
            if (hazard.gameObject.name.StartsWith(HazardName, System.StringComparison.Ordinal)) continue;

            hazard.enabled = false;
            disabled++;
        }

        if (disabled > 0)
            Debug.Log("[NeonGameSceneAdapter] " + disabled + " Hazard pada properti dekorasi dimatikan (hanya hazard neon yang aktif).", null);
    }

    // Scene GameNeonCity adalah alur lama (lari ke +Z, map diputar 90 derajat).
    // Begitu scene Game dimuat, alur lama dihentikan dan scene-nya dibuang agar
    // hanya map desainer yang tampil dan kamera chase tidak direbut.
    private static void TakeOver()
    {
        NeonCityGameplay[] legacyDrivers = FindObjectsByType<NeonCityGameplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacyDrivers.Length; i++)
        {
            if (legacyDrivers[i] != null) Destroy(legacyDrivers[i].gameObject);
        }

        NeonCityCameraRig[] legacyRigs = FindObjectsByType<NeonCityCameraRig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < legacyRigs.Length; i++)
        {
            if (legacyRigs[i] != null) legacyRigs[i].enabled = false;
        }

        Scene game = SceneManager.GetSceneByName(SceneName);
        Scene legacyScene = SceneManager.GetSceneByName(LegacySceneName);

        if (legacyScene.IsValid() && legacyScene.isLoaded)
        {
            if (SceneManager.GetActiveScene() == legacyScene && game.IsValid() && game.isLoaded)
                SceneManager.SetActiveScene(game);

            SceneManager.UnloadSceneAsync(legacyScene);
        }

        if (!game.IsValid() || !game.isLoaded) return;

        // UIManager bisa saja sudah diarahkan ke GameNeonCity oleh alur lama;
        // arahkan kembali ke scene Game supaya Restart tidak membuka map lama.
        UIManager[] managers = FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null) managers[i].SetGameScene(SceneName);
        }

        // Alur lama mematikan kamera & lampu milik scene Game. Nyalakan lagi
        // agar kamera chase punya sasaran.
        GameObject[] roots = game.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            if (roots[i].name == "Main Camera" || roots[i].name == "Directional Light")
                roots[i].SetActive(true);
        }
    }

    // Jaga-jaga: alur lama bisa mematikan root "Main Camera" satu frame setelah
    // TakeOver. Nyalakan kembali selama beberapa detik pertama.
    private static void WatchCameraRoot()
    {
        if (Time.unscaledTime > 5f) return;

        Scene game = SceneManager.GetSceneByName(SceneName);
        if (!game.IsValid() || !game.isLoaded) return;

        GameObject[] roots = game.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == "Main Camera" && !roots[i].activeSelf)
                roots[i].SetActive(true);
        }
    }

    private static Transform ResolvePlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
            return GameManager.Instance.Player;

        GameObject found = GameObject.FindGameObjectWithTag("Player");
        return found != null ? found.transform : null;
    }

    // GameObject.Find hanya mencari objek aktif dan bisa mengembalikan objek
    // milik scene lain. Helper ini hanya melihat scene yang diminta, termasuk
    // objek yang sedang nonaktif.
    private static GameObject FindSceneObject(Scene scene, string name)
    {
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

    private void LateUpdate()
    {
        WatchCameraRoot();
        if (!tiling || followPlayer == null || segments.Count < 2) return;

        float behind = followPlayer.position.x + SegmentBehindBuffer;
        int guard = 0;

        while (segments[rearIndex].minX > behind && guard++ < segments.Count)
        {
            Segment rear = segments[rearIndex];
            Segment front = segments[frontIndex];

            if (rear.root == null || front.root == null)
            {
                tiling = false;
                return;
            }

            rear.root.position = front.root.position + Vector3.left * segmentLength;
            rear.minX = front.minX - segmentLength;
            rear.maxX = front.maxX - segmentLength;

            ReactivateCoins(rear.root);

            frontIndex = rearIndex;
            rearIndex = (rearIndex + 1) % segments.Count;
        }
    }

    // Koin yang sudah diambil dimatikan (SetActive false). Saat segmen peta
    // didaur ulang ke depan, koin itu dinyalakan lagi agar lintasan tetap berisi.
    private static void ReactivateCoins(Transform root)
    {
        NeonCoinPickup[] pickups = root.GetComponentsInChildren<NeonCoinPickup>(true);
        for (int i = 0; i < pickups.Length; i++)
        {
            GameObject coin = pickups[i].gameObject;
            if (!coin.activeSelf) coin.SetActive(true);
        }

        CoinWork[] coins = root.GetComponentsInChildren<CoinWork>(true);
        for (int i = 0; i < coins.Length; i++)
        {
            GameObject coin = coins[i].gameObject;
            if (!coin.activeSelf) coin.SetActive(true);
        }
    }

    // Spawner lama menaruh koin/rintangan di sumbu +Z (map lama). Di map
    // desainer pemain berlari ke -X, jadi spawner lama dimatikan sebelum Start
    // agar tidak menumpuk objek di tempat yang salah.
    private static void DisableLegacySpawners(Scene scene)
    {
        TileSpawner[] tileSpawners = FindObjectsByType<TileSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tileSpawners.Length; i++)
        {
            if (tileSpawners[i] != null && tileSpawners[i].gameObject.scene == scene) tileSpawners[i].enabled = false;
        }

        CoinSpawner[] coinSpawners = FindObjectsByType<CoinSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < coinSpawners.Length; i++)
        {
            if (coinSpawners[i] != null && coinSpawners[i].gameObject.scene == scene) coinSpawners[i].enabled = false;
        }

        ObstacleSpawner[] obstacleSpawners = FindObjectsByType<ObstacleSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < obstacleSpawners.Length; i++)
        {
            if (obstacleSpawners[i] != null && obstacleSpawners[i].gameObject.scene == scene) obstacleSpawners[i].enabled = false;
        }

        GameObject tiles = FindSceneObject(scene, TilesName);
        if (tiles != null) tiles.SetActive(false);
    }

    // Lantai lama hanya selebar jalur +Z (9 x 600), sehingga pemain jatuh saat
    // mulai berlari ke arah -X. Ganti ukurannya agar menutup seluruh lintasan.
    private static void EnsureFloor(Scene scene)
    {
        GameObject floor = FindSceneObject(scene, FloorName);
        if (floor != null)
        {
            BoxCollider box = floor.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = Vector3.zero;
                box.size = new Vector3(FloorLength, FloorThickness, FloorWidth);
                floor.transform.rotation = Quaternion.identity;
                floor.transform.localScale = Vector3.one;
                floor.transform.position = new Vector3(0f, FloorCenterY, 0f);
                return;
            }
        }

        GameObject created = new GameObject(FloorName);
        if (scene.IsValid() && scene.isLoaded) SceneManager.MoveGameObjectToScene(created, scene);
        created.transform.position = new Vector3(0f, FloorCenterY, 0f);
        BoxCollider createdBox = created.AddComponent<BoxCollider>();
        createdBox.size = new Vector3(FloorLength, FloorThickness, FloorWidth);
    }

    // Koin desainer sudah tersusun rapi dan tegak di sepanjang jalan pada z yang
    // sejajar jalur pemain; yang belum ada hanya trigger. Jadi visual koin yang
    // ada langsung dijadikan koin interaktif, tanpa membuat salinan visual.
    private void BuildCoins(Transform environment, Vector3 spawnPoint)
    {
        Transform[] children = environment.GetComponentsInChildren<Transform>(true);
        int converted = 0;

        for (int i = 0; i < children.Length; i++)
        {
            Transform candidate = children[i];
            if (!IsCoinVisualRoot(candidate, environment)) continue;
            if (!MakeCollectible(candidate, spawnPoint)) continue;
            converted++;
        }

        int fromMarkers = BuildCoinsFromMarkers(environment, spawnPoint);

        Debug.Log("[NeonGameSceneAdapter] Koin interaktif: " + converted + " visual + " +
                  fromMarkers + " dari penanda 'coin'.", this);

        if (converted + fromMarkers == 0)
            Debug.LogWarning("[NeonGameSceneAdapter] Tidak ada koin yang bisa dibuat di scene " + SceneName + ".", this);
    }

    private static bool IsCoinVisualRoot(Transform candidate, Transform environment)
    {
        if (candidate == null) return false;
        if (!candidate.name.StartsWith(CoinVisualPrefix, System.StringComparison.Ordinal)) return false;
        if (candidate.GetComponentInChildren<Renderer>(true) == null) return false;

        Transform parent = candidate.parent;
        while (parent != null && parent != environment)
        {
            if (parent.name.StartsWith(CoinVisualPrefix, System.StringComparison.Ordinal)) return false;
            parent = parent.parent;
        }

        return true;
    }

    private static bool MakeCollectible(Transform coin, Vector3 spawnPoint)
    {
        if (coin == null) return false;
        if ((coin.position - spawnPoint).sqrMagnitude < CoinSkipSpawnSqr) return false;

        Collider[] colliders = coin.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        NeonCoinPickup pickup = coin.GetComponent<NeonCoinPickup>();
        if (pickup == null) pickup = coin.gameObject.AddComponent<NeonCoinPickup>();

        pickup.Configure(CoinCollectRadius);

        return true;
    }

    // Penanda "coin" milik desainer dipakai sebagai sumber posisi. Koin visual
    // yang sudah ada di bawah penanda tetap dipakai; kalau penanda tidak punya
    // koin visual, dibuatkan bola pengganti tepat di posisi penanda. Objek
    // penanda tidak dihapus agar tidak ikut menghancurkan koin di bawahnya.
    private int BuildCoinsFromMarkers(Transform environment, Vector3 spawnPoint)
    {
        List<Transform> markers = new List<Transform>();
        Transform[] children = environment.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (IsCoinMarker(children[i])) markers.Add(children[i]);
        }

        int created = 0;

        for (int i = 0; i < markers.Count; i++)
        {
            Transform marker = markers[i];
            if (marker == null) continue;

            // Sudah punya koin interaktif di bawahnya? Tidak perlu dibuat lagi.
            if (marker.GetComponentInChildren<NeonCoinPickup>(true) != null) continue;

            Vector3 position = marker.position;
            if (position.y < CoinHeight) position.y = CoinHeight;

            if ((position - spawnPoint).sqrMagnitude < CoinSkipSpawnSqr) continue;

            GameObject coin = new GameObject(marker.name + " (koin)");
            coin.transform.SetParent(marker, false);
            coin.transform.position = position;

            CreateFallbackVisual(coin.transform);

            NeonCoinPickup pickup = coin.AddComponent<NeonCoinPickup>();
            pickup.Configure(CoinCollectRadius);
            created++;
        }

        return created;
    }

    private static bool IsCoinMarker(Transform transform)
    {
        if (transform.name != CoinMarkerName && !transform.name.StartsWith(CoinMarkerName + " (")) return false;
        return transform.GetComponent<CoinWork>() == null;
    }

    private static void CreateFallbackVisual(Transform parent)
    {
        Material goldRimMat = NeonVfxFactory.CreateLitMaterial(
            new Color(1f, 0.78f, 0.16f), 0.85f, 0.88f,
            new Color(1f, 0.75f, 0.15f), 1.4f);
        Material coreMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(
            new Color(1f, 0.92f, 0.35f), 2.2f);
        Material accentMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(
            new Color(0.15f, 0.96f, 1f), 2.0f);

        GameObject visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(parent, false);

        // Outer metallic gold disc
        GameObject disc = CreateCleanPrimitive(PrimitiveType.Cylinder, "CoinDisc", visualRoot.transform,
            Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(1.5f, 0.14f, 1.5f), goldRimMat);

        // Inner bright core
        CreateCleanPrimitive(PrimitiveType.Cylinder, "CoinCore", visualRoot.transform,
            Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.95f, 0.18f, 0.95f), coreMat);

        // Cyan energy diamond emblem across the coin face
        CreateCleanPrimitive(PrimitiveType.Cube, "CoinEmblem", visualRoot.transform,
            Vector3.zero, new Vector3(45f, 0f, 0f), new Vector3(0.32f, 0.55f, 0.55f), accentMat);

        if (disc != null) disc.transform.localPosition = Vector3.zero;
    }

    // Rintangan neon 3D bervariasi dipasang runtime di jalur -X agar visual lebih
    // kaya (Laser Gate, Cyber Roadblock, Plasma Pylon) sambil tetap mempertahankan
    // ukuran BoxCollider trigger & nama "NeonHazard ..." untuk kompatibilitas penuh.
    private void BuildHazards(Transform environment, Vector3 spawnPoint)
    {
        float minX;
        float maxX;
        if (!MeasureRoadX(environment, out minX, out maxX))
        {
            minX = spawnPoint.x - 600f;
            maxX = spawnPoint.x;
        }

        Material darkMetalMat = NeonVfxFactory.CreateLitMaterial(
            new Color(0.08f, 0.10f, 0.16f), 0.78f, 0.65f,
            new Color(0.05f, 0.18f, 0.30f), 0.25f);
        Material trimSteelMat = NeonVfxFactory.CreateLitMaterial(
            new Color(0.18f, 0.22f, 0.30f), 0.85f, 0.75f,
            Color.black, 0f);
        Material laserMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(
            new Color(1f, 0.12f, 0.48f), 2.8f);
        Material fieldMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(
            new Color(1f, 0.10f, 0.45f), 1.3f, true, 0.28f);
        Material warnAmberMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(
            new Color(1f, 0.46f, 0.08f), 2.5f);
        Material cyanAccentMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(
            new Color(0.12f, 0.96f, 1f), 2.2f);
        Material plasmaVioletMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(
            new Color(0.92f, 0.18f, 1f), 2.8f);

        int[][] patterns =
        {
            new[] { 0 },
            new[] { 1 },
            new[] { 2 },
            new[] { 0, 2 },
            new[] { 1 }
        };

        float floorTop = FloorCenterY + FloorThickness * 0.5f;
        float centerY = floorTop + HazardHeight * 0.5f;
        int created = 0;
        int step = 0;

        for (float x = maxX - HazardStartOffset; x > minX; x -= HazardSpacing)
        {
            int[] lanes = patterns[step % patterns.Length];

            for (int i = 0; i < lanes.Length; i++)
            {
                int lane = lanes[i];
                int variant = (step + i) % 3;

                GameObject hazard = new GameObject(HazardName + " " + step + "-" + lane);
                hazard.transform.position = new Vector3(x, centerY, (lane - 1) * HazardLaneDistance);
                hazard.transform.rotation = Quaternion.identity;
                hazard.transform.SetParent(environment, true);

                BoxCollider box = hazard.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(HazardLength, HazardHeight, HazardDepth);
                box.center = Vector3.zero;

                switch (variant)
                {
                    case 0:
                        BuildLaserGateVisual(hazard.transform, darkMetalMat, trimSteelMat, laserMat, fieldMat, cyanAccentMat);
                        break;
                    case 1:
                        BuildCyberBarricadeVisual(hazard.transform, darkMetalMat, trimSteelMat, warnAmberMat, laserMat);
                        break;
                    default:
                        BuildPlasmaPylonVisual(hazard.transform, darkMetalMat, trimSteelMat, plasmaVioletMat, cyanAccentMat);
                        break;
                }

                hazard.AddComponent<Hazard>();
                created++;
            }

            step++;
        }

        if (created > 0)
            Debug.Log("[NeonGameSceneAdapter] " + created + " hazard 3D neon bervariasi dipasang di lajur -X.", this);
    }

    private static void BuildLaserGateVisual(Transform root, Material darkMetal, Material trimSteel, Material laserMat, Material fieldMat, Material cyanMat)
    {
        float halfH = HazardHeight * 0.5f;
        float halfW = HazardDepth * 0.5f - 0.35f;

        // Twin emitter pylons on left & right of lane
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * halfW;
            // Pedestal base
            CreateCleanPrimitive(PrimitiveType.Cube, "Base", root,
                new Vector3(0f, -halfH + 0.22f, z), Vector3.zero, new Vector3(1.5f, 0.44f, 1.15f), darkMetal);
            // Main upright emitter tower
            CreateCleanPrimitive(PrimitiveType.Cube, "Pylon", root,
                new Vector3(0f, 0.05f, z), Vector3.zero, new Vector3(0.85f, HazardHeight - 0.1f, 0.72f), darkMetal);
            // Metallic collar
            CreateCleanPrimitive(PrimitiveType.Cube, "Collar", root,
                new Vector3(0f, -0.35f, z), Vector3.zero, new Vector3(1.02f, 0.28f, 0.88f), trimSteel);
            // Glowing cyan emitter status cap
            CreateCleanPrimitive(PrimitiveType.Cube, "CapGlow", root,
                new Vector3(0f, halfH + 0.04f, z), Vector3.zero, new Vector3(0.62f, 0.18f, 0.54f), cyanMat);
        }

        // 3 horizontal plasma laser beams spanning the lane (along Z)
        float beamSpan = halfW * 2f - 0.4f;
        Renderer[] pulseList = new Renderer[3];
        float[] beamYs = { -0.72f, 0.0f, 0.72f };

        for (int b = 0; b < 3; b++)
        {
            GameObject beam = CreateCleanPrimitive(PrimitiveType.Cylinder, "LaserBeam_" + b, root,
                new Vector3(0f, beamYs[b], 0f), new Vector3(90f, 0f, 0f),
                new Vector3(0.22f, beamSpan * 0.5f, 0.22f), laserMat);
            pulseList[b] = beam.GetComponent<Renderer>();
        }

        // Subtle translucent holographic barrier sheet between beams
        CreateCleanPrimitive(PrimitiveType.Cube, "HoloField", root,
            Vector3.zero, Vector3.zero, new Vector3(0.08f, HazardHeight - 0.55f, beamSpan), fieldMat);

        NeonHazardVisualFX fx = root.gameObject.AddComponent<NeonHazardVisualFX>();
        fx.pulseRenderers = pulseList;
        fx.pulseBaseColor = new Color(1f, 0.12f, 0.48f);
        fx.pulseMinIntensity = 1.9f;
        fx.pulseMaxIntensity = 3.5f;
        fx.pulseSpeed = 7.5f;
    }

    private static void BuildCyberBarricadeVisual(Transform root, Material darkMetal, Material trimSteel, Material amberMat, Material magentaMat)
    {
        float halfH = HazardHeight * 0.5f;
        float fullW = HazardDepth - 0.2f;

        // Heavy armored slanted barricade wall
        CreateCleanPrimitive(PrimitiveType.Cube, "BarricadeBase", root,
            new Vector3(0f, -halfH + 0.45f, 0f), Vector3.zero, new Vector3(2.2f, 0.9f, fullW), darkMetal);
        CreateCleanPrimitive(PrimitiveType.Cube, "BarricadeShield", root,
            new Vector3(0f, 0.15f, 0f), new Vector3(0f, 0f, -12f), new Vector3(0.95f, HazardHeight * 0.78f, fullW - 0.25f), darkMetal);

        // Reinforced steel edge ribs
        for (int s = -1; s <= 1; s++)
        {
            CreateCleanPrimitive(PrimitiveType.Cube, "Rib_" + s, root,
                new Vector3(0.35f, 0.05f, s * (fullW * 0.36f)), new Vector3(0f, 0f, -12f),
                new Vector3(0.55f, HazardHeight * 0.82f, 0.36f), trimSteel);
        }

        // High-visibility glowing horizontal neon warning strips (front & back facing)
        CreateCleanPrimitive(PrimitiveType.Cube, "WarnStripUpper", root,
            new Vector3(0.52f, 0.55f, 0f), Vector3.zero, new Vector3(0.18f, 0.26f, fullW - 0.5f), amberMat);
        CreateCleanPrimitive(PrimitiveType.Cube, "WarnStripLower", root,
            new Vector3(0.75f, -0.35f, 0f), Vector3.zero, new Vector3(0.18f, 0.22f, fullW - 0.5f), magentaMat);

        // Twin flashing warning beacons on top
        Renderer[] beacons = new Renderer[2];
        for (int i = 0; i < 2; i++)
        {
            float z = (i == 0 ? -1f : 1f) * (fullW * 0.34f);
            GameObject beacon = CreateCleanPrimitive(PrimitiveType.Cylinder, "Beacon_" + i, root,
                new Vector3(0f, halfH + 0.14f, z), Vector3.zero, new Vector3(0.45f, 0.22f, 0.45f), amberMat);
            beacons[i] = beacon.GetComponent<Renderer>();
        }

        NeonHazardVisualFX fx = root.gameObject.AddComponent<NeonHazardVisualFX>();
        fx.pulseRenderers = beacons;
        fx.pulseBaseColor = new Color(1f, 0.42f, 0.06f);
        fx.pulseMinIntensity = 1.2f;
        fx.pulseMaxIntensity = 3.6f;
        fx.pulseSpeed = 9f;
    }

    private static void BuildPlasmaPylonVisual(Transform root, Material darkMetal, Material trimSteel, Material plasmaMat, Material cyanMat)
    {
        float halfH = HazardHeight * 0.5f;
        float fullW = HazardDepth - 0.3f;

        // Wide low cyber containment deck spanning the lane
        CreateCleanPrimitive(PrimitiveType.Cube, "Deck", root,
            new Vector3(0f, -halfH + 0.30f, 0f), Vector3.zero, new Vector3(2.4f, 0.6f, fullW), darkMetal);
        CreateCleanPrimitive(PrimitiveType.Cube, "DeckTrim", root,
            new Vector3(0f, -halfH + 0.62f, 0f), Vector3.zero, new Vector3(2.5f, 0.12f, fullW - 0.3f), cyanMat);

        // Side energy projector prongs
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * (fullW * 0.42f);
            CreateCleanPrimitive(PrimitiveType.Cube, "SideProng", root,
                new Vector3(0f, -0.15f, z), new Vector3(side * 14f, 0f, 0f), new Vector3(0.95f, 1.85f, 0.55f), trimSteel);
        }

        // Hovering spinning plasma core assembly in the center
        GameObject corePivot = new GameObject("PlasmaCorePivot");
        corePivot.transform.SetParent(root, false);
        corePivot.transform.localPosition = new Vector3(0f, 0.35f, 0f);

        GameObject core = CreateCleanPrimitive(PrimitiveType.Cube, "PlasmaCore", corePivot.transform,
            Vector3.zero, new Vector3(45f, 45f, 45f), new Vector3(1.15f, 1.15f, 1.15f), plasmaMat);

        // Horizontal plasma containment bars extending from core
        GameObject arcL = CreateCleanPrimitive(PrimitiveType.Cube, "PlasmaArcL", root,
            new Vector3(0f, 0.35f, -fullW * 0.22f), Vector3.zero, new Vector3(0.24f, 0.24f, fullW * 0.36f), plasmaMat);
        GameObject arcR = CreateCleanPrimitive(PrimitiveType.Cube, "PlasmaArcR", root,
            new Vector3(0f, 0.35f, fullW * 0.22f), Vector3.zero, new Vector3(0.24f, 0.24f, fullW * 0.36f), plasmaMat);

        NeonHazardVisualFX fx = root.gameObject.AddComponent<NeonHazardVisualFX>();
        fx.spinner = corePivot.transform;
        fx.spinSpeed = 140f;
        fx.hoverTarget = corePivot.transform;
        fx.hoverAmplitude = 0.16f;
        fx.hoverSpeed = 3.8f;
        fx.pulseRenderers = new[] { core.GetComponent<Renderer>(), arcL.GetComponent<Renderer>(), arcR.GetComponent<Renderer>() };
        fx.pulseBaseColor = new Color(0.92f, 0.18f, 1f);
        fx.pulseMinIntensity = 1.8f;
        fx.pulseMaxIntensity = 3.4f;
        fx.pulseSpeed = 6.5f;
    }

    // Delegasikan pembangunan asset jalan, gerbang, skyline, & prop neon ke NeonEnvironmentEnricher.
    private void BuildNeonRoadAssets(Transform environment, Vector3 spawnPoint)
    {
        float minX;
        float maxX;
        if (!MeasureRoadX(environment, out minX, out maxX))
        {
            minX = spawnPoint.x - 560f;
            maxX = spawnPoint.x + 20f;
        }

        float floorTop = FloorCenterY + FloorThickness * 0.5f;
        NeonEnvironmentEnricher.Build(environment, spawnPoint, floorTop, minX, maxX);
    }

    private static GameObject CreateCleanPrimitive(
        PrimitiveType type,
        string name,
        Transform parent,
        Vector3 localPos,
        Vector3 localEuler,
        Vector3 localScale,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale = localScale;

        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return go;
    }

    // Map desainer hanya sepanjang ~580 m, jadi salinan environment disambung
    // di depan (-X) dan didaur ulang saat sudah lewat kamera.
    private void BeginTiling(Transform environment)
    {
        if (followPlayer == null)
        {
            Debug.LogWarning("[NeonGameSceneAdapter] Player tidak ditemukan, map tidak diulang.", this);
            return;
        }

        float minX;
        float maxX;
        if (!MeasureRoadX(environment, out minX, out maxX)) return;

        float length = maxX - minX;
        if (length < 20f) return;

        segmentLength = length;
        segments.Add(new Segment { root = environment, minX = minX, maxX = maxX });

        for (int i = 1; i <= SegmentCopies; i++)
        {
            GameObject copy = Instantiate(environment.gameObject);
            copy.name = environment.name + " Segment " + i;
            copy.transform.position = environment.position + Vector3.left * (length * i);

            segments.Add(new Segment
            {
                root = copy.transform,
                minX = minX - length * i,
                maxX = maxX - length * i
            });
        }

        rearIndex = 0;
        frontIndex = segments.Count - 1;
        tiling = true;
    }

    private static bool MeasureRoadX(Transform root, out float minX, out float maxX)
    {
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;
            if (!renderer.gameObject.activeInHierarchy) continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.x < 0.01f) continue;
            if (!IsRoadPiece(renderer.transform, root)) continue;

            minX = Mathf.Min(minX, bounds.min.x);
            maxX = Mathf.Max(maxX, bounds.max.x);
        }

        if (float.IsInfinity(minX) || float.IsInfinity(maxX) || maxX <= minX)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!renderer.gameObject.activeInHierarchy) continue;
                if (renderer.bounds.size.x < 0.01f) continue;

                minX = Mathf.Min(minX, renderer.bounds.min.x);
                maxX = Mathf.Max(maxX, renderer.bounds.max.x);
            }
        }

        return !float.IsInfinity(minX) && !float.IsInfinity(maxX) && maxX > minX;
    }

    private static bool IsRoadPiece(Transform transform, Transform root)
    {
        Transform current = transform;
        for (int depth = 0; current != null && depth < 12; depth++)
        {
            string name = current.name;
            if (name.IndexOf("4Lane", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.StartsWith("Lane_Marking")) return true;
            if (current == root) break;
            current = current.parent;
        }

        return false;
    }
}
