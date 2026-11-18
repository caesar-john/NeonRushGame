using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[AddComponentMenu("Neon Rush/Integration/Neon City Gameplay")]
public class NeonCityGameplay : MonoBehaviour
{
    public const string GameplaySceneName = "GameNeonCity";

    private const string SystemsSceneName = "Game";
    private const float MapYaw = 90f;
    private const float StreetSurfaceY = -0.08f;
    private const float StartOffsetZ = -20f;
    private const float CameraDistance = 30f;
    private const float FallbackCameraLateral = -2.06f;
    private const float ClearanceHalfWidth = 6.5f;
    private const float ClearanceMinHeight = 0.6f;
    private const int SegmentCopies = 2;
    private const float SegmentBehindBuffer = 34f;

    private static bool bootstrapped;

    private sealed class Segment
    {
        public Transform root;
        public float minZ;
        public float maxZ;
    }

    private readonly List<Segment> segments = new List<Segment>();
    private float segmentLength;
    private float frontEdgeZ;
    private int rearIndex;
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
        if (scene.name == SystemsSceneName)
        {
            if (SceneManager.GetActiveScene().name == GameplaySceneName) ConfigureSystems(scene);
            return;
        }

        if (scene.name != GameplaySceneName) return;
        if (FindAnyObjectByType<NeonCityGameplay>() != null) return;

