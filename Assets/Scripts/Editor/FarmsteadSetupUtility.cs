#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace FarmASU.Editor
{
    /// <summary>
    /// Utility to set up the Witch's House asset pack into MainScene.
    /// Rescales all oversized 3D models to realistic, natural human proportions matching Peasant Man,
    /// positions table decor flush on tabletop, adds solid physical colliders,
    /// and expands the ground into a spacious 80x80m farmstead pasture.
    /// </summary>
    public static class FarmsteadSetupUtility
    {
        private const string PackFolder = "Assets/Bizulka/Witchs_house";
        private const string PrefabsFolder = PackFolder + "/Prefabs";
        private const string MaterialsFolder = PackFolder + "/Materials";

        // Accurate human scale factors calibrated against Peasant Man (1.8m tall):
        // Table: ~0.81m waist height | Stool: ~0.44m knee height | Well: ~2.2m overhead roof, 0.84m stone rim
        // Broom: ~1.36m chest height | Axe: ~0.76m hand axe | Pointer: ~2.17m eye level signpost
        // House: ~6.43m cozy 2-story cottage | Vessels/Mugs: ~0.25m - 0.35m realistic table jugs
        private static readonly (string prefabName, Vector3 pos, Vector3 rot, Vector3 scale, bool isMeshCollider, bool isConvex)[] Items = new[]
        {
            // 1. Cozy Cottage (scaled to 70% -> 6.4m tall, 4.5m wide, 5.5m deep)
            ("Witchs_house", new Vector3(2.5f, 0.0f, 16.0f), Vector3.zero, new Vector3(0.70f, 0.70f, 0.70f), true, false),

            // 2. Stone Water Well (scaled to 48% -> 2.2m total height, stone rim at waist level)
            ("well", new Vector3(-2.5f, 0.0f, 11.5f), new Vector3(0, 35.0f, 0), new Vector3(0.48f, 0.48f, 0.48f), true, false),

            // 3. Outdoor Dining / Crafting Table & Stool (table top at Y = 0.81m)
            ("table", new Vector3(-5.0f, 0.0f, 12.0f), new Vector3(0, -20.0f, 0), new Vector3(0.42f, 0.42f, 0.42f), true, true),
            ("stool", new Vector3(-4.0f, 0.0f, 12.3f), new Vector3(0, 45.0f, 0), new Vector3(0.50f, 0.50f, 0.50f), true, true),

            // 4. Tabletop Decor (flush on top of table at Y = 0.81m)
            ("vessel", new Vector3(-4.8f, 0.81f, 12.0f), Vector3.zero, new Vector3(0.18f, 0.18f, 0.18f), false, false),
            ("vessel_1", new Vector3(-5.2f, 0.81f, 11.8f), Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), false, false),
            ("vessel_2", new Vector3(-5.3f, 0.81f, 12.2f), Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), false, false),
            ("vessel_3", new Vector3(-4.3f, 0.00f, 11.3f), Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), false, false), // on ground by table

            // 5. Woodcutter Station (stacked logs, chopped wood, hand axe)
            ("frewood", new Vector3(-3.5f, 0.0f, 15.5f), new Vector3(0, 25.0f, 0), new Vector3(0.55f, 0.55f, 0.55f), true, true),
            ("frewood_1", new Vector3(-4.8f, 0.0f, 15.0f), new Vector3(0, -35.0f, 0), new Vector3(0.55f, 0.55f, 0.55f), true, true),
            ("axe", new Vector3(-4.1f, 0.0f, 15.2f), new Vector3(0, 45.0f, 0), new Vector3(0.35f, 0.35f, 0.35f), false, false),

            // 6. Porch & Entrance Props (Broom leaning by door, notice board, stone path)
            ("Broom", new Vector3(0.9f, 0.0f, 13.8f), new Vector3(0, 20.0f, -8.0f), new Vector3(0.30f, 0.30f, 0.30f), false, false),
            ("Board", new Vector3(-0.5f, 0.0f, 14.0f), new Vector3(0, -30.0f, 0), new Vector3(0.45f, 0.45f, 0.45f), true, true),
            ("Stones", new Vector3(1.5f, 0.02f, 10.0f), Vector3.zero, new Vector3(0.65f, 0.65f, 0.65f), false, false),

            // 7. Cauldron / Vat & Scoop
            ("vat", new Vector3(-2.8f, 0.0f, 17.5f), Vector3.zero, new Vector3(0.35f, 0.35f, 0.35f), true, true),
            ("scoop", new Vector3(-2.4f, 0.05f, 17.0f), new Vector3(0, 30.0f, 0), new Vector3(0.35f, 0.35f, 0.35f), false, false),

            // 8. Directional Wooden Signpost (eye level at 2.17m)
            ("Pointer", new Vector3(2.5f, 0.0f, 5.0f), new Vector3(0, -60.0f, 0), new Vector3(0.25f, 0.25f, 0.25f), true, true)
        };

        [MenuItem("FarmASU/Setup Farm House & Environment in Scene")]
        public static void SetupFarmstead()
        {
            Debug.Log("[FarmsteadSetup] Starting Farmstead setup with realistic human proportions...");

            Scene activeScene = SceneManager.GetActiveScene();

            // 1. Remove existing Farmstead root if re-running
            GameObject existingRoot = GameObject.Find("[Farmstead]");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
                Debug.Log("[FarmsteadSetup] Removed previous [Farmstead] hierarchy.");
            }

            // 2. Expand Ground plane so there is a vast, beautiful farm pasture (80m x 80m)
            GameObject ground = GameObject.Find("Ground_TestPlane_20x20");
            if (ground != null)
            {
                Undo.RecordObject(ground.transform, "Expand Ground for Farmstead");
                ground.transform.localScale = new Vector3(8.0f, 1.0f, 8.0f);
                ground.transform.position = new Vector3(0.0f, 0.0f, 15.0f);
                Debug.Log("[FarmsteadSetup] Expanded Ground plane to 80x80 meters.");
            }

            // 3. Create clean Farmstead parent
            GameObject farmsteadRoot = new GameObject("[Farmstead]");
            Undo.RegisterCreatedObjectUndo(farmsteadRoot, "Create [Farmstead]");
            farmsteadRoot.transform.position = Vector3.zero;

            int placedCount = 0;
            foreach (var item in Items)
            {
                string prefabPath = $"{PrefabsFolder}/{item.prefabName}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[FarmsteadSetup] Prefab not found at path: {prefabPath}");
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, farmsteadRoot.transform);
                instance.transform.position = item.pos;
                instance.transform.rotation = Quaternion.Euler(item.rot);
                instance.transform.localScale = item.scale;

                // Add physics colliders so the player does not walk through walls/props
                if (item.isMeshCollider)
                {
                    MeshFilter mf = instance.GetComponentInChildren<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        MeshCollider mc = instance.GetComponent<MeshCollider>();
                        if (mc == null) mc = instance.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = item.isConvex;
                    }
                }

                placedCount++;
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"[FarmsteadSetup] SUCCESS: Placed {placedCount} farmstead structures at human scale with solid colliders!");
        }

        [MenuItem("FarmASU/Rescale Existing Farmstead to Human Scale")]
        public static void RescaleExistingFarmstead()
        {
            GameObject farmsteadRoot = GameObject.Find("[Farmstead]");
            if (farmsteadRoot == null)
            {
                Debug.LogWarning("[FarmsteadSetup] No [Farmstead] object found in active scene. Running full Setup instead...");
                SetupFarmstead();
                return;
            }

            Undo.RecordObject(farmsteadRoot.transform, "Rescale Farmstead");

            int adjusted = 0;
            foreach (var item in Items)
            {
                Transform child = farmsteadRoot.transform.Find(item.prefabName);
                if (child != null)
                {
                    Undo.RecordObject(child, "Rescale Child");
                    child.position = item.pos;
                    child.rotation = Quaternion.Euler(item.rot);
                    child.localScale = item.scale;

                    if (item.isMeshCollider)
                    {
                        MeshCollider mc = child.GetComponent<MeshCollider>();
                        if (mc != null)
                        {
                            mc.convex = item.isConvex;
                        }
                    }

                    adjusted++;
                }
            }

            EditorSceneManager.MarkSceneDirty(farmsteadRoot.scene);
            Debug.Log($"[FarmsteadSetup] SUCCESS: Rescaled {adjusted} existing objects in [Farmstead] to natural human scale!");
        }
    }
}
#endif
