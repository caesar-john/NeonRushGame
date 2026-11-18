using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[AddComponentMenu("Neon Rush/Integration/Neon City Backdrop")]
public class NeonCityBackdrop : MonoBehaviour
{
    private const string GameSceneName = "Game";
    private const string BackdropSceneName = "NeonCity";
    private const string GameplaySceneName = "GameNeonCity";

    // Kamera map designer menghadap -X, jadi map diputar +90 derajat agar arah
    // pandangnya sama dengan arah lari pemain (+Z).
    private const float BackdropYaw = 90f;

    // Sedikit di bawah permukaan jalan P2 (y=0) supaya aspal map tidak menutupi
    // jalan game.
    private const float StreetSurfaceY = -0.08f;
    private const float StartOffsetZ = -20f;
    private static readonly int CopyCount = 1;
    private const float CopyGap = 2f;

    // Objek map yang menembus koridor jalur (mobil/barrier dekorasi) dimatikan.
    // Ambang tinggi mencegah aspal dan marka jalan ikut termatikan.
    private const float ClearanceHalfWidth = 6.5f;
    private const float ClearanceMinHeight = 0.6f;

    private static readonly string[] ConflictingRootNames =
    {
        "Main Camera", "EventSystem", "Canvas_HUD", "Directional Light"
    };

    private static readonly string[] ConflictingTypeNames =
    {
        "Camera", "AudioListener", "EventSystem", "Canvas"
    };

    private static bool bootstrapped;

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
        if (SceneManager.GetActiveScene().name == GameplaySceneName) return;
        if (scene.name == BackdropSceneName) return;

        if (scene.name != GameSceneName)
        {
            Scene loaded = SceneManager.GetSceneByName(BackdropSceneName);
            if (loaded.isLoaded && loaded != SceneManager.GetActiveScene()) SceneManager.UnloadSceneAsync(loaded);
            return;
        }

        if (HasDesignerMap(scene)) return;

        if (FindAnyObjectByType<NeonCityBackdrop>() != null) return;
        new GameObject("NeonCityBackdrop").AddComponent<NeonCityBackdrop>();
    }

    // Scene "Game" versi integrasi sudah memuat map desainer sendiri, jadi
    // backdrop NeonCity tidak perlu dimuat lagi.
    private static bool HasDesignerMap(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == "Environment") return true;
        }

        return false;
    }

    private void Start()
    {
        Scene backdrop = SceneManager.GetSceneByName(BackdropSceneName);
        if (backdrop.isLoaded)
        {
            Build(backdrop);
            return;
        }

        StartCoroutine(LoadBackdrop());
    }

    private IEnumerator LoadBackdrop()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(BackdropSceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogWarning("[NeonCityBackdrop] Scene '" + BackdropSceneName + "' tidak ada di Build Settings. Backdrop dilewati.", this);
            enabled = false;
            yield break;
        }

        while (!operation.isDone) yield return null;

        Build(SceneManager.GetSceneByName(BackdropSceneName));
    }

    private void Build(Scene backdrop)
    {
        List<GameObject> roots = new List<GameObject>();
        backdrop.GetRootGameObjects(roots);

        Transform environment = null;
        for (int i = 0; i < roots.Count; i++)
        {
            GameObject root = roots[i];

            if (root.name == "Environment")
            {
                environment = root.transform;
                DisableConflicts(root);
                continue;
            }

            // Volume map dipertahankan (Bloom/Tonemapping/Vignette) dan dipakai
            // oleh kamera game setelah post-processing dinyalakan.
            if (root.name == "Global Volume") continue;

            if (IsConflictingRoot(root.name))
            {
                root.SetActive(false);
                continue;
            }

            DisableConflicts(root);
        }

        if (environment == null)
        {
            Debug.LogWarning("[NeonCityBackdrop] Root 'Environment' tidak ditemukan di scene " + BackdropSceneName + ".", this);
            return;
        }

        environment.rotation = Quaternion.Euler(0f, BackdropYaw, 0f);
        AlignToStreet(environment);
        HideDecorations(environment);
        CullCorridor(environment);
        EnablePostProcessing();
        Tile(environment);
    }

    private static bool IsConflictingRoot(string name)
    {
        for (int i = 0; i < ConflictingRootNames.Length; i++)
        {
            if (name == ConflictingRootNames[i]) return true;
        }
        return false;
    }

    private static void AlignToStreet(Transform environment)
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

        // Fallback 1: baris koin dekorasi (baris tengah kota ada di z = 0).
        if (centerCount == 0)
        {
            for (int i = 0; i < children.Length; i++)
            {
                if (!children[i].name.StartsWith("Coin", System.StringComparison.OrdinalIgnoreCase)) continue;
                centerX += children[i].position.x;
                centerCount++;
            }
        }

        // Fallback 2: marka lajur.
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
            if (!renderers[i].enabled) continue;

            Bounds bounds = renderers[i].bounds;
            if (bounds.max.y < StreetSurfaceY + ClearanceMinHeight) continue;
            if (bounds.max.x < -ClearanceHalfWidth || bounds.min.x > ClearanceHalfWidth) continue;

            renderers[i].enabled = false;
        }
    }

    private static void EnablePostProcessing()
    {
        Camera camera = Camera.main;
        if (camera == null) camera = FindAnyObjectByType<Camera>();
        if (camera == null) return;

        UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
        if (data != null) data.renderPostProcessing = true;
    }

    private static void Tile(Transform environment)
    {
        if (CopyCount <= 1) return;

        float length = ComputeBounds(environment).size.z + CopyGap;
        if (length <= 1f) return;

        for (int i = 1; i < CopyCount; i++)
        {
            GameObject copy = Instantiate(environment.gameObject);
            copy.name = environment.name + " Copy " + i;
            copy.transform.position = environment.position + Vector3.forward * (length * i);
        }
    }

    private static Bounds ComputeBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void DisableConflicts(GameObject root)
    {
        Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null) continue;

            string typeName = behaviours[i].GetType().Name;
            for (int j = 0; j < ConflictingTypeNames.Length; j++)
            {
                if (typeName != ConflictingTypeNames[j]) continue;
                behaviours[i].enabled = false;
                break;
            }
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
    }
}
