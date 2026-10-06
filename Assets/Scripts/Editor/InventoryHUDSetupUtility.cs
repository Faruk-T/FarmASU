#if UNITY_EDITOR
using System.IO;
using FarmASU.Core;
using FarmASU.Inventory;
using FarmASU.Player;
using FarmASU.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FarmASU.Editor
{
    public static class InventoryHUDSetupUtility
    {
        private const string IconsFolder = "Assets/Art/UI/Icons";
        private const string DefinitionsFolder = "Assets/ScriptableObjects/Definitions";

        [MenuItem("FarmASU/Setup Inventory & Hotbar HUD in MainScene")]
        public static void SetupInventoryHUDInMainScene()
        {
            Debug.Log("[InventoryHUD] Starting Inventory, Hotbar & Interaction HUD setup...");

            EnsureDirectories();

            // 1. Generate crisp 2D icons for items
            GenerateItemIcons();

            // 2. Assign icons to ItemDefinitions
            AssignIconsToItemDefinitions();

            // 3. Ensure Player has InventoryController and HeldItemHandler
            ConfigurePlayerComponents();

            // 4. Create or update Canvas_HUD with Hotbar and InteractionPrompt
            SetupCanvasHUD();

            // 5. Ensure physical world pickups are placed on the terrain near player
            SetupWorldPickups();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[InventoryHUD] === INVENTORY & HOTBAR HUD SETUP COMPLETE! ===");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(IconsFolder))
            {
                Directory.CreateDirectory(IconsFolder);
                AssetDatabase.Refresh();
            }
        }

        private static void GenerateItemIcons()
        {
            string woodIconPath = $"{IconsFolder}/Icon_Wood.png";
            string stoneIconPath = $"{IconsFolder}/Icon_Stone.png";

            if (!File.Exists(woodIconPath))
            {
                CreateWoodIcon(woodIconPath);
            }

            if (!File.Exists(stoneIconPath))
            {
                CreateStoneIcon(stoneIconPath);
            }

            AssetDatabase.Refresh();
            ConfigureSpriteImporter(woodIconPath);
            ConfigureSpriteImporter(stoneIconPath);
        }

        private static void ConfigureSpriteImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreateWoodIcon(string path)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            Color woodBark = new Color(0.42f, 0.28f, 0.16f);
            Color woodRings = new Color(0.78f, 0.62f, 0.40f);
            Color woodCore = new Color(0.88f, 0.72f, 0.48f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.5f) / (size * 0.42f);
                    float dy = (y - size * 0.5f) / (size * 0.32f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= 1.0f)
                    {
                        if (dist > 0.85f)
                        {
                            tex.SetPixel(x, y, woodBark);
                        }
                        else
                        {
                            float ring = Mathf.Sin(dist * 18.0f) * 0.5f + 0.5f;
                            Color c = Color.Lerp(woodRings, woodCore, ring);
                            tex.SetPixel(x, y, c);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateStoneIcon(string path)
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            Color stoneDark = new Color(0.35f, 0.38f, 0.42f);
            Color stoneMid = new Color(0.55f, 0.58f, 0.62f);
            Color stoneHighlight = new Color(0.75f, 0.78f, 0.82f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.5f) / (size * 0.40f);
                    float dy = (y - size * 0.46f) / (size * 0.38f);
                    // Add faceted irregularity
                    float angle = Mathf.Atan2(dy, dx);
                    float r = 1.0f + Mathf.Cos(angle * 5.0f) * 0.08f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / r;

                    if (dist <= 1.0f)
                    {
                        float h = (dx * -0.5f + dy * 0.6f);
                        Color c = Color.Lerp(stoneDark, stoneMid, dist);
                        if (h > 0.2f) c = Color.Lerp(c, stoneHighlight, (h - 0.2f) * 1.5f);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void AssignIconsToItemDefinitions()
        {
            Sprite woodSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconsFolder}/Icon_Wood.png");
            Sprite stoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{IconsFolder}/Icon_Stone.png");

            ItemDefinition woodDef = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DefinitionsFolder}/Wood.asset");
            if (woodDef != null && woodSprite != null)
            {
                SerializedObject so = new SerializedObject(woodDef);
                so.FindProperty("_icon").objectReferenceValue = woodSprite;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(woodDef);
            }

            ItemDefinition stoneDef = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DefinitionsFolder}/Stone.asset");
            if (stoneDef != null && stoneSprite != null)
            {
                SerializedObject so = new SerializedObject(stoneDef);
                so.FindProperty("_icon").objectReferenceValue = stoneSprite;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(stoneDef);
            }

            AssetDatabase.SaveAssets();
        }

        private static void ConfigurePlayerComponents()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("[InventoryHUD] Could not find 'Player' object in active scene.");
                return;
            }

            Undo.RecordObject(player, "Configure Player Components");

            // 1. InventoryController
            InventoryController inv = player.GetComponent<InventoryController>();
            if (inv == null)
            {
                inv = player.AddComponent<InventoryController>();
                Debug.Log("[InventoryHUD] Added InventoryController to Player.");
            }

            // 2. PlayerHeldItemHandler
            PlayerHeldItemHandler held = player.GetComponent<PlayerHeldItemHandler>();
            if (held == null)
            {
                held = player.AddComponent<PlayerHeldItemHandler>();
                Debug.Log("[InventoryHUD] Added PlayerHeldItemHandler to Player.");
            }

            // 3. Disable debug OnGUI box in PlayerInteractionDetector so sleek HUD is used
            PlayerInteractionDetector detector = player.GetComponent<PlayerInteractionDetector>();
            if (detector != null)
            {
                SerializedObject so = new SerializedObject(detector);
                SerializedProperty prop = so.FindProperty("_showDebugPrompt");
                if (prop != null)
                {
                    prop.boolValue = false;
                    so.ApplyModifiedProperties();
                }
            }
        }

        private static void SetupCanvasHUD()
        {
            GameObject canvasObj = GameObject.Find("Canvas_HUD");
            if (canvasObj != null)
            {
                Undo.DestroyObjectImmediate(canvasObj);
            }

            canvasObj = new GameObject("Canvas_HUD");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas_HUD");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Setup Hotbar
            SetupHotbarUI(canvasObj.transform);

            // Setup Interaction Prompt
            SetupInteractionPromptUI(canvasObj.transform);
        }

        private static void SetupHotbarUI(Transform canvasTransform)
        {
            GameObject hotbarObj = new GameObject("Hotbar_Panel");
            hotbarObj.transform.SetParent(canvasTransform, false);

            RectTransform rt = hotbarObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.0f);
            rt.anchorMax = new Vector2(0.5f, 0.0f);
            rt.pivot = new Vector2(0.5f, 0.0f);
            rt.anchoredPosition = new Vector2(0.0f, 28.0f);
            rt.sizeDelta = new Vector2(540.0f, 68.0f);

            // Background tray
            Image trayBg = hotbarObj.AddComponent<Image>();
            trayBg.color = new Color(0.08f, 0.08f, 0.10f, 0.65f);

            // Horizontal layout
            HorizontalLayoutGroup layout = hotbarObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            HotbarController controller = hotbarObj.AddComponent<HotbarController>();

            // 8 Hotbar slots
            HotbarSlotUI[] slots = new HotbarSlotUI[8];
            for (int i = 0; i < 8; i++)
            {
                slots[i] = CreateSlotUI(hotbarObj.transform, i);
            }

            // Assign references in HotbarController
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty slotsProp = so.FindProperty("_slotViews");
            slotsProp.arraySize = 8;
            for (int i = 0; i < 8; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>($"{DefinitionsFolder}/ItemDatabase.asset");
            if (db != null)
            {
                so.FindProperty("_itemDatabase").objectReferenceValue = db;
            }

            so.ApplyModifiedProperties();
        }

        private static HotbarSlotUI CreateSlotUI(Transform parent, int index)
        {
            GameObject slotObj = new GameObject($"Slot_{index + 1}");
            slotObj.transform.SetParent(parent, false);

            RectTransform slotRT = slotObj.AddComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(56.0f, 56.0f);

            Image slotBg = slotObj.AddComponent<Image>();
            slotBg.color = new Color(0.14f, 0.14f, 0.17f, 0.85f);

            // Selection Highlight Border
            GameObject borderObj = new GameObject("SelectionHighlight");
            borderObj.transform.SetParent(slotObj.transform, false);
            RectTransform borderRT = borderObj.AddComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.sizeDelta = new Vector2(4.0f, 4.0f);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(1.0f, 0.85f, 0.35f, 1.0f);
            borderObj.SetActive(index == 0);

            // Item Icon
            GameObject iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.12f, 0.12f);
            iconRT.anchorMax = new Vector2(0.88f, 0.88f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconObj.SetActive(false);

            // Hotkey Number (top-left)
            GameObject keyObj = new GameObject("KeyText");
            keyObj.transform.SetParent(slotObj.transform, false);
            RectTransform keyRT = keyObj.AddComponent<RectTransform>();
            keyRT.anchorMin = new Vector2(0.0f, 1.0f);
            keyRT.anchorMax = new Vector2(0.0f, 1.0f);
            keyRT.pivot = new Vector2(0.0f, 1.0f);
            keyRT.anchoredPosition = new Vector2(4.0f, -2.0f);
            keyRT.sizeDelta = new Vector2(20.0f, 16.0f);
            TextMeshProUGUI keyTmp = keyObj.AddComponent<TextMeshProUGUI>();
            keyTmp.text = (index + 1).ToString();
            keyTmp.fontSize = 12;
            keyTmp.color = new Color(1.0f, 1.0f, 1.0f, 0.45f);
            keyTmp.alignment = TextAlignmentOptions.TopLeft;

            // Quantity Count (bottom-right)
            GameObject qtyObj = new GameObject("QuantityText");
            qtyObj.transform.SetParent(slotObj.transform, false);
            RectTransform qtyRT = qtyObj.AddComponent<RectTransform>();
            qtyRT.anchorMin = new Vector2(1.0f, 0.0f);
            qtyRT.anchorMax = new Vector2(1.0f, 0.0f);
            qtyRT.pivot = new Vector2(1.0f, 0.0f);
            qtyRT.anchoredPosition = new Vector2(-4.0f, 2.0f);
            qtyRT.sizeDelta = new Vector2(40.0f, 18.0f);
            TextMeshProUGUI qtyTmp = qtyObj.AddComponent<TextMeshProUGUI>();
            qtyTmp.fontSize = 15;
            qtyTmp.fontStyle = FontStyles.Bold;
            qtyTmp.color = Color.white;
            qtyTmp.alignment = TextAlignmentOptions.BottomRight;
            qtyObj.SetActive(false);

            // Hook up HotbarSlotUI
            HotbarSlotUI slotUI = slotObj.AddComponent<HotbarSlotUI>();
            SerializedObject so = new SerializedObject(slotUI);
            so.FindProperty("_slotBackground").objectReferenceValue = slotBg;
            so.FindProperty("_itemIcon").objectReferenceValue = iconImg;
            so.FindProperty("_quantityText").objectReferenceValue = qtyTmp;
            so.FindProperty("_keyNumberText").objectReferenceValue = keyTmp;
            so.FindProperty("_selectionHighlight").objectReferenceValue = borderImg;
            so.ApplyModifiedProperties();

            return slotUI;
        }

        private static void SetupInteractionPromptUI(Transform canvasTransform)
        {
            GameObject promptObj = new GameObject("InteractionPrompt_Panel");
            promptObj.transform.SetParent(canvasTransform, false);

            RectTransform rt = promptObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.0f);
            rt.anchorMax = new Vector2(0.5f, 0.0f);
            rt.pivot = new Vector2(0.5f, 0.0f);
            rt.anchoredPosition = new Vector2(0.0f, 120.0f);
            rt.sizeDelta = new Vector2(320.0f, 44.0f);

            CanvasGroup cg = promptObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Pill background
            Image pillBg = promptObj.AddComponent<Image>();
            pillBg.color = new Color(0.10f, 0.10f, 0.12f, 0.88f);

            // Key Badge [E]
            GameObject keyBadge = new GameObject("KeyBadge");
            keyBadge.transform.SetParent(promptObj.transform, false);
            RectTransform badgeRT = keyBadge.AddComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0.0f, 0.5f);
            badgeRT.anchorMax = new Vector2(0.0f, 0.5f);
            badgeRT.pivot = new Vector2(0.0f, 0.5f);
            badgeRT.anchoredPosition = new Vector2(10.0f, 0.0f);
            badgeRT.sizeDelta = new Vector2(28.0f, 28.0f);

            Image badgeImg = keyBadge.AddComponent<Image>();
            badgeImg.color = new Color(0.95f, 0.72f, 0.22f, 1.0f);

            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyBadge.transform, false);
            RectTransform ktRT = keyTextObj.AddComponent<RectTransform>();
            ktRT.anchorMin = Vector2.zero;
            ktRT.anchorMax = Vector2.one;
            ktRT.offsetMin = Vector2.zero;
            ktRT.offsetMax = Vector2.zero;
            TextMeshProUGUI keyTmp = keyTextObj.AddComponent<TextMeshProUGUI>();
            keyTmp.text = "E";
            keyTmp.fontSize = 16;
            keyTmp.fontStyle = FontStyles.Bold;
            keyTmp.color = new Color(0.15f, 0.12f, 0.05f, 1.0f);
            keyTmp.alignment = TextAlignmentOptions.Center;

            // Action Text
            GameObject actionTextObj = new GameObject("ActionText");
            actionTextObj.transform.SetParent(promptObj.transform, false);
            RectTransform atRT = actionTextObj.AddComponent<RectTransform>();
            atRT.anchorMin = new Vector2(0.0f, 0.0f);
            atRT.anchorMax = new Vector2(1.0f, 1.0f);
            atRT.offsetMin = new Vector2(46.0f, 0.0f);
            atRT.offsetMax = new Vector2(-12.0f, 0.0f);

            TextMeshProUGUI actionTmp = actionTextObj.AddComponent<TextMeshProUGUI>();
            actionTmp.text = "Topla";
            actionTmp.fontSize = 17;
            actionTmp.fontStyle = FontStyles.Bold;
            actionTmp.color = Color.white;
            actionTmp.alignment = TextAlignmentOptions.MidlineLeft;

            InteractionPromptUI promptUI = promptObj.AddComponent<InteractionPromptUI>();
            SerializedObject so = new SerializedObject(promptUI);
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_actionText").objectReferenceValue = actionTmp;
            so.FindProperty("_keyBadgeText").objectReferenceValue = keyTmp;
            so.FindProperty("_promptBackground").objectReferenceValue = pillBg;
            so.ApplyModifiedProperties();
        }

        private static void SetupWorldPickups()
        {
            Terrain terrain = Object.FindAnyObjectByType<Terrain>();
            GameObject player = GameObject.Find("Player");
            Vector3 spawnBase = player != null ? player.transform.position : new Vector3(0, 0, 10);

            ItemDefinition woodDef = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DefinitionsFolder}/Wood.asset");
            ItemDefinition stoneDef = AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{DefinitionsFolder}/Stone.asset");

            // 1. Position or update Pickup_Wood
            GameObject woodPickup = GameObject.Find("Pickup_Wood");
            if (woodPickup != null)
            {
                Undo.RecordObject(woodPickup.transform, "Position Pickup_Wood");
                Vector3 woodPos = spawnBase + new Vector3(1.5f, 0, 2.0f);
                if (terrain != null)
                {
                    woodPos.y = terrain.SampleHeight(woodPos) + terrain.transform.position.y + 0.25f;
                }
                woodPickup.transform.position = woodPos;
            }

            // 2. Add a Pickup_Stone
            GameObject stonePickup = GameObject.Find("Pickup_Stone");
            if (stonePickup == null && stoneDef != null)
            {
                stonePickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stonePickup.name = "Pickup_Stone";
                Undo.RegisterCreatedObjectUndo(stonePickup, "Create Pickup_Stone");
                stonePickup.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);

                Vector3 stonePos = spawnBase + new Vector3(-1.8f, 0, 2.2f);
                if (terrain != null)
                {
                    stonePos.y = terrain.SampleHeight(stonePos) + terrain.transform.position.y + 0.22f;
                }
                stonePickup.transform.position = stonePos;

                Collider sc = stonePickup.GetComponent<Collider>();
                if (sc != null) sc.isTrigger = true;

                WorldPickup wp = stonePickup.AddComponent<WorldPickup>();
                wp.Initialize(stoneDef, 5);

                // Grey stone material
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                Material stoneMat = new Material(lit);
                stoneMat.SetColor("_BaseColor", new Color(0.55f, 0.58f, 0.62f));
                stonePickup.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;
            }
        }
    }
}
#endif
