#if UNITY_EDITOR
using System;
using System.IO;
using FarmASU.Core;
using FarmASU.Inventory;
using FarmASU.Player;
using UnityEditor;
using UnityEngine;

namespace FarmASU.Editor
{
    /// <summary>
    /// Comprehensive test suite validating Phase 3 specifications:
    /// Physical Interaction, Item Pickups, Inventory Stacking, Save/Load Serialization, and Resilience.
    /// Can be executed via Unity Editor menu: FarmASU -> Run Phase 3 Tests
    /// </summary>
    public static class Phase3Tests
    {
        [MenuItem("FarmASU/Run Phase 3 Tests")]
        public static void RunAllTests()
        {
            Debug.Log("==================================================");
            Debug.Log("[Phase3Tests] Starting Phase 3 Test Suite...");
            int passed = 0;
            int failed = 0;

            RunTest("Test 1: Interaction Detection in Forward Cone", TestInteractionDetection, ref passed, ref failed);
            RunTest("Test 2: Interaction Ignores Objects Behind Player", TestInteractionAngleFiltering, ref passed, ref failed);
            RunTest("Test 3: Full Pickup Collection & Destruction", TestPickupCollection, ref passed, ref failed);
            RunTest("Test 4: Inventory Two-Pass Stacking", TestInventoryTwoPassStacking, ref passed, ref failed);
            RunTest("Test 5: Full Inventory Rejection (No Item Loss)", TestInventoryFullRejection, ref passed, ref failed);
            RunTest("Test 6: Partial Pickup (Split Stack)", TestPartialPickup, ref passed, ref failed);
            RunTest("Test 7: ItemDatabase ID Resolution", TestItemDatabaseResolution, ref passed, ref failed);
            RunTest("Test 8: Save File JSON Generation", TestSaveFileGeneration, ref passed, ref failed);
            RunTest("Test 9: Load Restoration (Time, Pos, Slots)", TestLoadRestoration, ref passed, ref failed);
            RunTest("Test 10: Persistence Roundtrip (State A -> B -> A)", TestPersistenceRoundtrip, ref passed, ref failed);
            RunTest("Test 11: Corrupted Save Resilience", TestCorruptedSaveResilience, ref passed, ref failed);

            Debug.Log("--------------------------------------------------");
            if (failed == 0)
            {
                Debug.Log($"[Phase3Tests] ALL {passed} TESTS PASSED SUCCESSFULLY! (0 Failures)");
            }
            else
            {
                Debug.LogError($"[Phase3Tests] TEST RUN FINISHED: {passed} Passed, {failed} FAILED.");
            }
            Debug.Log("==================================================");
        }

        private static void RunTest(string testName, Action testAction, ref int passed, ref int failed)
        {
            try
            {
                testAction.Invoke();
                Debug.Log($"[PASS] {testName}");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FAIL] {testName}: {ex.Message}");
                failed++;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        #region Mock Rig Helpers

        private static ItemDefinition CreateMockItem(string id, string displayName, int maxStack)
        {
            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Initialize(id, displayName, maxStack);
            return item;
        }

        private static GameObject CreateMockPlayer(out PlayerController player, out InventoryController inventory, out PlayerInteractionDetector detector)
        {
            GameObject go = new GameObject("Test_Player");
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            // CharacterController requires a collider
            go.AddComponent<CharacterController>();
            player = go.AddComponent<PlayerController>();
            inventory = go.AddComponent<InventoryController>();
            detector = go.AddComponent<PlayerInteractionDetector>();

            return go;
        }

        private static GameObject CreateMockPickup(ItemDefinition item, int quantity, Vector3 position)
        {
            GameObject go = new GameObject($"Pickup_{item.Id}");
            go.transform.position = position;
            SphereCollider col = go.AddComponent<SphereCollider>();
            col.radius = 0.5f;

            WorldPickup pickup = go.AddComponent<WorldPickup>();
            pickup.Initialize(item, quantity);
            return go;
        }

        #endregion

        #region Tests

        private static void TestInteractionDetection()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            ItemDefinition stone = CreateMockItem("stone", "Stone", 99);
            // Place pickup 1.2 meters directly in front of player
            GameObject pickupGo = CreateMockPickup(stone, 5, new Vector3(0, 0, 1.2f));

            try
            {
                detector.ScanForInteractables();
                Assert(detector.CurrentTarget != null, "Expected detector to find pickup in front of player");
                Assert(detector.CurrentTarget is WorldPickup wp && wp.ItemDefinition.Id == "stone", "Target is not the expected stone pickup");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerGo);
                UnityEngine.Object.DestroyImmediate(pickupGo);
                UnityEngine.Object.DestroyImmediate(stone);
            }
        }

