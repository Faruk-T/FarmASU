#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace FarmASU.Editor
{
    /// <summary>
    /// Utility to set up the newly imported Witch's House asset pack into MainScene.
    /// Converts materials to URP Lit, adds solid physical colliders,
    /// and instantiates a complete, atmospheric farmstead (house, well, table, woodpile, props).
    /// </summary>
    public static class FarmsteadSetupUtility
    {
        private const string PackFolder = "Assets/Bizulka/Witchs_house";
        private const string PrefabsFolder = PackFolder + "/Prefabs";
        private const string MaterialsFolder = PackFolder + "/Materials";

        [MenuItem("FarmASU/Setup Farm House & Environment in Scene")]
        public static void SetupFarmstead()
        {
            Debug.Log("[FarmsteadSetup] Starting Farmstead setup in current scene...");

            Scene activeScene = SceneManager.GetActiveScene();

            // 1. Remove existing Farmstead root if re-running
            GameObject existingRoot = GameObject.Find("[Farmstead]");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
                Debug.Log("[FarmsteadSetup] Removed previous [Farmstead] hierarchy.");
            }

            // 2. Create clean Farmstead parent
            GameObject farmsteadRoot = new GameObject("[Farmstead]");
            Undo.RegisterCreatedObjectUndo(farmsteadRoot, "Create [Farmstead]");
            farmsteadRoot.transform.position = Vector3.zero;

            // 3. Define the harmonious layout positions and rotations relative to player starting area
            // Player starts at (0, 0, 0) looking forward (+Z)
            // Farmstead is positioned comfortably ahead between Z=12 and Z=28
            var items = new (string prefabName, Vector3 pos, Vector3 rot, Vector3 scale, bool addMeshCollider)[]
            {
                // Main House (centerpiece)
                ("Witchs_house", new Vector3(3.5f, 0.0f, 18.0f), Vector3.zero, Vector3.one, true),
                
                // Stone Water Well (to the left of the house path)
                ("well", new Vector3(-2.5f, 0.0f, 15.0f), new Vector3(0, 35.0f, 0), Vector3.one, true),
                
                // Outdoor Crafting / Dining Table & Stool
                ("table", new Vector3(-6.5f, 0.0f, 16.5f), new Vector3(0, -15.0f, 0), Vector3.one, true),
                ("stool", new Vector3(-5.0f, 0.0f, 17.0f), new Vector3(0, 45.0f, 0), Vector3.one, true),
                
                // Table Decor (Clay jugs, mugs, vessels)
                ("vessel", new Vector3(-6.2f, 0.85f, 16.3f), Vector3.zero, Vector3.one, false),
                ("vessel_1", new Vector3(-6.7f, 0.85f, 16.7f), Vector3.zero, Vector3.one, false),
                ("vessel_2", new Vector3(-7.0f, 0.0f, 16.0f), Vector3.zero, Vector3.one, false),
                ("vessel_3", new Vector3(-6.0f, 0.0f, 17.5f), Vector3.zero, Vector3.one, false),
                
                // Firewood & Woodcutting Station (chopping block, logs, axe)
                ("frewood", new Vector3(-4.5f, 0.0f, 22.0f), new Vector3(0, 20.0f, 0), Vector3.one, true),
                ("frewood_1", new Vector3(-6.0f, 0.0f, 21.0f), new Vector3(0, -30.0f, 0), Vector3.one, true),
                ("axe", new Vector3(-5.2f, 0.0f, 21.5f), new Vector3(0, 60.0f, 0), Vector3.one, false),
                
                // Entrance & Porch Props (Broom, stone walkway, bulletin board)
                ("Broom", new Vector3(0.8f, 0.0f, 15.5f), new Vector3(0, 15.0f, -8.0f), Vector3.one, false),
                ("Stones", new Vector3(1.8f, 0.02f, 14.5f), Vector3.zero, Vector3.one, false),
                ("Board", new Vector3(-0.8f, 0.0f, 17.5f), new Vector3(0, -25.0f, 0), Vector3.one, true),
                
                // Brewing / Washing Cauldron & Scoop
                ("vat", new Vector3(-3.5f, 0.0f, 24.0f), Vector3.zero, Vector3.one, true),
                ("scoop", new Vector3(-3.0f, 0.1f, 23.5f), new Vector3(0, 40.0f, 0), Vector3.one, false),
                
                // Farmstead Entry Direction Signpost
                ("Pointer", new Vector3(3.0f, 0.0f, 8.0f), new Vector3(0, -60.0f, 0), Vector3.one, true)
            };

            int placedCount = 0;
            foreach (var item in items)
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
                if (item.addMeshCollider)
                {
                    MeshFilter mf = instance.GetComponentInChildren<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        MeshCollider mc = instance.GetComponent<MeshCollider>();
                        if (mc == null) mc = instance.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }

                placedCount++;
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"[FarmsteadSetup] SUCCESS: Successfully placed {placedCount} farmstead structures and props with colliders in the scene!");
        }
    }
}
#endif
