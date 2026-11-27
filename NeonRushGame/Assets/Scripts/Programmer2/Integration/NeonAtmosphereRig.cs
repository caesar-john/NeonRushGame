using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Menyiapkan atmosfer cyberpunk URP secara otomatis di scene Game:
// - Mengaktifkan Post-Processing di Main Camera
// - Membuat Global Volume runtime (Bloom, ACES Tonemapping, Vignette, ColorAdjustments)
// - Mengatur Fog & Trilight Ambient malam neon agar horizon menyatu mulus
[AddComponentMenu("Neon Rush/Integration/Neon Atmosphere Rig")]
public class NeonAtmosphereRig : MonoBehaviour
{
    public static void Install(Scene scene)
    {
        if (FindAnyObjectByType<NeonAtmosphereRig>() != null) return;

        GameObject go = new GameObject("__NeonAtmosphereRig");
        if (scene.IsValid() && scene.isLoaded)
            SceneManager.MoveGameObjectToScene(go, scene);

        go.AddComponent<NeonAtmosphereRig>();
    }

    private void Start()
    {
        ConfigureCameraAndSky();
        ConfigureFogAndAmbient();
        EnsureGlobalVolume();
    }

    private static void ConfigureCameraAndSky()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null) return;

        UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        // Deep cyberpunk indigo-violet horizon background matching fog
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.032f, 0.036f, 0.095f, 1f);
        if (cam.farClipPlane < 450f) cam.farClipPlane = 450f;
    }

    private static void ConfigureFogAndAmbient()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.042f, 0.038f, 0.125f, 1f);
        RenderSettings.fogDensity = 0.0026f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.16f, 0.19f, 0.36f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.22f, 0.12f, 0.30f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.07f, 0.12f, 1f);
    }

    private void EnsureGlobalVolume()
    {
        Volume[] existing = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] != null && existing[i].isGlobal && existing[i].gameObject.scene == gameObject.scene)
                return;
        }

        GameObject volumeObj = new GameObject("__NeonGlobalVolume");
        volumeObj.transform.SetParent(transform, false);

        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "NeonRushRuntimeProfile";

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.88f);
        bloom.intensity.Override(0.68f);
        bloom.scatter.Override(0.66f);
        bloom.highQualityFiltering.Override(true);

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.02f, 0.01f, 0.08f, 1f));
        vignette.intensity.Override(0.24f);
        vignette.smoothness.Override(0.45f);

        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.contrast.Override(10f);
        colorAdj.saturation.Override(14f);

        volume.sharedProfile = profile;
        Debug.Log("[NeonAtmosphereRig] URP Bloom, ACES Tonemapping, Vignette, dan Fog cyberpunk aktif.", this);
    }
}
