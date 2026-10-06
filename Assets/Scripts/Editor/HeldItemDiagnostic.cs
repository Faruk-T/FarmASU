#if UNITY_EDITOR
using System.IO;
using FarmASU.Inventory;
using UnityEditor;
using UnityEngine;

namespace FarmASU.Editor
{
    public static class HeldItemDiagnostic
    {
        [MenuItem("FarmASU/Diagnose Meshes And Build Perfect Held Prefabs")]
        public static void DiagnoseAndBuild()
        {
            Debug.Log("=== HELD ITEM DIAGNOSTIC START ===");

            // 1. Inspect frewood.fbx
            InspectMesh("Assets/Bizulka/Witchs_house/Models/frewood.fbx");
            // 2. Inspect frewood_1.fbx
            InspectMesh("Assets/Bizulka/Witchs_house/Models/frewood_1.fbx");
            // 3. Inspect Stones.fbx
            InspectMesh("Assets/Bizulka/Witchs_house/Models/Stones.fbx");

            BuildCorrectPrefabs();

            Debug.Log("=== HELD ITEM DIAGNOSTIC COMPLETE ===");
        }

        private static void InspectMesh(string fbxPath)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError($"Could not load FBX at {fbxPath}");
                return;
            }

            MeshFilter mf = fbx.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogError($"No MeshFilter found in {fbxPath}");
                return;
            }

            Mesh m = mf.sharedMesh;
            Debug.Log($"[Mesh Diagnostic] '{fbx.name}': bounds center = {m.bounds.center}, extents = {m.bounds.extents}, size = {m.bounds.size}");
        }

        private static void BuildCorrectPrefabs()
        {
            string heldDir = "Assets/Prefabs/HeldItems";
            if (!Directory.Exists(heldDir)) Directory.CreateDirectory(heldDir);

            // Load materials
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bizulka/Witchs_house/Materials/Whitchs_objects.mat");
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Bizulka/Witchs_house/Materials/Whitchs_house.mat");

            if (woodMat == null) Debug.LogError("Whitchs_objects.mat not found!");
            if (stoneMat == null) Debug.LogError("Whitchs_house.mat not found!");

            // --- BUILD HELD WOOD ---
            // Let's use frewood_1 (clean single log) or frewood
            GameObject woodFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bizulka/Witchs_house/Models/frewood_1.fbx");
            if (woodFBX == null) woodFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bizulka/Witchs_house/Models/frewood.fbx");
            Mesh woodMesh = woodFBX.GetComponentInChildren<MeshFilter>().sharedMesh;

            GameObject woodRoot = new GameObject("Held_Wood");
            GameObject woodVisual = new GameObject("Visual");
            woodVisual.transform.SetParent(woodRoot.transform, false);

            MeshFilter woodMf = woodVisual.AddComponent<MeshFilter>();
            woodMf.sharedMesh = woodMesh;
            MeshRenderer woodMr = woodVisual.AddComponent<MeshRenderer>();
            woodMr.sharedMaterial = woodMat;

            // Offset visual so the center of the wood mesh is exactly at the root grip point (0,0,0)
            woodVisual.transform.localPosition = -woodMesh.bounds.center;
            // Scale and rotate so the log is gripped naturally in the right hand
            woodRoot.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            woodVisual.transform.localRotation = Quaternion.Euler(0, 0, 90f);

            string woodPrefabPath = $"{heldDir}/Held_Wood.prefab";
            PrefabUtility.SaveAsPrefabAsset(woodRoot, woodPrefabPath);
            Object.DestroyImmediate(woodRoot);
            Debug.Log($"Saved correct Held_Wood prefab at {woodPrefabPath}");

            // --- BUILD HELD STONE ---
            GameObject stoneFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bizulka/Witchs_house/Models/Stones.fbx");
            Mesh stoneMesh = stoneFBX.GetComponentInChildren<MeshFilter>().sharedMesh;

            GameObject stoneRoot = new GameObject("Held_Stone");
            GameObject stoneVisual = new GameObject("Visual");
            stoneVisual.transform.SetParent(stoneRoot.transform, false);

            MeshFilter stoneMf = stoneVisual.AddComponent<MeshFilter>();
            stoneMf.sharedMesh = stoneMesh;
            MeshRenderer stoneMr = stoneVisual.AddComponent<MeshRenderer>();
            stoneMr.sharedMaterial = stoneMat;

            // Offset visual so the center of the stone cluster/rock is at the root grip point (0,0,0)
            stoneVisual.transform.localPosition = -stoneMesh.bounds.center;
            // Scale stone nicely for a handheld rock
            stoneRoot.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            string stonePrefabPath = $"{heldDir}/Held_Stone.prefab";
            PrefabUtility.SaveAsPrefabAsset(stoneRoot, stonePrefabPath);
            Object.DestroyImmediate(stoneRoot);
            Debug.Log($"Saved correct Held_Stone prefab at {stonePrefabPath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
