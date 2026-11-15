using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("Neon Rush/Integration/Neon City Backdrop")]
public class NeonCityBackdrop : MonoBehaviour
{
    private const string GameSceneName = "Game";
    private const string BackdropSceneName = "NeonCity";
    private const float BackdropYaw = -90f;
    private const float StreetSurfaceY = 0.5f;
    private const float StartOffsetZ = -80f;
    private const int CopyCount = 2;
    private const float CopyGap = 2f;

    private static readonly string[] ConflictingTypeNames =
    {
        "Camera", "AudioListener", "Light", "EventSystem", "Canvas", "Volume"
    };

    private static bool bootstrapped;
    private static bool aligned;

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
        if (scene.name != GameSceneName)
        {
            Scene loaded = SceneManager.GetSceneByName(BackdropSceneName);
            if (loaded.isLoaded) SceneManager.UnloadSceneAsync(loaded);
            aligned = false;
            return;
        }

        if (FindAnyObjectByType<NeonCityBackdrop>() != null) return;
        new GameObject("NeonCityBackdrop").AddComponent<NeonCityBackdrop>();
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
            DisableConflicts(roots[i]);
            if (environment == null && roots[i].name == "Environment") environment = roots[i].transform;
        }

        if (environment == null)
        {
            Debug.LogWarning("[NeonCityBackdrop] Root 'Environment' tidak ditemukan di scene " + BackdropSceneName + ".", this);
            return;
        }

        if (!aligned)
        {
            environment.rotation = Quaternion.Euler(0f, BackdropYaw, 0f);
            AlignToStreet(environment);
            aligned = true;
        }

        Tile(environment);
    }

    private static void AlignToStreet(Transform environment)
    {
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float surfaceY = 0f;
        int markers = 0;

        Transform[] children = environment.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (!children[i].name.StartsWith("Lane_Marking")) continue;

            Vector3 point = children[i].position;
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            surfaceY += point.y;
            markers++;
        }

        Vector3 position = environment.position;
        if (markers > 0)
        {
            position.x -= (minX + maxX) * 0.5f;
            position.y += StreetSurfaceY - surfaceY / markers;
        }
        else
        {
            Bounds initial = ComputeBounds(environment);
            position.x -= initial.center.x;
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

    private static void Tile(Transform environment)
    {
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
