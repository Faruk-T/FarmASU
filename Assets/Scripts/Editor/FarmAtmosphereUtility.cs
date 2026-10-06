#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FarmASU.Editor
{
    /// <summary>
    /// Comprehensive World & Atmosphere Generator for FarmASU.
    /// Transforms the flat, single-color green plane into a rich, living countryside farm:
    /// 1. Procedural 1024x1024 seamless textures: Lush Grass, Packed Dirt Road, Plowed Farmland Soil, Cobblestone.
    /// 2. Tangible normal maps generated via Sobel height filtering.
    /// 3. URP Lit materials with micro-tiling, normal maps, and realistic roughness.
    /// 4. Organic curved dirt path ribbons connecting entrance, cottage, well, workshop, and farmland.
    /// 5. Raised tilled farmland vegetable plots with wooden borders and sprouting green crops.
    /// 6. Rustic split-rail perimeter wooden fences with entrance gate.
    /// 7. Rolling countryside green hills framing the horizon.
    /// 8. Warm rustic lanterns with glowing point lights.
    /// 9. Warm golden sunlight, soft shadows, countryside horizon fog, and calibrated URP post-processing.
    /// 10. Perfectly scaled farmstead cottage and props matching the peasant player.
    /// </summary>
    public static class FarmAtmosphereUtility
    {
        private const string TexturesFolder = "Assets/Art/Environment/Textures";
        private const string MaterialsFolder = "Assets/Art/Environment/Materials";
        private const string SettingsFolder = "Assets/Settings";
        private const string DemoScenePath = "Assets/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers.unity";
        private const string MainScenePath = "Assets/Scenes/Main/MainScene.unity";
        private const string ValleyAssetFolder = "Assets/Art/Environment";
        private const string TerrainDataAssetPath = ValleyAssetFolder + "/FarmValleyTerrainData.asset";

        [MenuItem("FarmASU/Setup Valley World in MainScene", false, 0)]
        public static void SetupValleyWorldInMainScene()
        {
            Debug.Log("[ValleySetup] Starting Valley World setup...");

            if (!Directory.Exists(ValleyAssetFolder))
            {
                Directory.CreateDirectory(ValleyAssetFolder);
                AssetDatabase.Refresh();
            }

            Scene mainScene = SceneManager.GetActiveScene();
            if (mainScene.path != MainScenePath)
            {
                mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }

            // 1. Open DemoGrassFlowers additively to extract Terrain
            Scene demoScene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);
            if (!demoScene.IsValid())
            {
                Debug.LogError($"[ValleySetup] Could not open demo scene at {DemoScenePath}");
                return;
            }

            Terrain demoTerrain = null;
            GameObject[] demoRoots = demoScene.GetRootGameObjects();
            foreach (var root in demoRoots)
            {
                Terrain t = root.GetComponentInChildren<Terrain>();
                if (t != null)
                {
                    demoTerrain = t;
                    break;
                }
            }

            if (demoTerrain == null || demoTerrain.terrainData == null)
            {
                Debug.LogError("[ValleySetup] Could not find Terrain in DemoGrassFlowers scene!");
                EditorSceneManager.CloseScene(demoScene, true);
                return;
            }

            Debug.Log($"[ValleySetup] Found Demo Terrain: size={demoTerrain.terrainData.size}");

            // 2. Clone TerrainData so MainScene has its own independent asset
            TerrainData clonedData = Object.Instantiate(demoTerrain.terrainData);
            clonedData.name = "FarmValleyTerrainData";

            if (File.Exists(TerrainDataAssetPath))
            {
                AssetDatabase.DeleteAsset(TerrainDataAssetPath);
            }
            AssetDatabase.CreateAsset(clonedData, TerrainDataAssetPath);
            AssetDatabase.SaveAssets();

            float detailDist = demoTerrain.detailObjectDistance;
            float detailDensity = demoTerrain.detailObjectDensity;
            float treeDistance = demoTerrain.treeDistance;
            Material terrainMat = demoTerrain.materialTemplate;

            // 3. Close the demo scene
            EditorSceneManager.CloseScene(demoScene, true);

            // 4. In MainScene, clean up the old flat test plane and any procedural environment
            GameObject oldPlane = GameObject.Find("Ground_TestPlane_20x20");
            if (oldPlane != null)
            {
                Undo.DestroyObjectImmediate(oldPlane);
                Debug.Log("[ValleySetup] Removed flat Ground_TestPlane_20x20.");
            }

            GameObject oldEnv = GameObject.Find("[Farm_Environment]");
            if (oldEnv != null)
            {
                Undo.DestroyObjectImmediate(oldEnv);
                Debug.Log("[ValleySetup] Removed old [Farm_Environment].");
            }

            // 5. Create or update Terrain in MainScene
            GameObject terrainObj = GameObject.Find("Terrain_FarmValley");
            if (terrainObj != null)
            {
                Undo.DestroyObjectImmediate(terrainObj);
            }

            terrainObj = Terrain.CreateTerrainGameObject(clonedData);
            terrainObj.name = "Terrain_FarmValley";
            Undo.RegisterCreatedObjectUndo(terrainObj, "Create Farm Valley Terrain");

            Terrain terrain = terrainObj.GetComponent<Terrain>();
            terrain.detailObjectDistance = Mathf.Max(detailDist, 80f);
            terrain.detailObjectDensity = Mathf.Clamp(detailDensity, 0.7f, 1.0f);
            terrain.treeDistance = Mathf.Max(treeDistance, 500f);
            if (terrainMat != null)
            {
                terrain.materialTemplate = terrainMat;
            }

            Vector3 tSize = clonedData.size;
            float tPosX = -tSize.x * 0.5f;
            float tPosZ = -tSize.z * 0.5f + 15.0f;
            terrainObj.transform.position = new Vector3(tPosX, 0.0f, tPosZ);

            // 6. Setup the Farmstead cottage and props on the valley floor
            FarmsteadSetupUtility.SetupFarmstead();

            GameObject farmstead = GameObject.Find("[Farmstead]");
            if (farmstead != null)
            {
                float fY = terrain.SampleHeight(new Vector3(2.5f, 0, 16.0f)) + terrainObj.transform.position.y;
                farmstead.transform.position = new Vector3(0, fY, 0);
            }

            // 7. Ensure Player is positioned properly on the ground
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                Undo.RecordObject(player.transform, "Position Player on Valley");
                float pY = terrain.SampleHeight(player.transform.position) + terrainObj.transform.position.y;
                player.transform.position = new Vector3(player.transform.position.x, Mathf.Max(pY + 0.1f, 0.1f), player.transform.position.z);
            }

            // 8. Configure atmospheric sunlight and fog
            ConfigureValleyAtmosphere();

            // 9. Calibrate grass/flower heights down to human scale & clear farmstead yard
            CalibrateGrassScaleAndClearYard();

            EditorSceneManager.MarkSceneDirty(mainScene);
            Debug.Log("[ValleySetup] === VAST VALLEY WORLD SETUP COMPLETE! ===");
        }

        [MenuItem("FarmASU/Calibrate Grass Scale & Clear Farmstead Yard", false, 1)]
        public static void CalibrateGrassScaleAndClearYard()
        {
            Terrain terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning("[GrassCalibration] No active Terrain found in scene!");
                return;
            }

            terrain.detailObjectDensity = 1.0f;
            terrain.detailObjectDistance = 150f;

            TerrainData td = terrain.terrainData;
            Undo.RecordObject(td, "Calibrate Grass & Clear Yard");

            // 1. Rescale Grass & Flower Prototypes to lush, full, knee-height proportions
            // Wide tufts (0.65m - 0.95m) ensure dense, thick meadow coverage without sparse gaps
            DetailPrototype[] prototypes = td.detailPrototypes;
            for (int i = 0; i < prototypes.Length; i++)
            {
                string name = prototypes[i].prototypeTexture != null ? prototypes[i].prototypeTexture.name.ToLower() : "";
                if (name.Contains("flower"))
                {
                    prototypes[i].minHeight = 0.60f;
                    prototypes[i].maxHeight = 0.95f;
                    prototypes[i].minWidth = 0.65f;
                    prototypes[i].maxWidth = 0.95f;
                }
                else
                {
                    prototypes[i].minHeight = 0.50f;
                    prototypes[i].maxHeight = 0.80f;
                    prototypes[i].minWidth = 0.65f;
                    prototypes[i].maxWidth = 0.95f;
                }
            }
            td.detailPrototypes = prototypes;

            // 2. Clear only the immediate physical footprint under house and well,
            // leaving lush grass hugging closely around the farmstead
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = td.size;
            int dWidth = td.detailWidth;
            int dHeight = td.detailHeight;

            var clearZones = new (Vector3 center, float innerRadius, float outerRadius)[]
            {
                // Immediate cottage footprint (walls and floor)
                (new Vector3(2.5f, 0, 16.0f), 2.8f, 4.2f),
                // Immediate well rim
                (new Vector3(-2.5f, 0, 11.5f), 1.2f, 2.0f),
                // Dining table
                (new Vector3(-5.0f, 0, 12.0f), 1.2f, 2.0f),
                // Woodcutter chopping block
                (new Vector3(-4.0f, 0, 15.2f), 1.0f, 1.8f)
            };

            for (int layer = 0; layer < prototypes.Length; layer++)
            {
                int[,] map = td.GetDetailLayer(0, 0, dWidth, dHeight, layer);

                foreach (var zone in clearZones)
                {
                    float normX = (zone.center.x - tPos.x) / tSize.x;
                    float normZ = (zone.center.z - tPos.z) / tSize.z;
                    int cX = Mathf.RoundToInt(normX * dWidth);
                    int cZ = Mathf.RoundToInt(normZ * dHeight);

                    int rInner = Mathf.CeilToInt((zone.innerRadius / tSize.x) * dWidth);
                    int rOuter = Mathf.CeilToInt((zone.outerRadius / tSize.x) * dWidth);

                    int xMin = Mathf.Clamp(cX - rOuter, 0, dWidth - 1);
                    int xMax = Mathf.Clamp(cX + rOuter, 0, dWidth - 1);
                    int zMin = Mathf.Clamp(cZ - rOuter, 0, dHeight - 1);
                    int zMax = Mathf.Clamp(cZ + rOuter, 0, dHeight - 1);

                    for (int z = zMin; z <= zMax; z++)
                    {
                        for (int x = xMin; x <= xMax; x++)
                        {
                            float distPx = Mathf.Sqrt((x - cX) * (x - cX) + (z - cZ) * (z - cZ));
                            if (distPx <= rInner)
                            {
                                map[z, x] = 0;
                            }
                            else if (distPx <= rOuter && map[z, x] > 0)
                            {
                                float t = (distPx - rInner) / (rOuter - rInner);
                                map[z, x] = Mathf.RoundToInt(map[z, x] * t);
                            }
                        }
                    }
                }

                td.SetDetailLayer(0, 0, layer, map);
            }

            EditorUtility.SetDirty(td);
            Debug.Log("[GrassCalibration] SUCCESS: Calibrated grass/flower heights to human scale and cleared farmstead yard & pathways!");
        }

        private static void ConfigureValleyAtmosphere()
        {
            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj != null)
            {
                Undo.RecordObject(sunObj.transform, "Valley Sun Rotation");
                sunObj.transform.rotation = Quaternion.Euler(42.0f, -38.0f, 0.0f);

                Light sun = sunObj.GetComponent<Light>();
                if (sun != null)
                {
                    Undo.RecordObject(sun, "Valley Sun Settings");
                    sun.color = new Color(1.0f, 0.96f, 0.88f);
                    sun.intensity = 1.2f;
                    sun.shadows = LightShadows.Soft;
                    sun.shadowBias = 0.04f;
                    sun.shadowNormalBias = 0.35f;
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.80f, 0.94f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.82f, 0.75f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.50f, 0.25f);
            RenderSettings.ambientIntensity = 1.1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60.0f;
            RenderSettings.fogEndDistance = 280.0f;
            RenderSettings.fogColor = new Color(0.78f, 0.86f, 0.90f);
        }

        [MenuItem("FarmASU/Transform Scene to Living Farm World")]
        public static void TransformSceneToLivingFarmWorld()
        {
            Debug.Log("[FarmAtmosphere] === STARTING FARM WORLD TRANSFORMATION ===");

            EnsureDirectories();

            // Step 1: Procedural Textures & Normal Maps
            GenerateAllTextures();

            // Step 2: URP Materials
            Material grassMat = CreateOrGetGrassMaterial();
            Material dirtMat = CreateOrGetDirtPathMaterial();
            Material farmSoilMat = CreateOrGetFarmlandMaterial();
            Material cobbleMat = CreateOrGetCobblestoneMaterial();
            Material fenceMat = CreateOrGetFenceMaterial();

            // Step 3: Scene Lighting, Fog & URP Post-Processing
            ConfigureAtmosphereAndLighting();

            // Step 4: Expand Ground & Apply Lush Grass
            SetupGroundPlane(grassMat);

            // Step 5: Build Pathways, Farmland, Fences, Hills & Lanterns
            BuildFarmWorldEnvironment(grassMat, dirtMat, farmSoilMat, cobbleMat, fenceMat);

            // Step 6: Setup / Calibrate Farmstead Cottage & Props
            FarmsteadSetupUtility.SetupFarmstead();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FarmAtmosphere] === FARM WORLD TRANSFORMATION COMPLETE! ===");
        }

        [MenuItem("FarmASU/Configure Atmosphere & Lighting Only")]
        public static void MenuConfigureAtmosphere()
        {
            EnsureDirectories();
            ConfigureAtmosphereAndLighting();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FarmAtmosphere] Atmosphere, Sun & Lighting configured successfully!");
        }

        [MenuItem("FarmASU/Generate Farm Textures Only")]
        public static void MenuGenerateTextures()
        {
            EnsureDirectories();
            GenerateAllTextures();
            Debug.Log("[FarmAtmosphere] Farm textures generated and imported successfully!");
        }

        [MenuItem("FarmASU/Clear Generated Environment Objects")]
        public static void ClearGeneratedEnvironment()
        {
            GameObject env = GameObject.Find("[Farm_Environment]");
            if (env != null)
            {
                Undo.DestroyObjectImmediate(env);
                Debug.Log("[FarmAtmosphere] Removed [Farm_Environment] objects.");
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(TexturesFolder)) Directory.CreateDirectory(TexturesFolder);
            if (!Directory.Exists(MaterialsFolder)) Directory.CreateDirectory(MaterialsFolder);
            if (!Directory.Exists(SettingsFolder)) Directory.CreateDirectory(SettingsFolder);
            AssetDatabase.Refresh();
        }

        #region Procedural Textures Generation

        public static void GenerateAllTextures()
        {
            const int res = 1024;

            // 1. Grass Albedo & Normal
            string grassAlbedoPath = $"{TexturesFolder}/T_FarmGrass_Albedo.png";
            string grassNormalPath = $"{TexturesFolder}/T_FarmGrass_Normal.png";
            if (!File.Exists(grassAlbedoPath) || !File.Exists(grassNormalPath))
            {
                GenerateGrassTexture(res, grassAlbedoPath, grassNormalPath);
            }

            // 2. Dirt Road Albedo & Normal
            string dirtAlbedoPath = $"{TexturesFolder}/T_DirtPath_Albedo.png";
            string dirtNormalPath = $"{TexturesFolder}/T_DirtPath_Normal.png";
            if (!File.Exists(dirtAlbedoPath) || !File.Exists(dirtNormalPath))
            {
                GenerateDirtPathTexture(res, dirtAlbedoPath, dirtNormalPath);
            }

            // 3. Farmland Soil Albedo & Normal
            string soilAlbedoPath = $"{TexturesFolder}/T_Farmland_Albedo.png";
            string soilNormalPath = $"{TexturesFolder}/T_Farmland_Normal.png";
            if (!File.Exists(soilAlbedoPath) || !File.Exists(soilNormalPath))
            {
                GenerateFarmlandTexture(res, soilAlbedoPath, soilNormalPath);
            }

            // 4. Cobblestone Albedo & Normal
            string cobbleAlbedoPath = $"{TexturesFolder}/T_Cobblestone_Albedo.png";
            string cobbleNormalPath = $"{TexturesFolder}/T_Cobblestone_Normal.png";
            if (!File.Exists(cobbleAlbedoPath) || !File.Exists(cobbleNormalPath))
            {
                GenerateCobblestoneTexture(res, cobbleAlbedoPath, cobbleNormalPath);
            }

            AssetDatabase.Refresh();
            ConfigureTextureImporter(grassAlbedoPath, false);
            ConfigureTextureImporter(grassNormalPath, true);
            ConfigureTextureImporter(dirtAlbedoPath, false);
            ConfigureTextureImporter(dirtNormalPath, true);
            ConfigureTextureImporter(soilAlbedoPath, false);
            ConfigureTextureImporter(soilNormalPath, true);
            ConfigureTextureImporter(cobbleAlbedoPath, false);
            ConfigureTextureImporter(cobbleNormalPath, true);
        }

        private static void ConfigureTextureImporter(string path, bool isNormalMap)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool modified = false;
            if (isNormalMap)
            {
                if (importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    modified = true;
                }
            }
            else
            {
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                    modified = true;
                }
            }

            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                modified = true;
            }

            if (modified)
            {
                importer.SaveAndReimport();
            }
        }

        private static float SampleSeamlessNoise(float u, float v, float scale, float seed)
        {
            // 4-corner smooth cosine blend for perfect seamless tiling
            float u1 = (u + seed) * scale;
            float v1 = (v + seed) * scale;
            float u2 = (u + seed + 23.45f) * scale;
            float v2 = (v + seed + 23.45f) * scale;

            float n1 = Mathf.PerlinNoise(u1, v1);
            float n2 = Mathf.PerlinNoise(u2, v1);
            float n3 = Mathf.PerlinNoise(u1, v2);
            float n4 = Mathf.PerlinNoise(u2, v2);

            float bu = 0.5f - 0.5f * Mathf.Cos(u * Mathf.PI * 2f);
            float bv = 0.5f - 0.5f * Mathf.Cos(v * Mathf.PI * 2f);

            return Mathf.Lerp(Mathf.Lerp(n1, n2, bu), Mathf.Lerp(n3, n4, bu), bv);
        }

        private static void GenerateGrassTexture(int res, string albedoPath, string normalPath)
        {
            Texture2D albedo = new Texture2D(res, res, TextureFormat.RGBA32, true);
            float[,] heightMap = new float[res, res];

            Color deepGreen = new Color(0.24f, 0.44f, 0.16f);
            Color brightGreen = new Color(0.36f, 0.60f, 0.22f);
            Color goldenGreen = new Color(0.48f, 0.65f, 0.26f);
            Color soilGreen = new Color(0.28f, 0.32f, 0.16f);
            Color daisyYellow = new Color(0.96f, 0.88f, 0.32f);
            Color daisyWhite = new Color(0.95f, 0.95f, 0.90f);

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    // Multi-frequency noise
                    float broad = SampleSeamlessNoise(u, v, 3.0f, 10.5f);
                    float medium = SampleSeamlessNoise(u, v, 12.0f, 45.2f);
                    float fine = SampleSeamlessNoise(u, v, 32.0f, 89.1f);
                    float detail = SampleSeamlessNoise(u, v, 75.0f, 12.7f);

                    float h = broad * 0.4f + medium * 0.3f + fine * 0.2f + detail * 0.1f;
                    heightMap[x, y] = h;

                    // Base color gradient
                    Color c = Color.Lerp(deepGreen, brightGreen, broad);
                    c = Color.Lerp(c, goldenGreen, medium * 0.6f);
                    if (h < 0.35f)
                    {
                        c = Color.Lerp(c, soilGreen, (0.35f - h) * 2.5f);
                    }

                    // Blade fine highlights & shadows
                    c *= Mathf.Lerp(0.85f, 1.15f, fine);

                    // Scattered tiny wildflowers (daisies & buttercups)
                    float flowerNoise = SampleSeamlessNoise(u, v, 60.0f, 150.3f);
                    if (flowerNoise > 0.86f && h > 0.5f)
                    {
                        c = (flowerNoise > 0.91f) ? daisyYellow : daisyWhite;
                    }

                    c.a = 1.0f;
                    albedo.SetPixel(x, y, c);
                }
            }
            albedo.Apply();
            File.WriteAllBytes(albedoPath, albedo.EncodeToPNG());
            Object.DestroyImmediate(albedo);

            SaveNormalMapFromHeight(res, heightMap, 2.2f, normalPath);
        }

        private static void GenerateDirtPathTexture(int res, string albedoPath, string normalPath)
        {
            Texture2D albedo = new Texture2D(res, res, TextureFormat.RGBA32, true);
            float[,] heightMap = new float[res, res];

            Color earthDark = new Color(0.38f, 0.27f, 0.17f);
            Color earthWarm = new Color(0.50f, 0.38f, 0.25f);
            Color gravelLight = new Color(0.60f, 0.48f, 0.34f);
            Color pebbleGrey = new Color(0.46f, 0.44f, 0.40f);

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    float broad = SampleSeamlessNoise(u, v, 4.0f, 21.3f);
                    float medium = SampleSeamlessNoise(u, v, 14.0f, 63.8f);
                    float gravel = SampleSeamlessNoise(u, v, 48.0f, 95.1f);
                    float fine = SampleSeamlessNoise(u, v, 90.0f, 114.2f);

                    float h = broad * 0.35f + medium * 0.35f + gravel * 0.2f + fine * 0.1f;
                    heightMap[x, y] = h;

                    Color c = Color.Lerp(earthDark, earthWarm, broad);
                    c = Color.Lerp(c, gravelLight, medium * 0.4f);

                    // Pebbles
                    if (gravel > 0.78f)
                    {
                        c = Color.Lerp(c, pebbleGrey, (gravel - 0.78f) * 4.0f);
                        heightMap[x, y] += 0.15f;
                    }

                    c *= Mathf.Lerp(0.88f, 1.12f, fine);
                    c.a = 1.0f;
                    albedo.SetPixel(x, y, c);
                }
            }
            albedo.Apply();
            File.WriteAllBytes(albedoPath, albedo.EncodeToPNG());
            Object.DestroyImmediate(albedo);

            SaveNormalMapFromHeight(res, heightMap, 2.5f, normalPath);
        }

        private static void GenerateFarmlandTexture(int res, string albedoPath, string normalPath)
        {
            Texture2D albedo = new Texture2D(res, res, TextureFormat.RGBA32, true);
            float[,] heightMap = new float[res, res];

            Color furrowShadow = new Color(0.18f, 0.12f, 0.08f);
            Color darkSoil = new Color(0.26f, 0.18f, 0.12f);
            Color ridgeLight = new Color(0.35f, 0.25f, 0.17f);

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    // Plowed furrow ridges along V axis
                    float furrowBase = Mathf.Sin(v * 24.0f * Mathf.PI);
                    float warp = SampleSeamlessNoise(u, v, 6.0f, 33.2f) * 0.3f;
                    float furrow = Mathf.Sin((v + warp) * 24.0f * Mathf.PI);

                    float soilNoise = SampleSeamlessNoise(u, v, 36.0f, 77.4f);
                    float clod = SampleSeamlessNoise(u, v, 80.0f, 102.8f);

                    float ridgeProfile = furrow * 0.5f + 0.5f; // 0 (trough) to 1 (ridge crest)
                    float h = ridgeProfile * 0.7f + soilNoise * 0.2f + clod * 0.1f;
                    heightMap[x, y] = h;

                    Color c = Color.Lerp(furrowShadow, darkSoil, ridgeProfile);
                    c = Color.Lerp(c, ridgeLight, Mathf.Pow(ridgeProfile, 2.0f) * 0.8f);
                    c *= Mathf.Lerp(0.85f, 1.15f, clod);

                    c.a = 1.0f;
                    albedo.SetPixel(x, y, c);
                }
            }
            albedo.Apply();
            File.WriteAllBytes(albedoPath, albedo.EncodeToPNG());
            Object.DestroyImmediate(albedo);

            SaveNormalMapFromHeight(res, heightMap, 3.5f, normalPath);
        }

        private static void GenerateCobblestoneTexture(int res, string albedoPath, string normalPath)
        {
            Texture2D albedo = new Texture2D(res, res, TextureFormat.RGBA32, true);
            float[,] heightMap = new float[res, res];

            Color stoneLight = new Color(0.55f, 0.52f, 0.48f);
            Color stoneDark = new Color(0.40f, 0.38f, 0.35f);
            Color mortarDark = new Color(0.24f, 0.21f, 0.18f);

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    // Cellular stone pattern
                    float su = (u * 8.0f) % 1.0f;
                    float sv = (v * 8.0f) % 1.0f;
                    float distCenter = Mathf.Sqrt((su - 0.5f) * (su - 0.5f) + (sv - 0.5f) * (sv - 0.5f)) * 2.0f;

                    float stoneNoise = SampleSeamlessNoise(u, v, 24.0f, 14.1f);
                    float h = Mathf.Clamp01(1.0f - distCenter * 1.2f) * 0.8f + stoneNoise * 0.2f;
                    heightMap[x, y] = h;

                    Color c;
                    if (distCenter > 0.82f)
                    {
                        c = mortarDark;
                        heightMap[x, y] *= 0.2f;
                    }
                    else
                    {
                        c = Color.Lerp(stoneDark, stoneLight, h);
                    }

                    c.a = 1.0f;
                    albedo.SetPixel(x, y, c);
                }
            }
            albedo.Apply();
            File.WriteAllBytes(albedoPath, albedo.EncodeToPNG());
            Object.DestroyImmediate(albedo);

            SaveNormalMapFromHeight(res, heightMap, 3.0f, normalPath);
        }

        private static void SaveNormalMapFromHeight(int res, float[,] height, float strength, string path)
        {
            Texture2D normalTex = new Texture2D(res, res, TextureFormat.RGBA32, true);

            for (int y = 0; y < res; y++)
            {
                int yPrev = (y - 1 + res) % res;
                int yNext = (y + 1) % res;

                for (int x = 0; x < res; x++)
                {
                    int xPrev = (x - 1 + res) % res;
                    int xNext = (x + 1) % res;

                    // Sobel filter
                    float tl = height[xPrev, yNext];
                    float tc = height[x, yNext];
                    float tr = height[xNext, yNext];
                    float ml = height[xPrev, y];
                    float mr = height[xNext, y];
                    float bl = height[xPrev, yPrev];
                    float bc = height[x, yPrev];
                    float br = height[xNext, yPrev];

                    float dX = ((tr + 2f * mr + br) - (tl + 2f * ml + bl));
                    float dY = ((tl + 2f * tc + tr) - (bl + 2f * bc + br));

                    Vector3 n = new Vector3(-dX * strength, -dY * strength, 1.0f).normalized;
                    Color nc = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1.0f);
                    normalTex.SetPixel(x, y, nc);
                }
            }
            normalTex.Apply();
            File.WriteAllBytes(path, normalTex.EncodeToPNG());
            Object.DestroyImmediate(normalTex);
        }

        #endregion

        #region Material Creation

        private static Material CreateOrGetGrassMaterial()
        {
            string matPath = $"{MaterialsFolder}/M_FarmGrass.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_FarmGrass_Albedo.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_FarmGrass_Normal.png");

            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", new Vector2(24.0f, 24.0f));
            mat.SetColor("_BaseColor", Color.white);

            if (normal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", new Vector2(24.0f, 24.0f));
                mat.SetFloat("_BumpScale", 0.85f);
            }

            mat.SetFloat("_Smoothness", 0.15f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetDirtPathMaterial()
        {
            string matPath = $"{MaterialsFolder}/M_FarmDirtPath.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_DirtPath_Albedo.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_DirtPath_Normal.png");

            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", new Vector2(6.0f, 6.0f));
            mat.SetColor("_BaseColor", Color.white);

            if (normal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", new Vector2(6.0f, 6.0f));
                mat.SetFloat("_BumpScale", 1.0f);
            }

            mat.SetFloat("_Smoothness", 0.10f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetFarmlandMaterial()
        {
            string matPath = $"{MaterialsFolder}/M_FarmlandSoil.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Farmland_Albedo.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Farmland_Normal.png");

            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", new Vector2(4.0f, 4.0f));
            mat.SetColor("_BaseColor", Color.white);

            if (normal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", new Vector2(4.0f, 4.0f));
                mat.SetFloat("_BumpScale", 1.4f);
            }

            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetCobblestoneMaterial()
        {
            string matPath = $"{MaterialsFolder}/M_Cobblestone.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Cobblestone_Albedo.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/T_Cobblestone_Normal.png");

            mat.SetTexture("_BaseMap", albedo);
            mat.SetTextureScale("_BaseMap", new Vector2(3.0f, 3.0f));
            mat.SetColor("_BaseColor", Color.white);

            if (normal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", new Vector2(3.0f, 3.0f));
                mat.SetFloat("_BumpScale", 1.2f);
            }

            mat.SetFloat("_Smoothness", 0.18f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetFenceMaterial()
        {
            string matPath = $"{MaterialsFolder}/M_RusticFence.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // Warm rustic weathered wood tone
            mat.SetColor("_BaseColor", new Color(0.42f, 0.28f, 0.17f));
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        #endregion

        #region Lighting, Fog & Atmosphere

        public static void ConfigureAtmosphereAndLighting()
        {
            // 1. Sun (Directional Light)
            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        sunObj = l.gameObject;
                        break;
                    }
                }
            }

            if (sunObj != null)
            {
                Undo.RecordObject(sunObj.transform, "Atmosphere Sun Transform");
                // Warm golden angle casting long, dramatic farmstead shadows
                sunObj.transform.rotation = Quaternion.Euler(42.0f, -38.0f, 0.0f);

                Light sunLight = sunObj.GetComponent<Light>();
                if (sunLight != null)
                {
                    Undo.RecordObject(sunLight, "Atmosphere Sun Light");
                    // Warm golden sunlight
                    sunLight.color = new Color(1.0f, 0.95f, 0.86f);
                    sunLight.intensity = 1.25f;
                    sunLight.shadows = LightShadows.Soft;
                    sunLight.shadowStrength = 0.85f;
                    sunLight.shadowBias = 0.05f;
                    sunLight.shadowNormalBias = 0.4f;
                }
            }

            // 2. RenderSettings: Sky, Ambient bounce & Warm Horizon Fog
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.65f, 0.78f, 0.92f);     // Soft warm sky
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.80f, 0.72f); // Warm countryside haze
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.48f, 0.22f);  // Vibrant grass bounce
            RenderSettings.ambientIntensity = 1.15f;

            // Countryside horizon fog: smoothly blends ground edge into sky
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 35.0f;
            RenderSettings.fogEndDistance = 88.0f;
            RenderSettings.fogColor = new Color(0.76f, 0.85f, 0.88f);

            // 3. Post-Processing Profile (Global Volume)
            SetupGlobalVolumeProfile();
        }

        private static void SetupGlobalVolumeProfile()
        {
            string profilePath = $"{SettingsFolder}/FarmVolumeProfile.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            // Color Adjustments: warm, rich, vibrant farm palette
            if (!profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.postExposure.overrideState = true;
            colorAdj.postExposure.value = 0.15f;
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 14.0f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 18.0f;

            // Tonemapping: Neutral filmic curve
            if (!profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.Neutral;

            // Bloom: subtle golden sun bloom
            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.05f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.28f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.65f;

            // Vignette: subtle lens focus
            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.18f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.35f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            // Assign profile to scene's Global Volume
            GameObject volumeObj = GameObject.Find("Global Volume");
            if (volumeObj == null)
            {
                volumeObj = new GameObject("Global Volume");
                Undo.RegisterCreatedObjectUndo(volumeObj, "Create Global Volume");
            }

            Volume vol = volumeObj.GetComponent<Volume>();
            if (vol == null) vol = volumeObj.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.profile = profile;
            vol.weight = 1.0f;
        }

        #endregion

        #region Environment Geometry & Layout

        private static void SetupGroundPlane(Material grassMat)
        {
            GameObject ground = GameObject.Find("Ground_TestPlane_20x20");
            if (ground != null)
            {
                Undo.RecordObject(ground.transform, "Expand Ground to 80x80");
                ground.transform.localScale = new Vector3(8.0f, 1.0f, 8.0f);
                ground.transform.position = new Vector3(0.0f, 0.0f, 15.0f);

                MeshRenderer mr = ground.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Undo.RecordObject(mr, "Set Ground Material");
                    mr.sharedMaterial = grassMat;
                }
            }
        }

        public static void BuildFarmWorldEnvironment(Material grassMat, Material dirtMat, Material soilMat, Material cobbleMat, Material fenceMat)
        {
            GameObject envRoot = GameObject.Find("[Farm_Environment]");
            if (envRoot != null)
            {
                Undo.DestroyObjectImmediate(envRoot);
            }

            envRoot = new GameObject("[Farm_Environment]");
            Undo.RegisterCreatedObjectUndo(envRoot, "Create [Farm_Environment]");
            envRoot.transform.position = Vector3.zero;

            // 1. Organic Dirt Pathways
            BuildPathways(envRoot.transform, dirtMat);

            // 2. Cobblestone Stepping Stones around cottage & well
            BuildSteppingPavers(envRoot.transform, cobbleMat);

            // 3. Tilled Farmland Plot with Raised Wooden Beds & Crops
            BuildFarmlandPlots(envRoot.transform, soilMat, fenceMat);

            // 4. Perimeter Wooden Fences with Entrance Gate
            BuildPerimeterFences(envRoot.transform, fenceMat);

            // 5. Rolling Countryside Boundary Hills
            BuildPerimeterHills(envRoot.transform, grassMat);

            // 6. Cozy Farm Lantern Post
            BuildLanternPost(envRoot.transform, fenceMat);
        }

        private static void BuildPathways(Transform parent, Material dirtMat)
        {
            GameObject pathGroup = new GameObject("Dirt_Pathways");
            pathGroup.transform.SetParent(parent, false);

            // Smooth curved pathway segments connecting key locations
            // Main Path: Signpost (2.5, 0, 4.5) -> Courtyard (2.0, 0, 9.0) -> Cottage Door (1.8, 0, 13.5)
            Vector3[] mainPoints = new[]
            {
                new Vector3(2.5f, 0.015f, 4.2f),
                new Vector3(2.4f, 0.015f, 6.5f),
                new Vector3(2.1f, 0.015f, 9.0f),
                new Vector3(1.8f, 0.015f, 11.5f),
                new Vector3(1.6f, 0.015f, 13.8f)
            };
            CreateCurvedPathMesh("Main_Path", pathGroup.transform, mainPoints, 2.2f, dirtMat);

            // Well Branch: Courtyard (2.1, 0, 9.0) -> Well (-2.5, 0, 11.5)
            Vector3[] wellPoints = new[]
            {
                new Vector3(2.0f, 0.016f, 9.0f),
                new Vector3(0.5f, 0.016f, 10.0f),
                new Vector3(-1.2f, 0.016f, 10.8f),
                new Vector3(-2.5f, 0.016f, 11.5f)
            };
            CreateCurvedPathMesh("Well_Branch", pathGroup.transform, wellPoints, 1.8f, dirtMat);

            // Workshop Branch: Well (-2.5, 0, 11.5) -> Table & Woodcutting (-4.8, 0, 12.2)
            Vector3[] shopPoints = new[]
            {
                new Vector3(-2.5f, 0.016f, 11.5f),
                new Vector3(-3.8f, 0.016f, 11.8f),
                new Vector3(-5.0f, 0.016f, 12.2f)
            };
            CreateCurvedPathMesh("Workshop_Branch", pathGroup.transform, shopPoints, 2.0f, dirtMat);

            // Farmland Branch: Signpost area (2.4, 0, 6.5) -> Farmland plot (-5.5, 0, 7.5)
            Vector3[] farmPoints = new[]
            {
                new Vector3(2.4f, 0.016f, 6.5f),
                new Vector3(0.2f, 0.016f, 6.8f),
                new Vector3(-2.8f, 0.016f, 7.2f),
                new Vector3(-5.5f, 0.016f, 7.5f)
            };
            CreateCurvedPathMesh("Farmland_Branch", pathGroup.transform, farmPoints, 1.8f, dirtMat);
        }

        private static void CreateCurvedPathMesh(string name, Transform parent, Vector3[] points, float width, Material mat)
        {
            if (points == null || points.Length < 2) return;

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            int count = points.Length;
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uvs = new Vector2[count * 2];
            int[] triangles = new int[(count - 1) * 6];

            float halfW = width * 0.5f;
            float totalLen = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 forward;
                if (i == 0) forward = (points[1] - points[0]).normalized;
                else if (i == count - 1) forward = (points[count - 1] - points[count - 2]).normalized;
                else forward = (points[i + 1] - points[i - 1]).normalized;

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                if (i > 0) totalLen += Vector3.Distance(points[i], points[i - 1]);

                vertices[i * 2] = points[i] - right * halfW;
                vertices[i * 2 + 1] = points[i] + right * halfW;

                float uvY = totalLen * 0.5f;
                uvs[i * 2] = new Vector2(0f, uvY);
                uvs[i * 2 + 1] = new Vector2(1f, uvY);
            }

            int triIndex = 0;
            for (int i = 0; i < count - 1; i++)
            {
                int bl = i * 2;
                int br = i * 2 + 1;
                int tl = (i + 1) * 2;
                int tr = (i + 1) * 2 + 1;

                triangles[triIndex++] = bl;
                triangles[triIndex++] = tl;
                triangles[triIndex++] = br;

                triangles[triIndex++] = br;
                triangles[triIndex++] = tl;
                triangles[triIndex++] = tr;
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }

        private static void BuildSteppingPavers(Transform parent, Material cobbleMat)
        {
            GameObject paverGroup = new GameObject("Stepping_Pavers");
            paverGroup.transform.SetParent(parent, false);

            Vector3[] paverSpots = new[]
            {
                new Vector3(1.6f, 0.02f, 13.2f),
                new Vector3(1.2f, 0.02f, 13.8f),
                new Vector3(0.8f, 0.02f, 14.3f),
                new Vector3(-2.5f, 0.02f, 10.6f),
                new Vector3(-3.2f, 0.02f, 11.2f),
                new Vector3(-4.2f, 0.02f, 12.0f)
            };

            for (int i = 0; i < paverSpots.Length; i++)
            {
                GameObject paver = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                paver.name = $"Paver_{i + 1}";
                paver.transform.SetParent(paverGroup.transform, false);
                paver.transform.position = paverSpots[i];
                paver.transform.localScale = new Vector3(0.65f, 0.015f, 0.65f);
                paver.transform.rotation = Quaternion.Euler(0, (i * 47f) % 360f, 0);

                // Remove collider to keep walking completely smooth
                Collider col = paver.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);

                MeshRenderer mr = paver.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = cobbleMat;
            }
        }

        private static void BuildFarmlandPlots(Transform parent, Material soilMat, Material fenceMat)
        {
            GameObject farmGroup = new GameObject("Farmland_Plots");
            farmGroup.transform.SetParent(parent, false);
            farmGroup.transform.position = new Vector3(-8.0f, 0.0f, 7.5f);

            // Two 4.5m x 2.2m raised tilled soil beds
            for (int b = 0; b < 2; b++)
            {
                float zOffset = (b == 0) ? -1.6f : 1.6f;
                GameObject bed = new GameObject($"CropBed_{b + 1}");
                bed.transform.SetParent(farmGroup.transform, false);
                bed.transform.localPosition = new Vector3(0f, 0f, zOffset);

                // Raised Soil Mesh
                GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
                soil.name = "Tilled_Soil";
                soil.transform.SetParent(bed.transform, false);
                soil.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                soil.transform.localScale = new Vector3(4.6f, 0.12f, 2.2f);
                Collider sc = soil.GetComponent<Collider>();
                if (sc != null) Object.DestroyImmediate(sc);
                soil.GetComponent<MeshRenderer>().sharedMaterial = soilMat;

                // Wooden plank border frame
                BuildPlankBorder(bed.transform, 4.8f, 2.4f, 0.16f, fenceMat);

                // Neat rows of sprouting green crops (carrots/cabbages)
                BuildSproutingCrops(bed.transform);
            }
        }

        private static void BuildPlankBorder(Transform parent, float width, float length, float height, Material woodMat)
        {
            GameObject border = new GameObject("Wooden_Border");
            border.transform.SetParent(parent, false);

            float beamThick = 0.12f;

            // North beam
            CreateBeam(border.transform, new Vector3(0f, height * 0.5f, length * 0.5f), new Vector3(width, height, beamThick), woodMat);
            // South beam
            CreateBeam(border.transform, new Vector3(0f, height * 0.5f, -length * 0.5f), new Vector3(width, height, beamThick), woodMat);
            // West beam
            CreateBeam(border.transform, new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(beamThick, height, length), woodMat);
            // East beam
            CreateBeam(border.transform, new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(beamThick, height, length), woodMat);
        }

        private static void CreateBeam(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "Plank";
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = localPos;
            beam.transform.localScale = scale;
            beam.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void BuildSproutingCrops(Transform bedParent)
        {
            GameObject cropsGroup = new GameObject("Sprouting_Crops");
            cropsGroup.transform.SetParent(bedParent, false);

            // Material for cute green sprouts
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            Material cropMat = new Material(litShader);
            cropMat.SetColor("_BaseColor", new Color(0.38f, 0.72f, 0.22f));
            cropMat.SetFloat("_Smoothness", 0.3f);

            // 3 rows along X, 5 plants per row
            for (int r = -1; r <= 1; r++)
            {
                float z = r * 0.65f;
                for (int c = -2; c <= 2; c++)
                {
                    float x = c * 0.9f;
                    GameObject sprout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sprout.name = $"Crop_{r}_{c}";
                    sprout.transform.SetParent(cropsGroup.transform, false);
                    sprout.transform.localPosition = new Vector3(x, 0.14f, z);
                    sprout.transform.localScale = new Vector3(0.24f, 0.18f, 0.24f);

                    Collider sc = sprout.GetComponent<Collider>();
                    if (sc != null) Object.DestroyImmediate(sc);
                    sprout.GetComponent<MeshRenderer>().sharedMaterial = cropMat;
                }
            }
        }

        private static void BuildPerimeterFences(Transform parent, Material fenceMat)
        {
            GameObject fenceGroup = new GameObject("Farm_Fences");
            fenceGroup.transform.SetParent(parent, false);

            // Boundary yard: X: -13 to +11, Z: 3 to 21
            // Front fence with opening at entrance [X = 0.5 to 3.5]
            BuildFenceLine(fenceGroup.transform, new Vector3(-13f, 0f, 3f), new Vector3(0.5f, 0f, 3f), 2.4f, fenceMat);
            BuildFenceLine(fenceGroup.transform, new Vector3(3.8f, 0f, 3f), new Vector3(11f, 0f, 3f), 2.4f, fenceMat);

            // Left fence
            BuildFenceLine(fenceGroup.transform, new Vector3(-13f, 0f, 3f), new Vector3(-13f, 0f, 21f), 2.5f, fenceMat);

            // Right fence
            BuildFenceLine(fenceGroup.transform, new Vector3(11f, 0f, 3f), new Vector3(11f, 0f, 21f), 2.5f, fenceMat);

            // Back fence
            BuildFenceLine(fenceGroup.transform, new Vector3(-13f, 0f, 21f), new Vector3(11f, 0f, 21f), 2.5f, fenceMat);
        }

        private static void BuildFenceLine(Transform parent, Vector3 start, Vector3 end, float postSpacing, Material mat)
        {
            float dist = Vector3.Distance(start, end);
            int postCount = Mathf.Max(2, Mathf.RoundToInt(dist / postSpacing) + 1);
            Vector3 dir = (end - start).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);

            Vector3[] postPositions = new Vector3[postCount];
            for (int i = 0; i < postCount; i++)
            {
                float t = (float)i / (postCount - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                postPositions[i] = pos;

                // Sturdy vertical post
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Fence_Post";
                post.transform.SetParent(parent, false);
                post.transform.position = pos + Vector3.up * 0.65f;
                // Slight organic rotation jitter for hand-built rustic charm
                float jitter = Mathf.Sin(pos.x * 3.7f + pos.z * 2.1f) * 2.5f;
                post.transform.rotation = Quaternion.Euler(jitter, rot.eulerAngles.y, jitter * 0.5f);
                post.transform.localScale = new Vector3(0.18f, 0.65f, 0.18f);
                post.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            // Dual horizontal rails
            for (int i = 0; i < postCount - 1; i++)
            {
                Vector3 p1 = postPositions[i];
                Vector3 p2 = postPositions[i + 1];
                Vector3 mid = (p1 + p2) * 0.5f;
                float segLen = Vector3.Distance(p1, p2);

                for (int r = 0; r < 2; r++)
                {
                    float y = (r == 0) ? 0.45f : 0.95f;
                    GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rail.name = "Fence_Rail";
                    rail.transform.SetParent(parent, false);
                    rail.transform.position = new Vector3(mid.x, y, mid.z);
                    rail.transform.rotation = rot;
                    rail.transform.localScale = new Vector3(0.08f, 0.12f, segLen);
                    rail.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
        }

        private static void BuildPerimeterHills(Transform parent, Material grassMat)
        {
            GameObject hillsGroup = new GameObject("Perimeter_Rolling_Hills");
            hillsGroup.transform.SetParent(parent, false);

            // Ring of rolling green mounds around the 80m boundary (radius 34m - 42m)
            // Completely eliminates the "flat plane in void" look and frames the farm in a cozy valley
            (Vector3 pos, Vector3 scale)[] hillData = new[]
            {
                (new Vector3(-32f, -1.5f, 5f), new Vector3(32f, 7f, 28f)),
                (new Vector3(-28f, -2f, 25f), new Vector3(30f, 8f, 30f)),
                (new Vector3(0f, -2f, 38f), new Vector3(38f, 9f, 26f)),
                (new Vector3(28f, -2f, 26f), new Vector3(32f, 8f, 28f)),
                (new Vector3(30f, -1.8f, 5f), new Vector3(28f, 7f, 26f)),
                (new Vector3(25f, -2.2f, -15f), new Vector3(34f, 8f, 30f)),
                (new Vector3(-5f, -2f, -18f), new Vector3(36f, 8f, 26f)),
                (new Vector3(-28f, -2f, -12f), new Vector3(30f, 7f, 28f))
            };

            foreach (var h in hillData)
            {
                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = "Rolling_Hill";
                hill.transform.SetParent(hillsGroup.transform, false);
                hill.transform.position = h.pos;
                hill.transform.localScale = h.scale;

                // Keep collider so player doesn't clip if exploring boundary
                hill.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
            }
        }

        private static void BuildLanternPost(Transform parent, Material woodMat)
        {
            GameObject lanternObj = new GameObject("Farm_Lantern_Post");
            lanternObj.transform.SetParent(parent, false);
            lanternObj.transform.position = new Vector3(0.6f, 0.0f, 8.5f);

            // Vertical timber post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(lanternObj.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            post.transform.localScale = new Vector3(0.16f, 1.3f, 0.16f);
            post.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Horizontal cross arm
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "CrossArm";
            arm.transform.SetParent(lanternObj.transform, false);
            arm.transform.localPosition = new Vector3(0.35f, 2.5f, 0f);
            arm.transform.localScale = new Vector3(0.8f, 0.12f, 0.12f);
            arm.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Lantern housing
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            Material glowMat = new Material(litShader);
            Color amber = new Color(1.0f, 0.76f, 0.35f);
            glowMat.SetColor("_BaseColor", amber);
            glowMat.SetColor("_EmissionColor", amber * 2.5f);
            glowMat.EnableKeyword("_EMISSION");

            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lantern.name = "Lantern_Glass";
            lantern.transform.SetParent(lanternObj.transform, false);
            lantern.transform.localPosition = new Vector3(0.65f, 2.2f, 0f);
            lantern.transform.localScale = new Vector3(0.24f, 0.35f, 0.24f);
            Collider lc = lantern.GetComponent<Collider>();
            if (lc != null) Object.DestroyImmediate(lc);
            lantern.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

            // Warm Glowing Point Light
            GameObject lightObj = new GameObject("Lantern_PointLight");
            lightObj.transform.SetParent(lantern.transform, false);
            lightObj.transform.localPosition = Vector3.zero;

            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = amber;
            pLight.intensity = 2.2f;
            pLight.range = 8.5f;
            pLight.shadows = LightShadows.Soft;
        }

        #endregion
    }
}
#endif