        private static void TestInteractionAngleFiltering()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            ItemDefinition stone = CreateMockItem("stone", "Stone", 99);
            // Place pickup 1.2 meters BEHIND player (angle = 180 degrees)
            GameObject pickupGo = CreateMockPickup(stone, 5, new Vector3(0, 0, -1.2f));

            try
            {
                detector.ScanForInteractables();
                Assert(detector.CurrentTarget == null, "Expected detector to ignore pickup behind the player");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerGo);
                UnityEngine.Object.DestroyImmediate(pickupGo);
                UnityEngine.Object.DestroyImmediate(stone);
            }
        }

        private static void TestPickupCollection()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            ItemDefinition wood = CreateMockItem("wood", "Wood", 99);
            GameObject pickupGo = CreateMockPickup(wood, 15, new Vector3(0, 0, 1.0f));

            try
            {
                detector.ScanForInteractables();
                Assert(detector.CurrentTarget != null, "Target should not be null before interaction");

                bool success = detector.TriggerInteract();
                Assert(success, "TriggerInteract should succeed");
                Assert(inventory.CountItem("wood") == 15, $"Expected 15 wood in inventory, got {inventory.CountItem("wood")}");
                // Pickup GameObject should be destroyed
                Assert(pickupGo == null || !pickupGo.activeInHierarchy || pickupGo.GetComponent<WorldPickup>() == null, "Pickup GameObject should be destroyed after full collection");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerGo);
                if (pickupGo != null) UnityEngine.Object.DestroyImmediate(pickupGo);
                UnityEngine.Object.DestroyImmediate(wood);
            }
        }

        private static void TestInventoryTwoPassStacking()
        {
            InventoryState state = new InventoryState(5);
            ItemDefinition wood = CreateMockItem("wood", "Wood", 10); // max stack 10

            try
            {
                // Slot 0 has 7 wood
                state.SetSlot(0, new InventorySlot("wood", 7));

                // Add 8 wood: 3 should merge into slot 0 (reaching 10), remaining 5 should go to slot 1
                int unadded = state.TryAddItem(wood, 8);

                Assert(unadded == 0, $"Expected 0 unadded, got {unadded}");
                Assert(state.GetSlot(0).Quantity == 10, $"Expected slot 0 to have 10, got {state.GetSlot(0).Quantity}");
                Assert(state.GetSlot(1).Quantity == 5, $"Expected slot 1 to have 5, got {state.GetSlot(1).Quantity}");
                Assert(state.CountItem("wood") == 15, $"Expected total 15 wood, got {state.CountItem("wood")}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wood);
            }
        }

        private static void TestInventoryFullRejection()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            ItemDefinition stone = CreateMockItem("stone", "Stone", 10);
            GameObject pickupGo = CreateMockPickup(stone, 5, new Vector3(0, 0, 1.0f));

            try
            {
                // Fill all inventory slots completely
                for (int i = 0; i < inventory.SlotCount; i++)
                {
                    inventory.SetSlot(i, new InventorySlot("stone", 10));
                }

                detector.ScanForInteractables();
                detector.TriggerInteract();

                // Pickup must NOT be destroyed, quantity must remain 5
                Assert(pickupGo != null, "Pickup should remain alive when inventory is full");
                WorldPickup wp = pickupGo.GetComponent<WorldPickup>();
                Assert(wp != null && wp.Quantity == 5, $"Expected pickup to keep quantity 5, got {wp?.Quantity}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerGo);
                if (pickupGo != null) UnityEngine.Object.DestroyImmediate(pickupGo);
                UnityEngine.Object.DestroyImmediate(stone);
            }
        }

        private static void TestPartialPickup()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            ItemDefinition iron = CreateMockItem("iron", "Iron", 10);
            GameObject pickupGo = CreateMockPickup(iron, 8, new Vector3(0, 0, 1.0f));

            try
            {
                // Fill all slots except 1 slot with 7 iron (can only take 3 more)
                for (int i = 0; i < inventory.SlotCount; i++)
                {
                    inventory.SetSlot(i, new InventorySlot("unrelated_item", 10));
                }
                // Slot 0 has 7 iron
                inventory.SetSlot(0, new InventorySlot("iron", 7));

                detector.ScanForInteractables();
                detector.TriggerInteract();

                // Inventory slot 0 should now be full (10)
                Assert(inventory.GetSlot(0).Quantity == 10, $"Expected slot 0 to have 10 iron, got {inventory.GetSlot(0).Quantity}");

                // World pickup should NOT be destroyed, its count should be 8 - 3 = 5
                Assert(pickupGo != null, "World pickup must survive partial collection");
                WorldPickup wp = pickupGo.GetComponent<WorldPickup>();
                Assert(wp != null && wp.Quantity == 5, $"Expected world pickup to retain 5 items, got {wp?.Quantity}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerGo);
                if (pickupGo != null) UnityEngine.Object.DestroyImmediate(pickupGo);
                UnityEngine.Object.DestroyImmediate(iron);
            }
        }

        private static void TestItemDatabaseResolution()
        {
            ItemDefinition wood = CreateMockItem("wood", "Wood", 99);
            ItemDefinition stone = CreateMockItem("stone", "Stone", 99);

            ItemDatabase db = ScriptableObject.CreateInstance<ItemDatabase>();
            db.SetItemsForTesting(new[] { wood, stone });

            try
            {
                Assert(db.GetItem("wood") == wood, "Database failed to resolve 'wood'");
                Assert(db.GetItem("stone") == stone, "Database failed to resolve 'stone'");
                Assert(db.GetItem("unknown_item") == null, "Database should return null for unknown item");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(db);
                UnityEngine.Object.DestroyImmediate(wood);
                UnityEngine.Object.DestroyImmediate(stone);
            }
        }

        private static void TestSaveFileGeneration()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            playerGo.transform.position = new Vector3(12.5f, 0.0f, -4.2f);
            inventory.SetSlot(0, new InventorySlot("wood", 42));

            GameObject saveManagerGo = new GameObject("Test_SaveManager");
            SaveManager sm = saveManagerGo.AddComponent<SaveManager>();
            sm.SetReferencesForTesting(player, inventory, null);

            int testSlot = 99; // Isolated test slot

            try
            {
                sm.DeleteSaveFile(testSlot);
                bool saved = sm.SaveGame(testSlot);

                Assert(saved, "SaveGame returned false");
                Assert(sm.HasSaveFile(testSlot), "Save file was not written to disk");

                string path = sm.GetSaveFilePath(testSlot);
                string json = File.ReadAllText(path);
                Assert(json.Contains("\"SaveVersion\": 1"), "JSON does not contain SaveVersion 1");
                Assert(json.Contains("\"wood\""), "JSON does not contain saved item 'wood'");
                Assert(json.Contains("12.5"), "JSON does not contain saved player position X");
            }
            finally
            {
                sm.DeleteSaveFile(testSlot);
                UnityEngine.Object.DestroyImmediate(playerGo);
                UnityEngine.Object.DestroyImmediate(saveManagerGo);
            }
        }

        private static void TestLoadRestoration()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            GameObject timeGo = new GameObject("Test_TimeManager");
            TimeManager tm = timeGo.AddComponent<TimeManager>();
            tm.InitializeState();

            GameObject saveManagerGo = new GameObject("Test_SaveManager");
            SaveManager sm = saveManagerGo.AddComponent<SaveManager>();
            sm.SetReferencesForTesting(player, inventory, null);

            int testSlot = 98;

            try
            {
                // Setup known State A
                player.transform.position = new Vector3(5f, 0f, 15f);
                inventory.SetSlot(2, new InventorySlot("stone", 33));
                tm.SetState(new GameTimeState(3, 14, 25));

                sm.SaveGame(testSlot);

                // Mutate to State B
                player.transform.position = Vector3.zero;
                inventory.Clear();
                tm.SetState(new GameTimeState(1, 6, 0));

                // Load State A
                bool loaded = sm.LoadGame(testSlot);
                Assert(loaded, "LoadGame returned false");

                Assert(Vector3.Distance(player.transform.position, new Vector3(5f, 0f, 15f)) < 0.01f, $"Player position not restored: {player.transform.position}");
                Assert(inventory.GetSlot(2).ItemId == "stone" && inventory.GetSlot(2).Quantity == 33, "Inventory slot 2 was not restored");
                Assert(tm.Day == 3 && tm.Hour == 14 && tm.Minute == 25, $"Game time not restored: Day {tm.Day} {tm.Hour}:{tm.Minute}");
            }
            finally
            {
                sm.DeleteSaveFile(testSlot);
                UnityEngine.Object.DestroyImmediate(playerGo);
                UnityEngine.Object.DestroyImmediate(timeGo);
                UnityEngine.Object.DestroyImmediate(saveManagerGo);
            }
        }

        private static void TestPersistenceRoundtrip()
        {
            // Explicitly tests State A -> State B -> Reload State A
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            GameObject saveManagerGo = new GameObject("Test_SaveManager");
            SaveManager sm = saveManagerGo.AddComponent<SaveManager>();
            sm.SetReferencesForTesting(player, inventory, null);

            int testSlot = 97;

            try
            {
                // State A: 20 wood in slot 0, pos = (10, 0, 10)
                player.transform.position = new Vector3(10f, 0f, 10f);
                inventory.SetSlot(0, new InventorySlot("wood", 20));
                sm.SaveGame(testSlot);

                // Modify to State B: 99 stone in slot 0, pos = (-5, 0, -5)
                player.transform.position = new Vector3(-5f, 0f, -5f);
                inventory.SetSlot(0, new InventorySlot("stone", 99));

                Assert(inventory.GetSlot(0).ItemId == "stone", "Sanity check failed for State B");

                // Reload State A
                sm.LoadGame(testSlot);

                Assert(inventory.GetSlot(0).ItemId == "wood" && inventory.GetSlot(0).Quantity == 20, "Persistence failed to restore State A inventory");
                Assert(Vector3.Distance(player.transform.position, new Vector3(10f, 0f, 10f)) < 0.01f, "Persistence failed to restore State A position");
            }
            finally
            {
                sm.DeleteSaveFile(testSlot);
                UnityEngine.Object.DestroyImmediate(playerGo);
                UnityEngine.Object.DestroyImmediate(saveManagerGo);
            }
        }

        private static void TestCorruptedSaveResilience()
        {
            GameObject playerGo = CreateMockPlayer(out var player, out var inventory, out var detector);
            GameObject saveManagerGo = new GameObject("Test_SaveManager");
            SaveManager sm = saveManagerGo.AddComponent<SaveManager>();
            sm.SetReferencesForTesting(player, inventory, null);

            int testSlot = 96;
            string path = sm.GetSaveFilePath(testSlot);

            try
            {
                // Write invalid malformed JSON
                File.WriteAllText(path, "{ corrupted_incomplete_json: [}}");

                // Attempt to load
                bool loaded = sm.LoadGame(testSlot);

                Assert(!loaded, "LoadGame should return false for malformed JSON");
                Assert(File.Exists(path), "Corrupted save file should not be deleted");
            }
            finally
            {
                sm.DeleteSaveFile(testSlot);
                string corruptPath = path + ".corrupt";
                if (File.Exists(corruptPath)) File.Delete(corruptPath);
                UnityEngine.Object.DestroyImmediate(playerGo);
                UnityEngine.Object.DestroyImmediate(saveManagerGo);
            }
        }

        #endregion
    }
}
#endif
