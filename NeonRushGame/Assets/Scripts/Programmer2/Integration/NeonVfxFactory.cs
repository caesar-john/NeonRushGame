using UnityEngine;

// Factory ringan untuk VFX partikel neon (coin pickup, jump, landing, crash)
// dan material URP HDR emissive sehingga semua asset in-game tampak menyala
// saat Post-Processing Bloom aktif tanpa butuh prefab eksternal tambahan.
public static class NeonVfxFactory
{
    private static Material particleMatGold;
    private static Material particleMatCyan;
    private static Material particleMatMagenta;
    private static Material trailMatCyan;
    private static Material trailMatMagenta;

    public static Material CreateLitMaterial(Color baseColor, float metallic, float smoothness, Color emissionColor, float emissionIntensity = 0f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        Material mat = new Material(shader);
        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (emissionIntensity > 0f && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        }

        return mat;
    }

    public static Material CreateEmissiveUnlitMaterial(Color color, float intensity, bool transparent = false, float alpha = 1f)
    {
        if (transparent)
        {
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (pShader == null) pShader = Shader.Find("Sprites/Default");
            if (pShader == null) return null;

            Material pMat = new Material(pShader);
            Color c = new Color(
                Mathf.Clamp01(color.r) * intensity,
                Mathf.Clamp01(color.g) * intensity,
                Mathf.Clamp01(color.b) * intensity,
                Mathf.Clamp01(alpha));

            if (pMat.HasProperty("_BaseColor")) pMat.SetColor("_BaseColor", c);
            if (pMat.HasProperty("_Color")) pMat.SetColor("_Color", c);
            if (pMat.HasProperty("_Surface")) pMat.SetFloat("_Surface", 1f);
            if (pMat.HasProperty("_Blend")) pMat.SetFloat("_Blend", 2f); // Additive
            if (pMat.HasProperty("_SrcBlend")) pMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (pMat.HasProperty("_DstBlend")) pMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (pMat.HasProperty("_ZWrite")) pMat.SetFloat("_ZWrite", 0f);
            pMat.SetOverrideTag("RenderType", "Transparent");
            pMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            pMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return pMat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        Material mat = new Material(shader);
        Color hdr = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);

        return mat;
    }

    public static Material GetTrailMaterial(bool magenta)
    {
        if (magenta)
        {
            if (trailMatMagenta == null)
                trailMatMagenta = CreateParticleMaterial(new Color(1f, 0.2f, 0.88f), 2.4f);
            return trailMatMagenta;
        }

        if (trailMatCyan == null)
            trailMatCyan = CreateParticleMaterial(new Color(0.1f, 0.96f, 1f), 2.4f);
        return trailMatCyan;
    }

    public static void SpawnCoinPickupBurst(Vector3 position)
    {
        if (particleMatGold == null)
            particleMatGold = CreateParticleMaterial(new Color(1f, 0.85f, 0.25f), 2.8f);

        GameObject go = new GameObject("VFX_CoinBurst");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.55f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 11.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.92f, 0.35f, 1f),
            new Color(0.2f, 0.98f, 1f, 1f));
        main.gravityModifier = 0.45f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.45f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = particleMatGold;

        ps.Play();
    }

    public static void SpawnJumpBurst(Vector3 position)
    {
        if (particleMatCyan == null)
            particleMatCyan = CreateParticleMaterial(new Color(0.15f, 0.96f, 1f), 2.6f);

        GameObject go = new GameObject("VFX_JumpBurst");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.45f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 8.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.15f, 0.98f, 1f, 1f),
            new Color(1f, 0.25f, 0.9f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.7f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMatCyan;

        ps.Play();
    }

    public static void SpawnCrashBurst(Vector3 position)
    {
        if (particleMatMagenta == null)
            particleMatMagenta = CreateParticleMaterial(new Color(1f, 0.2f, 0.45f), 3.0f);

        GameObject go = new GameObject("VFX_CrashBurst");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.75f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 16f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.18f, 0.42f, 1f),
            new Color(1f, 0.78f, 0.2f, 1f));
        main.gravityModifier = 0.7f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.8f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMatMagenta;

        ps.Play();
    }

    private static Material CreateParticleMaterial(Color color, float intensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        Color hdr = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);

        // Soft additive glow sprite texture
        Texture2D tex = BuildGlowDotTexture(32);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f); // Additive in URP Particles/Unlit
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }

    private static Texture2D BuildGlowDotTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = a * a;
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
