#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmASU.Editor
{
    public static class ValleySetupUtility
    {
        private const string DemoScenePath = "Assets/ALP_Assets/GrassFlowersFREE/Demo/DemoGrassFlowers.unity";
        private const string MainScenePath = "Assets/Scenes/Main/MainScene.unity";
        private const string ValleyAssetFolder = "Assets/Art/Environment";
        private const string TerrainDataAssetPath = ValleyAssetFolder + "/FarmValleyTerrainData.asset";

        [MenuItem("FarmASU/Setup Valley World in MainScene")]
        public static void SetupValleyWorldInMainScene()
        {
            Debug.Log("[ValleySetup] Starting Valley World setup...");

            if (!Directory.Exists(ValleyAssetFolder))
            {
                Directory.CreateDirectory(ValleyAssetFolder);
                AssetDatabase.Refresh();
            }

            // Ensure MainScene is open
            Scene mainScene = SceneManager.GetActiveScene();
            if (mainScene.path != MainScenePath)
            {
                mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }

            // 1. Open DemoGrassFlowers additively to inspect and extract Terrain
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
                Debug.LogError("[ValleySetup] Could not find Terrain with TerrainData in DemoGrassFlowers scene!");
                EditorSceneManager.CloseScene(demoScene, true);
                return;
            }

            Debug.Log($"[ValleySetup] Found Demo Terrain: size={demoTerrain.terrainData.size}, details={demoTerrain.terrainData.detailPrototypes.Length}, layers={demoTerrain.terrainData.terrainLayers.Length}");

            // 2. Clone TerrainData so MainScene has its own independent asset
            TerrainData clonedData = Object.Instantiate(demoTerrain.terrainData);
            clonedData.name = "FarmValleyTerrainData";

            // If an asset already exists, replace or overwrite it
            if (File.Exists(TerrainDataAssetPath))
            {
                AssetDatabase.DeleteAsset(TerrainDataAssetPath);
            }
            AssetDatabase.CreateAsset(clonedData, TerrainDataAssetPath);
            AssetDatabase.SaveAssets();

            // Store detail settings
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

            // Center the terrain so the valley center is around (0, 0, 15) where the farmstead sits
            Vector3 tSize = clonedData.size;
            // E.g. offset so (tSize.x * 0.5, 0, tSize.z * 0.5) is at (0, 0, 15)
            float tPosX = -tSize.x * 0.5f;
            float tPosZ = -tSize.z * 0.5f + 15.0f;
            terrainObj.transform.position = new Vector3(tPosX, 0.0f, tPosZ);

            // Sample ground height at farmstead center to ensure house and player sit on surface
            float groundY = terrain.SampleHeight(new Vector3(0, 0, 15));
            Debug.Log($"[ValleySetup] Valley terrain placed. Ground height at farmstead center = {groundY:F2}m");

            // 6. Ensure Player is positioned properly on the ground
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                Undo.RecordObject(player.transform, "Position Player on Valley");
                float pY = terrain.SampleHeight(player.transform.position) + terrainObj.transform.position.y;
                player.transform.position = new Vector3(player.transform.position.x, Mathf.Max(pY + 0.1f, 0.1f), player.transform.position.z);
            }

            // 7. Setup the Farmstead cottage and props on the valley floor
            FarmsteadSetupUtility.SetupFarmstead();

            // Adjust Farmstead height to match terrain
            GameObject farmstead = GameObject.Find("[Farmstead]");
            if (farmstead != null)
            {
                float fY = terrain.SampleHeight(new Vector3(2.5f, 0, 16.0f)) + terrainObj.transform.position.y;
                farmstead.transform.position = new Vector3(0, fY, 0);
            }

            // 8. Configure atmospheric sunlight and fog to complement the vast valley
            ConfigureValleyAtmosphere();

            EditorSceneManager.MarkSceneDirty(mainScene);
            Debug.Log("[ValleySetup] === VAST VALLEY WORLD SETUP COMPLETE! ===");
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

            // Distant soft valley mist
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60.0f;
            RenderSettings.fogEndDistance = 280.0f;
            RenderSettings.fogColor = new Color(0.78f, 0.86f, 0.90f);
        }
    }
}
#endif
