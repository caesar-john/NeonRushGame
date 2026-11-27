using System.Collections.Generic;
using UnityEngine;

// Mengembangkan visual environment di scene Game dengan mempertahankan konsistensi
// seluruh bangunan dan susunan asset milik desainer, lalu membuatnya menyala sesuai tema Neon Rush:
// 1. Marka jalan putih asli (Marka / Lane_Marking) dibuat menyala terang (tanpa strip double)
// 2. Seluruh gedung milik desainer diberi aksen trim neon & boost emissive pada jendela/panel terang
// 3. Elemen pencahayaan & struktur jalan (billboard, lampu jalan, barrier, sci-fi barrier,
//    dan koin) diberi cahaya / strip neon serasi (mobil & tanaman tetap asli)
// 4. Rel tepi jalan neon & Gerbang Gapura Cyberpunk (sesuai prefab Neon_Cyber_Gapura.prefab)
public static class NeonEnvironmentEnricher
{
    private static readonly string[] BuildingPrefixes =
    {
        "Building_", "BR-", "BB-", "EC-", "DC-", "BC-", "EB-", "QD-"
    };

    public static void Build(Transform environment, Vector3 spawnPoint, float floorTop, float minX, float maxX)
    {
        if (environment == null) return;
        if (environment.Find("__NeonEnvironmentAssets") != null) return;

        float roadLength = maxX - minX;
        if (roadLength < 20f) return;

        Material darkSteelMat = NeonVfxFactory.CreateLitMaterial(
            new Color(0.055f, 0.072f, 0.125f), 0.82f, 0.72f, Color.black, 0f);
        Material brushedMetalMat = NeonVfxFactory.CreateLitMaterial(
            new Color(0.14f, 0.17f, 0.25f), 0.88f, 0.78f, Color.black, 0f);

        Material whiteMarkaNeonMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(new Color(0.92f, 0.98f, 1f), 2.15f);
        Material cyanNeonMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(new Color(0.08f, 0.95f, 1f), 2.3f);
        Material magentaNeonMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(new Color(1f, 0.16f, 0.86f), 2.3f);
        Material violetNeonMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(new Color(0.62f, 0.24f, 1f), 2.2f);
        Material goldNeonMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(new Color(1f, 0.82f, 0.22f), 2.2f);
        Material orangeWarnMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(new Color(1f, 0.42f, 0.08f), 2.3f);
        Material warmLampMat = NeonVfxFactory.CreateEmissiveUnlitMaterial(new Color(0.75f, 0.96f, 1f), 2.6f);

        Material[] posterMats =
        {
            CreatePosterMaterial(0),
            CreatePosterMaterial(1),
            CreatePosterMaterial(2)
        };

        GameObject root = new GameObject("__NeonEnvironmentAssets");
        root.transform.SetParent(environment, false);

        // 1. Terangi & beri cahaya neon pada elemen struktur & lampu kota (mobil & tanaman dibiarkan asli)
        EnhanceAllUserAssets(
            environment,
            root.transform,
            whiteMarkaNeonMat,
            cyanNeonMat,
            magentaNeonMat,
            violetNeonMat,
            orangeWarnMat,
            warmLampMat,
            posterMats);

        // 2. Rel tepi jalan neon + Gerbang Gapura Cyberpunk (tanpa marka double & tanpa gedung balok baru)
        BuildEdgeGuardRails(root.transform, floorTop, minX, maxX, darkSteelMat, brushedMetalMat, cyanNeonMat, magentaNeonMat);
        BuildCyberGapura(root.transform, floorTop, minX, maxX, darkSteelMat, brushedMetalMat, cyanNeonMat, magentaNeonMat, goldNeonMat, posterMats);

        Debug.Log("[NeonEnvironmentEnricher] Marka putih menyala, seluruh asset existing diperkaya neon, dan Gapura terpasang.");
    }

    // ---------------------------------------------------------------- enhance all user-placed assets

