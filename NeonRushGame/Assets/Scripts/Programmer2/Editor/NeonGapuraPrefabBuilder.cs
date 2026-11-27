using System.IO;
using UnityEditor;
using UnityEngine;

// Membuat asset Prefab permanen untuk Gapura Cyberpunk di:
//   Assets/Prefabs/Environment/Neon_Cyber_Gapura.prefab
// lengkap dengan material (.mat) & tekstur poster (.png) tersimpan di:
//   Assets/Prefabs/Environment/NeonGapura_Assets/
// sehingga bisa langsung di-drag ke scene mana saja dan diedit di Inspector.
[InitializeOnLoad]
public static class NeonGapuraPrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Environment/Neon_Cyber_Gapura.prefab";
    private const string AssetsFolder = "Assets/Prefabs/Environment/NeonGapura_Assets";

    static NeonGapuraPrefabBuilder()
    {
        EditorApplication.delayCall += EnsurePrefabExists;
    }

    private static void EnsurePrefabExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;

        BuildGapuraPrefab();
    }

    [MenuItem("Neon Rush/Build Gapura Prefab")]
    public static void BuildGapuraPrefab()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Environment");
        EnsureFolder(AssetsFolder);

        // 1. Save cyberpunk banner PNG textures
        Texture2D texCyan = SaveBannerTexture(AssetsFolder + "/GapuraBanner_Cyan.png", 0);
        Texture2D texMagenta = SaveBannerTexture(AssetsFolder + "/GapuraBanner_Magenta.png", 1);

        // 2. Create & save persistent URP materials
        Material darkSteelMat = SaveLitMaterial(
            AssetsFolder + "/Gapura_DarkSteel.mat",
            new Color(0.055f, 0.072f, 0.125f), 0.82f, 0.72f, Color.black, 0f);
        Material brushedMetalMat = SaveLitMaterial(
            AssetsFolder + "/Gapura_BrushedMetal.mat",
            new Color(0.14f, 0.17f, 0.25f), 0.88f, 0.78f, Color.black, 0f);
        Material cyanNeonMat = SaveEmissiveMaterial(
            AssetsFolder + "/Gapura_NeonCyan.mat", new Color(0.08f, 0.95f, 1f), 2.3f);
        Material magentaNeonMat = SaveEmissiveMaterial(
            AssetsFolder + "/Gapura_NeonMagenta.mat", new Color(1f, 0.16f, 0.86f), 2.3f);
        Material goldNeonMat = SaveEmissiveMaterial(
            AssetsFolder + "/Gapura_NeonGold.mat", new Color(1f, 0.80f, 0.20f), 2.3f);
        Material bannerCyanMat = SavePosterMaterial(
            AssetsFolder + "/Gapura_BannerCyan.mat", texCyan);
        Material bannerMagentaMat = SavePosterMaterial(
            AssetsFolder + "/Gapura_BannerMagenta.mat", texMagenta);

        // 3. Build Gapura GameObject hierarchy
        float halfSpan = 21.2f;
        float height = 18.8f;

        GameObject root = new GameObject("Neon_Cyber_Gapura");

        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * halfSpan;
            string tag = side < 0 ? "L" : "R";

            AddPart(PrimitiveType.Cube, "Footing_" + tag, root.transform,
                new Vector3(0f, 0.9f, z), Vector3.zero, new Vector3(3.2f, 1.8f, 2.8f), darkSteelMat);
            AddPart(PrimitiveType.Cube, "Tower_" + tag, root.transform,
                new Vector3(0f, height * 0.5f, z), Vector3.zero, new Vector3(1.8f, height, 1.8f), darkSteelMat);
            AddPart(PrimitiveType.Cube, "TowerNeon_Front_" + tag, root.transform,
                new Vector3(0.95f, height * 0.5f, z), Vector3.zero, new Vector3(0.20f, height - 2.4f, 0.40f), cyanNeonMat);
            AddPart(PrimitiveType.Cube, "TowerNeon_Back_" + tag, root.transform,
                new Vector3(-0.95f, height * 0.5f, z), Vector3.zero, new Vector3(0.20f, height - 2.4f, 0.40f), magentaNeonMat);

            // Diagonal cyberpunk truss brace
            AddPart(PrimitiveType.Cube, "DiagonalBrace_" + tag, root.transform,
                new Vector3(0f, height - 2.6f, z - side * 2.7f), new Vector3(side * 42f, 0f, 0f),
                new Vector3(0.8f, 0.8f, 6.8f), brushedMetalMat);
            AddPart(PrimitiveType.Cube, "BraceNeon_" + tag, root.transform,
                new Vector3(0.45f, height - 2.6f, z - side * 2.7f), new Vector3(side * 42f, 0f, 0f),
                new Vector3(0.14f, 0.20f, 6.2f), magentaNeonMat);

            // Hanging holographic neon banner panel
            float bannerZ = side * 12.2f;
            AddPart(PrimitiveType.Cube, "HoloFrame_" + tag, root.transform,
                new Vector3(0.10f, height - 2.5f, bannerZ), Vector3.zero, new Vector3(0.28f, 3.0f, 7.6f), darkSteelMat);
            AddPart(PrimitiveType.Cube, "HoloScreen_" + tag, root.transform,
                new Vector3(0.32f, height - 2.5f, bannerZ), Vector3.zero, new Vector3(0.08f, 2.55f, 7.1f),
                side < 0 ? bannerCyanMat : bannerMagentaMat);
            AddPart(PrimitiveType.Cube, "HoloBorderBot_" + tag, root.transform,
                new Vector3(0.35f, height - 4.02f, bannerZ), Vector3.zero, new Vector3(0.16f, 0.14f, 7.6f), cyanNeonMat);
        }

        // Overhead main bridge truss
        AddPart(PrimitiveType.Cube, "MainBeam", root.transform,
            new Vector3(0f, height, 0f), Vector3.zero, new Vector3(2.4f, 1.9f, halfSpan * 2f + 2.4f), darkSteelMat);
        AddPart(PrimitiveType.Cube, "UpperCrown", root.transform,
            new Vector3(0f, height + 1.35f, 0f), Vector3.zero, new Vector3(1.5f, 0.7f, halfSpan * 1.4f), brushedMetalMat);

        // Dual neon tubes on both front (+X) and back (-X)
        AddPart(PrimitiveType.Cube, "BeamNeonLower_Front", root.transform,
            new Vector3(1.26f, height - 0.62f, 0f), Vector3.zero, new Vector3(0.22f, 0.28f, halfSpan * 2f - 1.2f), cyanNeonMat);
        AddPart(PrimitiveType.Cube, "BeamNeonUpper_Front", root.transform,
            new Vector3(1.26f, height + 0.62f, 0f), Vector3.zero, new Vector3(0.22f, 0.24f, halfSpan * 2f - 1.2f), magentaNeonMat);
        AddPart(PrimitiveType.Cube, "BeamNeonLower_Back", root.transform,
            new Vector3(-1.26f, height - 0.62f, 0f), Vector3.zero, new Vector3(0.22f, 0.28f, halfSpan * 2f - 1.2f), cyanNeonMat);

        // Central pulsing gold diamond crest
        GameObject crest = AddPart(PrimitiveType.Cube, "CenterCrest", root.transform,
            new Vector3(1.36f, height, 0f), new Vector3(45f, 0f, 0f), new Vector3(0.34f, 2.6f, 2.6f), goldNeonMat);

        NeonHazardVisualFX pulseFx = root.AddComponent<NeonHazardVisualFX>();
        pulseFx.pulseRenderers = new[] { crest.GetComponent<Renderer>() };
        pulseFx.pulseBaseColor = new Color(1f, 0.82f, 0.22f);
        pulseFx.pulseMinIntensity = 1.6f;
        pulseFx.pulseMaxIntensity = 3.2f;
        pulseFx.pulseSpeed = 3.6f;

        // 4. Save as persistent Prefab asset
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NeonGapuraPrefabBuilder] Prefab Gapura berhasil dibuat di: " + PrefabPath);
    }

    private static GameObject AddPart(
        PrimitiveType type,
        string name,
        Transform parent,
        Vector3 localPos,
        Vector3 localEuler,
        Vector3 localScale,
        Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = localEuler;
        go.transform.localScale = localScale;

        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        Renderer r = go.GetComponent<Renderer>();
        if (r != null && mat != null)
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return go;
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
        string parent = Path.GetDirectoryName(assetFolderPath).Replace("\\", "/");
        string name = Path.GetFileName(assetFolderPath);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static Material SaveLitMaterial(string path, Color baseColor, float metallic, float smoothness, Color emission, float emissionIntensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (emissionIntensity > 0f && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission * emissionIntensity);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material SaveEmissiveMaterial(string path, Color color, float intensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        Color hdr = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material SavePosterMaterial(string path, Texture2D tex)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        Color tint = new Color(1.55f, 1.55f, 1.55f, 1f);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Texture2D SaveBannerTexture(string assetPath, int style)
    {
        int w = 512;
        int h = 256;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color primary = style == 0 ? new Color(0.08f, 0.96f, 1f) : new Color(1f, 0.16f, 0.85f);
        Color secondary = style == 0 ? new Color(1f, 0.18f, 0.86f) : new Color(0.10f, 0.95f, 1f);
        Color bgTop = new Color(0.02f, 0.03f, 0.10f);
        Color bgBot = new Color(0.08f, 0.03f, 0.16f);

        Color[] pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            bool scanline = (y % 4) == 0;
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                Color col = Color.Lerp(bgBot, bgTop, v);

                if (v < 0.42f)
                {
                    float gY = Mathf.Abs(Mathf.Sin(v * 38f));
                    float gX = Mathf.Abs(Mathf.Sin((u - 0.5f) / (0.48f - v) * 10f));
                    if (gY > 0.95f || gX > 0.96f)
                        col = Color.Lerp(col, secondary, 0.45f);
                }

                if (style == 0)
                {
                    float dx = (u - 0.30f) * 2.0f;
                    float dy = v - 0.56f;
                    if (Mathf.Sqrt(dx * dx + dy * dy) < 0.28f)
                    {
                        bool cut = v < 0.56f && ((y / 7) % 2 == 0);
                        if (!cut) col = Color.Lerp(secondary, new Color(1f, 0.85f, 0.22f), Mathf.Clamp01((v - 0.3f) * 2.2f));
                    }
                    if (u > 0.52f && u < 0.92f)
                    {
                        if (v > 0.64f && v < 0.76f) col = primary;
                        else if (v > 0.46f && v < 0.56f && u < 0.84f) col = secondary;
                        else if (v > 0.30f && v < 0.37f && u < 0.76f) col = Color.Lerp(primary, Color.white, 0.4f);
                    }
                }
                else
                {
                    float diag = Mathf.Abs(((u * 5f + v * 2.2f) % 1f) - 0.5f);
                    if (diag < 0.09f && v > 0.22f && v < 0.78f)
                        col = Color.Lerp(col, primary, 0.82f);

                    float diamond = Mathf.Abs((u - 0.5f) * 2f) + Mathf.Abs((v - 0.5f) * 1.4f);
                    if (diamond < 0.32f && diamond > 0.24f) col = secondary;
                    else if (diamond < 0.14f) col = Color.white;
                }

                bool outerBorder = x < 8 || x >= w - 8 || y < 8 || y >= h - 8;
                if (outerBorder) col = primary;
                if (scanline && !outerBorder) col *= 0.82f;
                col.a = 1f;
                pixels[y * w + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        string relative = assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;
        string fullDiskPath = Path.Combine(Application.dataPath, relative);
        File.WriteAllBytes(fullDiskPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }
}
