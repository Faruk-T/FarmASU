using System;
using System.IO;
using FarmASU.Inventory;
using FarmASU.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmASU.Core
{
    /// <summary>
    /// Central manager for saving and loading the game state.
    /// Handles deterministic JSON serialization via JsonUtility and atomic file IO.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [Header("System References")]
        [Tooltip("Active player controller reference.")]
        [SerializeField] private PlayerController _playerController;

        [Tooltip("Active player inventory controller reference.")]
        [SerializeField] private InventoryController _inventoryController;

        [Tooltip("Item database asset used to validate and resolve item definitions during load.")]
        [SerializeField] private ItemDatabase _itemDatabase;

        [Header("Save Configuration")]
        [Tooltip("Directory name under persistentDataPath where save files are stored.")]
        [SerializeField] private string _saveFolder = "Saves";

        [Tooltip("Base filename for save slots.")]
        [SerializeField] private string _saveFileName = "save_slot_{0}.json";

        [Tooltip("Allow F5 (QuickSave) and F9 (QuickLoad) keyboard shortcuts in debug/development.")]
        [SerializeField] private bool _enableDebugKeybinds = true;

        public event Action<SaveData> OnGameSaved;
        public event Action<SaveData> OnGameLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_enableDebugKeybinds && Keyboard.current != null)
            {
                if (Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    Debug.Log("[SaveManager] F5 pressed: Quick-Saving game...");
                    SaveGame(0);
                }
                else if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Debug.Log("[SaveManager] F9 pressed: Quick-Loading game...");
                    LoadGame(0);
                }
            }
        }

        private void ResolveReferences()
        {
            if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<PlayerController>();
            }

            if (_inventoryController == null && _playerController != null)
            {
                _inventoryController = _playerController.GetComponent<InventoryController>();
            }

            if (_inventoryController == null)
            {
                _inventoryController = FindAnyObjectByType<InventoryController>();
            }
        }

        /// <summary>
        /// Gets the absolute filepath for a given save slot index.
        /// </summary>
        public string GetSaveFilePath(int slotIndex = 0)
        {
            string folder = Path.Combine(Application.persistentDataPath, _saveFolder);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string filename = string.Format(_saveFileName, slotIndex);
            return Path.Combine(folder, filename);
        }

        /// <summary>
        /// Checks if a valid save file exists for the given slot.
        /// </summary>
        public bool HasSaveFile(int slotIndex = 0)
        {
            string path = GetSaveFilePath(slotIndex);
            return File.Exists(path);
        }

        /// <summary>
        /// Deletes the save file for the given slot if it exists.
        /// </summary>
        public void DeleteSaveFile(int slotIndex = 0)
        {
            string path = GetSaveFilePath(slotIndex);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Saves the active game state to disk using an atomic temporary write.
        /// </summary>
        public bool SaveGame(int slotIndex = 0)
        {
            ResolveReferences();

            try
            {
                SaveData saveData = new SaveData
                {
                    SaveVersion = SaveData.CurrentSaveVersion,
                    TimestampUtc = DateTime.UtcNow.Ticks
                };

                // 1. Capture Player State
                if (_playerController != null)
                {
                    saveData.Player.Position = _playerController.transform.position;
                    saveData.Player.RotationY = _playerController.transform.eulerAngles.y;
                    saveData.Player.Gold = 0; // Baseline currency
                }

                // 2. Capture Inventory State
                if (_inventoryController != null)
                {
                    int slotCount = _inventoryController.SlotCount;
                    saveData.Inventory.SlotCount = slotCount;
                    saveData.Inventory.Slots = new InventorySlotSaveData[slotCount];

                    for (int i = 0; i < slotCount; i++)
                    {
                        InventorySlot slot = _inventoryController.GetSlot(i);
                        saveData.Inventory.Slots[i] = new InventorySlotSaveData(slot.ItemId, slot.Quantity);
                    }
                }

                // 3. Capture Time State
                if (TimeManager.Instance != null)
                {
                    saveData.Time.Day = TimeManager.Instance.Day;
                    saveData.Time.Hour = TimeManager.Instance.Hour;
                    saveData.Time.Minute = TimeManager.Instance.Minute;
                }

                // 4. Atomic serialization write to disk
                string json = JsonUtility.ToJson(saveData, true);
                string targetPath = GetSaveFilePath(slotIndex);
                string tempPath = targetPath + ".tmp";

                File.WriteAllText(tempPath, json);

                if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }
                File.Move(tempPath, targetPath);

                Debug.Log($"[SaveManager] Game saved successfully to: {targetPath}");
                OnGameSaved?.Invoke(saveData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save game: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads the game state from disk and deterministically restores subsystems.
        /// </summary>
        public bool LoadGame(int slotIndex = 0)
        {
            ResolveReferences();
            string path = GetSaveFilePath(slotIndex);

            if (!File.Exists(path))
            {
                Debug.Log($"[SaveManager] No save file found at: {path}. Starting fresh game.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData == null)
                {
                    throw new Exception("Parsed SaveData is null.");
                }

                // Version validation
                if (saveData.SaveVersion > SaveData.CurrentSaveVersion)
                {
                    Debug.LogWarning($"[SaveManager] Unsupported save version {saveData.SaveVersion} (Current: {SaveData.CurrentSaveVersion}). Aborting load.");
                    return false;
                }

                // 1. Deterministic Time Restoration
                if (TimeManager.Instance != null && saveData.Time != null)
                {
                    TimeManager.Instance.SetState(new GameTimeState(saveData.Time.Day, saveData.Time.Hour, saveData.Time.Minute));
                }

                // 2. Deterministic Inventory Restoration
                if (_inventoryController != null && saveData.Inventory != null)
                {
                    int slotCount = saveData.Inventory.SlotCount > 0 ? saveData.Inventory.SlotCount : InventoryState.DefaultCapacity;
                    InventoryState restoredState = new InventoryState(slotCount);

                    if (saveData.Inventory.Slots != null)
                    {
                        for (int i = 0; i < saveData.Inventory.Slots.Length && i < slotCount; i++)
                        {
                            var savedSlot = saveData.Inventory.Slots[i];
                            if (savedSlot != null && !string.IsNullOrEmpty(savedSlot.ItemId))
                            {
                                // Validate item against database if assigned
                                if (_itemDatabase != null && _itemDatabase.GetItem(savedSlot.ItemId) == null)
                                {
                                    Debug.LogWarning($"[SaveManager] Unknown item '{savedSlot.ItemId}' in save slot {i}. Skipping.");
                                    continue;
                                }

                                restoredState.SetSlot(i, new InventorySlot(savedSlot.ItemId, savedSlot.Quantity));
                            }
                        }
                    }

                    _inventoryController.LoadState(restoredState);
                }

                // 3. Deterministic Player Position & Rotation Restoration
                if (_playerController != null && saveData.Player != null)
                {
                    CharacterController cc = _playerController.GetComponent<CharacterController>();
                    if (cc != null)
                    {
                        cc.enabled = false; // Must disable CharacterController to avoid physics override during teleport
                    }

                    _playerController.transform.position = saveData.Player.Position;
                    _playerController.transform.rotation = Quaternion.Euler(0.0f, saveData.Player.RotationY, 0.0f);

                    if (cc != null)
                    {
                        cc.enabled = true;
                    }
                }

                Debug.Log($"[SaveManager] Game loaded successfully from: {path}");
                OnGameLoaded?.Invoke(saveData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load save file (corrupt or invalid JSON): {ex.Message}");
                // Preserve corrupt file for debugging
                try
                {
                    string corruptBackupPath = path + ".corrupt";
                    if (File.Exists(corruptBackupPath)) File.Delete(corruptBackupPath);
                    File.Copy(path, corruptBackupPath);
                }
                catch { }

                return false;
            }
        }

        #region Testing Helpers

        public void SetReferencesForTesting(PlayerController player, InventoryController inventory, ItemDatabase database)
        {
            _playerController = player;
            _inventoryController = inventory;
            _itemDatabase = database;
        }

        #endregion
    }
}