    private static void EnhanceAllUserAssets(
        Transform environment,
        Transform accentsParent,
        Material whiteMarkaNeonMat,
        Material cyanNeonMat,
        Material magentaNeonMat,
        Material violetNeonMat,
        Material orangeWarnMat,
        Material warmLampMat,
        Material[] posterMats)
    {
        Dictionary<Material, Material> emissiveCache = new Dictionary<Material, Material>();
        Material[] buildingNeons = { cyanNeonMat, magentaNeonMat, violetNeonMat };

        int markaCount = 0;
        int buildingCount = 0;
        int billboardCount = 0;
        int streetLightCount = 0;
        int barrierCount = 0;
        int coinGlowCount = 0;

        // Loop anak langsung level atas di Environment terlebih dahulu untuk bangunan & prop utuh
        int directCount = environment.childCount;
        for (int i = 0; i < directCount; i++)
        {
            Transform child = environment.GetChild(i);
            if (child == null || child == accentsParent) continue;
            string name = child.name;

            // A. Marka Jalan Putih Asli -> dibuat menyala terang (HDR white neon)
            if (name.StartsWith("Marka", System.StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Lane_Marking", System.StringComparison.OrdinalIgnoreCase))
            {
                Renderer[] rs = child.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < rs.Length; r++)
                {
                    if (rs[r] == null) continue;
                    rs[r].sharedMaterial = whiteMarkaNeonMat;
                    markaCount++;
                }
                continue;
            }

        // B. Gedung-gedung yang sudah disusun user -> konsisten, tetap pakai tekstur asli
        //    (tanpa mengubah material mobil/pohon child di dalamnya), cukup ditambah garis trim neon
        if (IsUserBuilding(name))
        {
            Material primaryTrim = buildingNeons[buildingCount % buildingNeons.Length];
            Material secondaryTrim = buildingNeons[(buildingCount + 1) % buildingNeons.Length];
            EnhanceBuildingWithNeon(child, accentsParent, primaryTrim, secondaryTrim);
            buildingCount++;
            continue;
        }

            // C. Billboard -> poster neon + lampu sorot menyala + bingkai neon
            if (name.StartsWith("billboard", System.StringComparison.OrdinalIgnoreCase))
            {
                EnhanceBillboard(child, accentsParent, posterMats[billboardCount % posterMats.Length], warmLampMat,
                    (billboardCount % 2 == 0) ? cyanNeonMat : magentaNeonMat);
                billboardCount++;
                continue;
            }

            // D. Lampu Jalan (StreetLight-01) -> kepala lampu LED menyala + aksen tiang neon
            if (name.StartsWith("StreetLight", System.StringComparison.OrdinalIgnoreCase))
            {
                Material poleAccent = (streetLightCount % 2 == 0) ? cyanNeonMat : magentaNeonMat;
                EnhanceStreetLight(child, accentsParent, warmLampMat, poleAccent, streetLightCount % 2 == 0);
                streetLightCount++;
                continue;
            }

            // E. Sci-Fi Laser Barrier & Barrier Jalan -> aksen & material menyala neon
            if (name.StartsWith("Scifi_WidthLaser_Barrier", System.StringComparison.OrdinalIgnoreCase))
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null)
                {
                    Color glow = (barrierCount % 2 == 0) ? new Color(0.08f, 0.94f, 1f) : new Color(1f, 0.18f, 0.85f);
                    r.sharedMaterial = NeonVfxFactory.CreateLitMaterial(
                        new Color(0.09f, 0.13f, 0.22f), 0.86f, 0.80f, glow, 1.15f);
                }
                barrierCount++;
                continue;
            }

            if (name.StartsWith("Barrier-", System.StringComparison.OrdinalIgnoreCase))
            {
                Material strip = (barrierCount % 2 == 0) ? orangeWarnMat : magentaNeonMat;
                EnhanceRoadBarrier(child, accentsParent, strip);
                barrierCount++;
                continue;
            }

            // F. Koin yang sudah disusun user -> boost kilau emas HDR agar menangkap Bloom
            if (name.StartsWith("coin", System.StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Coin_Gold_Lightning", System.StringComparison.OrdinalIgnoreCase))
            {
                BoostCoinsEmission(child, emissiveCache, ref coinGlowCount);
            }
        }

        // Tangkap juga Lane_Marking yang mungkin berada di bawah objek lain
        Transform[] allTransforms = environment.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t == null) continue;
            if (t.name.StartsWith("Lane_Marking", System.StringComparison.OrdinalIgnoreCase))
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != whiteMarkaNeonMat)
                {
                    r.sharedMaterial = whiteMarkaNeonMat;
                    markaCount++;
                }
            }
        }

        Debug.Log("[NeonEnvironmentEnricher] Asset menyala: " + markaCount + " marka putih, " +
                  buildingCount + " gedung, " + billboardCount + " billboard, " +
                  streetLightCount + " lampu jalan, " + barrierCount + " barrier, " +
                  coinGlowCount + " koin.");
    }

    private static bool IsUserBuilding(string name)
    {
        for (int i = 0; i < BuildingPrefixes.Length; i++)
        {
            if (name.StartsWith(BuildingPrefixes[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void EnhanceBuildingWithNeon(
        Transform building,
        Transform accentsParent,
        Material primaryNeon,
        Material secondaryNeon)
    {
        // Ukur bounds gedung utama saja (abaikan child prop seperti mobil/pohon yang menempel di prefab kota)
        Renderer rootRenderer = building.GetComponent<Renderer>();
        Bounds combined;
        if (rootRenderer != null && rootRenderer.enabled)
        {
            combined = rootRenderer.bounds;
        }
        else
        {
            Renderer[] renderers = building.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;
            combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                string n = r.gameObject.name;
                if (n.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Car", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Palm", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                combined.Encapsulate(r.bounds);
            }
        }

        if (combined.size.x < 1f || combined.size.y < 2f) return;

        // Tambahkan garis lampu neon arsitektural di bawah accentsParent (bebas distorsi scale & isStatic)
        GameObject trimRoot = new GameObject("BuildingNeon_" + building.name);
        trimRoot.transform.SetParent(accentsParent, false);

        float roadFacingZ = combined.center.z >= 0f ? (combined.min.z + 0.35f) : (combined.max.z - 0.35f);
        float spanX = Mathf.Max(2f, combined.size.x * 0.92f);
        float heightY = combined.size.y;

        // 1. Garis neon atap (menghadap jalan)
        PlaceWorldBox(trimRoot.transform, "RoofNeon",
            new Vector3(combined.center.x, combined.max.y - 0.65f, roadFacingZ),
            new Vector3(spanX, 0.38f, 0.38f), primaryNeon);

        // 2. Garis neon tengah/kanopi bawah
        if (heightY > 8f)
        {
            PlaceWorldBox(trimRoot.transform, "CanopyNeon",
                new Vector3(combined.center.x, combined.min.y + Mathf.Min(5.2f, heightY * 0.32f), roadFacingZ),
                new Vector3(spanX, 0.26f, 0.32f), secondaryNeon);
        }

        // 3. Dua garis vertikal neon di sudut kiri & kanan fasad gedung
        float cornerHeight = Mathf.Max(2f, heightY * 0.86f);
        float cornerY = combined.min.y + cornerHeight * 0.5f + 0.4f;
        PlaceWorldBox(trimRoot.transform, "CornerNeonL",
            new Vector3(combined.min.x + 0.45f, cornerY, roadFacingZ),
            new Vector3(0.30f, cornerHeight, 0.30f), primaryNeon);
        PlaceWorldBox(trimRoot.transform, "CornerNeonR",
            new Vector3(combined.max.x - 0.45f, cornerY, roadFacingZ),
            new Vector3(0.30f, cornerHeight, 0.30f), secondaryNeon);
    }

    private static void EnhanceBillboard(Transform billboard, Transform accentsParent, Material posterMat, Material warmLampMat, Material frameNeon)
    {
        Transform[] children = billboard.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform t = children[i];
            if (t == null) continue;

            if (t.name == "billboard_poster")
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null)
                {
                    r.sharedMaterial = posterMat;

                    Bounds b = r.bounds;
                    GameObject frame = new GameObject("BillboardNeon_" + billboard.name);
                    frame.transform.SetParent(accentsParent, false);

                    PlaceWorldBox(frame.transform, "Top", new Vector3(b.center.x, b.max.y + 0.18f, b.center.z),
                        new Vector3(b.size.x + 0.4f, 0.28f, b.size.z + 0.4f), frameNeon);
                    PlaceWorldBox(frame.transform, "Bottom", new Vector3(b.center.x, b.min.y - 0.18f, b.center.z),
                        new Vector3(b.size.x + 0.4f, 0.28f, b.size.z + 0.4f), frameNeon);
                }
            }
            else if (t.name == "billboard_lamp_lens")
            {
                Renderer r = t.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = warmLampMat;
            }
        }
    }

    private static void EnhanceStreetLight(Transform streetLight, Transform accentsParent, Material warmLampMat, Material poleAccent, bool addPointLight)
    {
        Renderer r = streetLight.GetComponentInChildren<Renderer>(true);
        if (r == null) return;
        Bounds b = r.bounds;

        GameObject lampGroup = new GameObject("LampNeon_" + streetLight.name);
        lampGroup.transform.SetParent(accentsParent, false);

        Vector3 topPos = new Vector3(b.center.x, b.max.y - 0.30f, b.center.z);
        PlaceWorldBox(lampGroup.transform, "GlowHead", topPos, new Vector3(1.15f, 0.26f, 1.85f), warmLampMat);

        Vector3 polePos = new Vector3(b.center.x, b.min.y + b.size.y * 0.46f, b.center.z);
        PlaceWorldBox(lampGroup.transform, "PoleAccent", polePos, new Vector3(0.28f, b.size.y * 0.55f, 0.28f), poleAccent);

        if (addPointLight)
        {
            GameObject lightObj = new GameObject("StreetNeonLight");
            lightObj.transform.SetParent(lampGroup.transform, false);
            lightObj.transform.position = topPos + Vector3.down * 1.2f;

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.28f, 0.90f, 1f);
            l.intensity = 12f;
            l.range = 24f;
            l.shadows = LightShadows.None;
        }
    }

    private static void EnhanceRoadBarrier(Transform barrier, Transform accentsParent, Material stripMat)
    {
        Renderer r = barrier.GetComponentInChildren<Renderer>(true);
        if (r == null) return;
        Bounds b = r.bounds;

        PlaceWorldBox(accentsParent, "BarrierNeon_" + barrier.name,
            new Vector3(b.center.x, b.max.y + 0.08f, b.center.z),
            new Vector3(Mathf.Max(0.22f, b.size.x * 0.92f), 0.16f, Mathf.Max(0.22f, b.size.z * 0.92f)),
            stripMat);
    }

    private static void BoostCoinsEmission(Transform coinGroup, Dictionary<Material, Material> emissiveCache, ref int count)
    {
        Renderer[] rs = coinGroup.GetComponentsInChildren<Renderer>(true);
        Color goldGlow = new Color(1f, 0.80f, 0.16f);

        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null) continue;
            Material[] mats = r.sharedMaterials;
            bool changed = false;

            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;
                Material glowing = GetOrCreateGlowingCopy(mats[m], goldGlow, 1.35f, emissiveCache);
                if (glowing != null && glowing != mats[m])
                {
                    mats[m] = glowing;
                    changed = true;
                }
            }

            if (changed)
            {
                r.sharedMaterials = mats;
                count++;
            }
        }
    }

    // ---------------------------------------------------------------- edge rails & cyber gapura

    private static void BuildEdgeGuardRails(
        Transform parent,
        float floorTop,
        float minX,
        float maxX,
        Material darkSteelMat,
        Material brushedMetalMat,
        Material cyanNeonMat,
        Material magentaNeonMat)
    {
        float roadLength = maxX - minX;
        float midX = (minX + maxX) * 0.5f;

        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * 18.5f;
            Material neonMat = side < 0 ? cyanNeonMat : magentaNeonMat;

            CreatePrimitive(PrimitiveType.Cube, "EdgeCurb_" + side, parent,
                new Vector3(midX, floorTop + 0.16f, z), Vector3.zero,
                new Vector3(roadLength, 0.32f, 0.70f), darkSteelMat);

            CreatePrimitive(PrimitiveType.Cube, "EdgeNeonTop_" + side, parent,
                new Vector3(midX, floorTop + 0.35f, z), Vector3.zero,
                new Vector3(roadLength, 0.10f, 0.28f), neonMat);

            for (float x = maxX - 14f; x > minX + 10f; x -= 28f)
            {
                CreatePrimitive(PrimitiveType.Cube, "CurbPost", parent,
                    new Vector3(x, floorTop + 0.52f, z + side * 0.18f), Vector3.zero,
                    new Vector3(0.45f, 1.04f, 0.45f), brushedMetalMat);

                CreatePrimitive(PrimitiveType.Cube, "CurbBeacon", parent,
                    new Vector3(x, floorTop + 1.08f, z + side * 0.18f), Vector3.zero,
                    new Vector3(0.32f, 0.14f, 0.32f), neonMat);
            }
        }
    }

    private static void BuildCyberGapura(
        Transform parent,
        float floorTop,
        float minX,
        float maxX,
        Material darkSteelMat,
        Material brushedMetalMat,
        Material cyanNeonMat,
        Material magentaNeonMat,
        Material goldNeonMat,
        Material[] posterMats)
    {
        float archSpacing = 140f;
        float archHalfSpan = 21.2f;
        float archHeight = 18.8f;
        int archIndex = 0;

        for (float x = maxX - 75f; x > minX + 25f; x -= archSpacing)
        {
            GameObject arch = new GameObject("Neon_Cyber_Gapura_" + archIndex);
            arch.transform.SetParent(parent, false);
            arch.transform.position = new Vector3(x, floorTop, 0f);

            Material primaryNeon = (archIndex % 2 == 0) ? cyanNeonMat : magentaNeonMat;
            Material secondaryNeon = (archIndex % 2 == 0) ? magentaNeonMat : cyanNeonMat;

            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * archHalfSpan;

                CreatePrimitive(PrimitiveType.Cube, "Footing", arch.transform,
                    new Vector3(0f, 0.9f, z), Vector3.zero, new Vector3(3.2f, 1.8f, 2.8f), darkSteelMat);
                CreatePrimitive(PrimitiveType.Cube, "Tower", arch.transform,
                    new Vector3(0f, archHeight * 0.5f, z), Vector3.zero, new Vector3(1.8f, archHeight, 1.8f), darkSteelMat);
                CreatePrimitive(PrimitiveType.Cube, "TowerNeon", arch.transform,
                    new Vector3(0.95f, archHeight * 0.5f, z), Vector3.zero, new Vector3(0.20f, archHeight - 2.4f, 0.40f), primaryNeon);

                CreatePrimitive(PrimitiveType.Cube, "DiagonalBrace", arch.transform,
                    new Vector3(0f, archHeight - 2.6f, z - side * 2.7f), new Vector3(side * 42f, 0f, 0f),
                    new Vector3(0.8f, 0.8f, 6.8f), brushedMetalMat);
                CreatePrimitive(PrimitiveType.Cube, "BraceNeon", arch.transform,
                    new Vector3(0.45f, archHeight - 2.6f, z - side * 2.7f), new Vector3(side * 42f, 0f, 0f),
                    new Vector3(0.14f, 0.20f, 6.2f), secondaryNeon);

                float bannerZ = side * 12.2f;
                CreatePrimitive(PrimitiveType.Cube, "HoloFrame_" + side, arch.transform,
                    new Vector3(0.10f, archHeight - 2.5f, bannerZ), Vector3.zero, new Vector3(0.28f, 3.0f, 7.6f), darkSteelMat);
                CreatePrimitive(PrimitiveType.Cube, "HoloScreen_" + side, arch.transform,
                    new Vector3(0.32f, archHeight - 2.5f, bannerZ), Vector3.zero, new Vector3(0.08f, 2.55f, 7.1f),
                    posterMats[(archIndex + (side > 0 ? 1 : 0)) % posterMats.Length]);
                CreatePrimitive(PrimitiveType.Cube, "HoloBorderBot_" + side, arch.transform,
                    new Vector3(0.35f, archHeight - 4.02f, bannerZ), Vector3.zero, new Vector3(0.16f, 0.14f, 7.6f), primaryNeon);
            }

            CreatePrimitive(PrimitiveType.Cube, "MainBeam", arch.transform,
                new Vector3(0f, archHeight, 0f), Vector3.zero, new Vector3(2.4f, 1.9f, archHalfSpan * 2f + 2.4f), darkSteelMat);
            CreatePrimitive(PrimitiveType.Cube, "UpperCrown", arch.transform,
                new Vector3(0f, archHeight + 1.35f, 0f), Vector3.zero, new Vector3(1.5f, 0.7f, archHalfSpan * 1.4f), brushedMetalMat);

            CreatePrimitive(PrimitiveType.Cube, "BeamNeonLower", arch.transform,
                new Vector3(1.26f, archHeight - 0.62f, 0f), Vector3.zero, new Vector3(0.22f, 0.28f, archHalfSpan * 2f - 1.2f), primaryNeon);
            CreatePrimitive(PrimitiveType.Cube, "BeamNeonUpper", arch.transform,
                new Vector3(1.26f, archHeight + 0.62f, 0f), Vector3.zero, new Vector3(0.22f, 0.24f, archHalfSpan * 2f - 1.2f), secondaryNeon);

            GameObject crest = CreatePrimitive(PrimitiveType.Cube, "CenterCrest", arch.transform,
                new Vector3(1.36f, archHeight, 0f), new Vector3(45f, 0f, 0f), new Vector3(0.34f, 2.6f, 2.6f), goldNeonMat);

            NeonHazardVisualFX pulseFx = arch.AddComponent<NeonHazardVisualFX>();
            pulseFx.pulseRenderers = new[] { crest.GetComponent<Renderer>() };
            pulseFx.pulseBaseColor = new Color(1f, 0.82f, 0.22f);
            pulseFx.pulseMinIntensity = 1.6f;
            pulseFx.pulseMaxIntensity = 3.2f;
            pulseFx.pulseSpeed = 3.6f;

            archIndex++;
        }
    }

    // ---------------------------------------------------------------- poster textures & material helpers

    private static Material CreatePosterMaterial(int style)
    {
        Texture2D tex = BuildCyberPosterTexture(512, 256, style);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material mat = new Material(shader);
        Color tint = new Color(1.55f, 1.55f, 1.55f, 1f);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
        return mat;
    }

    private static Texture2D BuildCyberPosterTexture(int w, int h, int style)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.name = "NeonPoster_" + style;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color primary = style == 0
            ? new Color(0.08f, 0.96f, 1f)
            : (style == 1 ? new Color(1f, 0.16f, 0.85f) : new Color(1f, 0.80f, 0.18f));
        Color secondary = style == 0
            ? new Color(1f, 0.18f, 0.86f)
            : (style == 1 ? new Color(0.10f, 0.95f, 1f) : new Color(0.55f, 0.22f, 1f));
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
                else if (style == 1)
                {
                    float diag = Mathf.Abs(((u * 5f + v * 2.2f) % 1f) - 0.5f);
                    if (diag < 0.09f && v > 0.22f && v < 0.78f)
                        col = Color.Lerp(col, primary, 0.82f);

                    float diamond = Mathf.Abs((u - 0.5f) * 2f) + Mathf.Abs((v - 0.5f) * 1.4f);
                    if (diamond < 0.32f && diamond > 0.24f) col = secondary;
                    else if (diamond < 0.14f) col = Color.white;
                }
                else
                {
                    int bar = (int)(u * 18f);
                    float barX = (u * 18f) - bar;
                    float barH = 0.22f + 0.52f * Mathf.Abs(Mathf.Sin(bar * 1.3f + 0.7f));
                    if (barX > 0.18f && barX < 0.82f && v > 0.16f && v < 0.16f + barH)
                        col = Color.Lerp(secondary, primary, (v - 0.16f) / barH);
                }

                bool outerBorder = x < 8 || x >= w - 8 || y < 8 || y >= h - 8;
                bool innerAccent = (x >= 14 && x < 17 && y >= 14 && y < h - 14) || (x >= w - 17 && x < w - 14 && y >= 14 && y < h - 14);
                if (outerBorder) col = primary;
                else if (innerAccent) col = secondary;

                if (scanline && !outerBorder) col *= 0.82f;
                col.a = 1f;
                pixels[y * w + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static Material GetOrCreateGlowingCopy(Material src, Color glowColor, float intensity, Dictionary<Material, Material> cache)
    {
        if (src == null) return null;
        if (cache.TryGetValue(src, out Material cached)) return cached;
        if (!src.HasProperty("_EmissionColor")) return src;

        Material copy = new Material(src);
        copy.name = src.name + " (NeonGlow)";
        copy.hideFlags = HideFlags.HideAndDontSave;
        copy.EnableKeyword("_EMISSION");

        // Pertahankan tekstur asli (mis. gambar petir pada koin) sebagai EmissionMap agar tidak memutih
        Texture mainTex = null;
        if (copy.HasProperty("_BaseMap")) mainTex = copy.GetTexture("_BaseMap");
        if (mainTex == null && copy.HasProperty("_MainTex")) mainTex = copy.GetTexture("_MainTex");
        if (mainTex != null && copy.HasProperty("_EmissionMap"))
            copy.SetTexture("_EmissionMap", mainTex);

        float scaled = mainTex != null ? intensity * 0.55f : intensity * 0.75f;
        copy.SetColor("_EmissionColor", new Color(glowColor.r * scaled, glowColor.g * scaled, glowColor.b * scaled, 1f));

        cache[src] = copy;
        return copy;
    }

    private static GameObject PlaceWorldBox(Transform parent, string name, Vector3 worldPos, Vector3 worldScale, Material mat)
    {
        GameObject go = CreatePrimitive(PrimitiveType.Cube, name, parent, Vector3.zero, Vector3.zero, Vector3.one, mat);
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.identity;
        SetWorldScale(go.transform, worldScale);
        return go;
    }

    private static GameObject CreatePrimitive(
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
        if (col != null) Object.Destroy(col);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return go;
    }

    private static void SetWorldScale(Transform t, Vector3 worldScale)
    {
        if (t.parent == null)
        {
            t.localScale = worldScale;
            return;
        }

        Vector3 p = t.parent.lossyScale;
        t.localScale = new Vector3(
            Mathf.Abs(p.x) > 0.0001f ? worldScale.x / p.x : worldScale.x,
            Mathf.Abs(p.y) > 0.0001f ? worldScale.y / p.y : worldScale.y,
            Mathf.Abs(p.z) > 0.0001f ? worldScale.z / p.z : worldScale.z);
    }
}
