using System;
using UnityEngine;

namespace FarmASU.Core
{
    /// <summary>
    /// Root data-transfer object (DTO) for saving and loading the game state in FarmASU.
    /// Pure C# serializable class compatible with Unity JsonUtility.
    /// Completely isolated from scene hierarchies, MonoBehaviours, and visual transforms.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentSaveVersion = 1;

        [Tooltip("Schema version number for backwards compatibility and migrations.")]
        public int SaveVersion = CurrentSaveVersion;

        [Tooltip("UTC timestamp ticks when the save was recorded.")]
        public long TimestampUtc;

        public PlayerSaveData Player = new PlayerSaveData();
        public InventorySaveData Inventory = new InventorySaveData();
        public TimeSaveData Time = new TimeSaveData();
    }

    [Serializable]
    public class PlayerSaveData
    {
        public Vector3 Position;
        public float RotationY;
        public int Gold;

        public PlayerSaveData()
        {
            Position = Vector3.zero;
            RotationY = 0.0f;
            Gold = 0;
        }

        public PlayerSaveData(Vector3 position, float rotationY, int gold = 0)
        {
            Position = position;
            RotationY = rotationY;
            Gold = gold;
        }
    }

    [Serializable]
    public class InventorySaveData
    {
        public int SlotCount;
        public InventorySlotSaveData[] Slots = Array.Empty<InventorySlotSaveData>();

        public InventorySaveData()
        {
            SlotCount = 0;
            Slots = Array.Empty<InventorySlotSaveData>();
        }

        public InventorySaveData(int slotCount, InventorySlotSaveData[] slots)
        {
            SlotCount = slotCount;
            Slots = slots ?? Array.Empty<InventorySlotSaveData>();
        }
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public string ItemId;
        public int Quantity;

        public InventorySlotSaveData()
        {
            ItemId = string.Empty;
            Quantity = 0;
        }

        public InventorySlotSaveData(string itemId, int quantity)
        {
            ItemId = itemId ?? string.Empty;
            Quantity = quantity;
        }
    }

    [Serializable]
    public class TimeSaveData
    {
        public int Day;
        public int Hour;
        public int Minute;

        public TimeSaveData()
        {
            Day = 1;
            Hour = 6;
            Minute = 0;
        }

        public TimeSaveData(int day, int hour, int minute)
        {
            Day = day;
            Hour = hour;
            Minute = minute;
        }
    }
}