        GameObject host = new GameObject("NeonCityGameplay");
        host.AddComponent<NeonCityGameplay>();
    }

    private void Start()
    {
        StartCoroutine(Setup());
    }

    private IEnumerator Setup()
    {
        // Scene GameNeonCity sudah digantikan oleh scene Game (map desainer,
        // lari ke -X). Jangan pernah menjalankan alur +Z ini lagi.
        if (SceneManager.GetActiveScene().name != GameplaySceneName)
        {
            Destroy(gameObject);
            yield break;
        }

        Scene gameplay = SceneManager.GetSceneByName(GameplaySceneName);
        if (!gameplay.IsValid() || !gameplay.isLoaded) yield break;

        Transform environment = null;
        Camera mapCamera = null;
        Light mapLight = null;

        List<GameObject> roots = new List<GameObject>();
        gameplay.GetRootGameObjects(roots);
        for (int i = 0; i < roots.Count; i++)
        {
            GameObject root = roots[i];
            if (root.name == "Environment") environment = root.transform;
            else if (root.name == "Main Camera") mapCamera = root.GetComponent<Camera>();
            else if (root.name == "Directional Light") mapLight = root.GetComponent<Light>();
            else if (root.name == "Canvas_HUD" || root.name == "EventSystem") root.SetActive(false);
        }

        if (environment == null)
        {
            Debug.LogWarning("[NeonCityGameplay] Root 'Environment' tidak ditemukan di scene " + GameplaySceneName + ".", this);
            yield break;
        }

        float roadCenterZ = AverageZ(environment, "4Lane", true);
        float laneSurfaceY = MinY(environment, "Lane_Marking");
        float cameraHeight = mapCamera != null ? mapCamera.transform.position.y - laneSurfaceY : 11.24f;
        float cameraLateral = mapCamera != null ? mapCamera.transform.position.z - roadCenterZ : FallbackCameraLateral;

        Quaternion mapRotation = Quaternion.Euler(0f, MapYaw, 0f);

        environment.rotation = mapRotation * environment.rotation;
        float alignedPlayerZ = AlignToStreet(environment);
        HideDecorations(environment);
        CullCorridor(environment);
        DisableColliders(environment);

        if (mapLight != null)
            mapLight.transform.rotation = mapRotation * mapLight.transform.rotation;

        NeonCityCameraRig rig = null;
        if (mapCamera != null)
        {
            mapCamera.transform.rotation = mapRotation * mapCamera.transform.rotation;

            UniversalAdditionalCameraData cameraData = mapCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null) cameraData.renderPostProcessing = true;

            // Cloned road segments are moved at runtime, so baked occlusion data
            // no longer matches them.
            mapCamera.useOcclusionCulling = false;

            float cameraWorldY = StreetSurfaceY + cameraHeight;
            mapCamera.transform.position = new Vector3(cameraLateral, cameraWorldY, -CameraDistance);

            rig = mapCamera.gameObject.GetComponent<NeonCityCameraRig>();
            if (rig == null) rig = mapCamera.gameObject.AddComponent<NeonCityCameraRig>();
            rig.Configure(null, cameraWorldY, CameraDistance, cameraLateral);
        }

        Scene systems = SceneManager.GetSceneByName(SystemsSceneName);
        if (!systems.isLoaded)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(SystemsSceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogWarning("[NeonCityGameplay] Scene '" + SystemsSceneName + "' tidak ada di Build Settings.", this);
                yield break;
            }

            while (!operation.isDone) yield return null;
            systems = SceneManager.GetSceneByName(SystemsSceneName);
        }

        if (!systems.IsValid() || !systems.isLoaded) yield break;

        // Scene Game memuat integrator map desainer (NeonGameSceneAdapter).
        // Kalau itu ada, alur +Z ini menyerah dan menghapus dirinya sendiri.
        if (FindAnyObjectByType<NeonGameSceneAdapter>() != null)
        {
            Debug.Log("[NeonCityGameplay] Map desainer di scene Game aktif, alur +Z dimatikan.", this);
            Destroy(gameObject);
            yield break;
        }

        ConfigureSystems(systems);

        Transform player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) yield break;

        float deltaZ = player.position.z - alignedPlayerZ;
        if (Mathf.Abs(deltaZ) > 0.001f)
        {
            Vector3 position = environment.position;
            position.z += deltaZ;
            environment.position = position;
        }

        if (rig != null) rig.Configure(player, StreetSurfaceY + cameraHeight, CameraDistance, cameraLateral);

        BeginTiling(environment, player);
    }

    // The city map is a finite scene (~180 m of straight road), so after a few
    // hundred metres the player used to run past the last building and the world
    // went empty. Keep two copies of the environment back to back and recycle the
    // rear one as soon as it is fully behind the camera.
    private void BeginTiling(Transform environment, Transform player)
    {
        followPlayer = player;
        if (environment == null || player == null) return;

        float minZ;
        float maxZ;
        if (!MeasureRoadZ(environment, out minZ, out maxZ) &&
            !MeasureVisibleZ(environment, out minZ, out maxZ)) return;

        float length = maxZ - minZ;
        if (length < 10f) return;

        segmentLength = length;
        segments.Add(new Segment { root = environment, minZ = minZ, maxZ = maxZ });
        frontEdgeZ = maxZ;

        for (int i = 1; i < SegmentCopies; i++)
        {
            GameObject copy = Instantiate(environment.gameObject);
            copy.name = environment.name + " Segment " + i;
            copy.transform.position = environment.position + Vector3.forward * (frontEdgeZ - minZ);

            segments.Add(new Segment
            {
                root = copy.transform,
                minZ = frontEdgeZ,
                maxZ = frontEdgeZ + length
            });

            frontEdgeZ += length;
        }

        tiling = true;
    }

    private void LateUpdate()
    {
        if (!tiling || followPlayer == null || segments.Count < 2) return;

        float behind = followPlayer.position.z - SegmentBehindBuffer;
        int guard = 0;

        while (segments[rearIndex].maxZ < behind && guard++ < segments.Count)
        {
            Segment rear = segments[rearIndex];
            float offset = frontEdgeZ - rear.minZ;

            rear.root.position += Vector3.forward * offset;
            rear.minZ = frontEdgeZ;
            rear.maxZ = frontEdgeZ + segmentLength;
            frontEdgeZ = rear.maxZ;

            rearIndex = (rearIndex + 1) % segments.Count;
        }
    }

    // The road itself defines the loop distance; the ground plane is slightly
    // longer than the street, so using every renderer would leave a small gap
    // between two copies.
    private static bool MeasureRoadZ(Transform root, out float minZ, out float maxZ)
    {
        minZ = float.PositiveInfinity;
        maxZ = float.NegativeInfinity;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;
            if (!renderer.gameObject.activeInHierarchy) continue;
            if (!IsRoadPiece(renderer.transform, root)) continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.z < 0.01f) continue;

            minZ = Mathf.Min(minZ, bounds.min.z);
            maxZ = Mathf.Max(maxZ, bounds.max.z);
        }

        return !float.IsInfinity(minZ) && !float.IsInfinity(maxZ) && maxZ > minZ;
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

    private static bool MeasureVisibleZ(Transform root, out float minZ, out float maxZ)
    {
        minZ = float.PositiveInfinity;
        maxZ = float.NegativeInfinity;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;
            if (!renderer.gameObject.activeInHierarchy) continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.z < 0.01f) continue;

            minZ = Mathf.Min(minZ, bounds.min.z);
            maxZ = Mathf.Max(maxZ, bounds.max.z);
        }

        return !float.IsInfinity(minZ) && !float.IsInfinity(maxZ) && maxZ > minZ;
    }

    private static void ConfigureSystems(Scene systems)
    {
        if (!systems.IsValid() || !systems.isLoaded) return;

        List<GameObject> roots = new List<GameObject>();
        systems.GetRootGameObjects(roots);
        for (int i = 0; i < roots.Count; i++)
        {
            GameObject root = roots[i];

            if (root.name == "Main Camera" || root.name == "Directional Light")
            {
                root.SetActive(false);
                continue;
            }

            TileSpawner[] spawners = root.GetComponentsInChildren<TileSpawner>(true);
            for (int j = 0; j < spawners.Length; j++)
            {
                if (spawners[j] != null) spawners[j].enabled = false;
            }

            UIManager[] uiManagers = root.GetComponentsInChildren<UIManager>(true);
            for (int j = 0; j < uiManagers.Length; j++)
            {
                if (uiManagers[j] != null) uiManagers[j].SetGameScene(GameplaySceneName);
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name == "Tiles") children[j].gameObject.SetActive(false);
            }
        }
    }

    private static float AlignToStreet(Transform environment)
    {
        float centerX = 0f;
        int centerCount = 0;
        float surfaceY = float.PositiveInfinity;
        int surfaceCount = 0;

        Transform[] children = environment.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.IndexOf("4Lane", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                centerX += children[i].position.x;
                centerCount++;
            }

            if (children[i].name.StartsWith("Lane_Marking"))
            {
                surfaceY = Mathf.Min(surfaceY, children[i].position.y);
                surfaceCount++;
            }
        }

        if (centerCount == 0)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (!children[i].name.StartsWith("Coin", System.StringComparison.OrdinalIgnoreCase)) continue;
                centerX += children[i].position.x;
                centerCount++;
            }
        }

        if (centerCount == 0)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (!children[i].name.StartsWith("Lane_Marking")) continue;
                centerX += children[i].position.x;
                centerCount++;
            }
        }

        Vector3 position = environment.position;
        if (centerCount > 0) position.x -= centerX / centerCount;

        if (surfaceCount > 0) position.y += StreetSurfaceY - surfaceY;
        else
        {
            Bounds initial = ComputeBounds(environment);
            position.y += StreetSurfaceY - initial.min.y;
        }

        environment.position = position;

        float playerZ = 0f;
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
            playerZ = GameManager.Instance.Player.position.z;

        Bounds bounds = ComputeBounds(environment);
        position = environment.position;
        position.z += (playerZ + StartOffsetZ) - bounds.min.z;
        environment.position = position;

        return playerZ;
    }

    private static float AverageZ(Transform root, string pattern, bool fallback)
    {
        float sum = 0f;
        int count = 0;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.IndexOf(pattern, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            sum += children[i].position.z;
            count++;
        }

        if (count > 0) return sum / count;
        if (!fallback) return 0f;

        for (int i = 0; i < children.Length; i++)
        {
            if (!children[i].name.StartsWith("Coin", System.StringComparison.OrdinalIgnoreCase)) continue;
            sum += children[i].position.z;
            count++;
        }

        if (count > 0) return sum / count;

        for (int i = 0; i < children.Length; i++)
        {
            if (!children[i].name.StartsWith("Lane_Marking")) continue;
            sum += children[i].position.z;
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }

    private static float MinY(Transform root, string prefix)
    {
        float min = float.PositiveInfinity;
        int count = 0;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (!children[i].name.StartsWith(prefix)) continue;
            min = Mathf.Min(min, children[i].position.y);
            count++;
        }

        if (count > 0) return min;
        return ComputeBounds(root).min.y;
    }

    private static void HideDecorations(Transform environment)
    {
        Transform[] children = environment.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.StartsWith("Coin", System.StringComparison.OrdinalIgnoreCase))
                children[i].gameObject.SetActive(false);
        }
    }

    private static void CullCorridor(Transform environment)
    {
        Renderer[] renderers = environment.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Bounds bounds = renderers[i].bounds;
            if (bounds.max.y < StreetSurfaceY + ClearanceMinHeight) continue;
            if (bounds.max.x < -ClearanceHalfWidth || bounds.min.x > ClearanceHalfWidth) continue;

            renderers[i].enabled = false;
            renderers[i].forceRenderingOff = true;
        }
    }

    private static void DisableColliders(Transform environment)
    {
        Collider[] colliders = environment.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
    }

    private static Bounds ComputeBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
