using System;
using UnityEngine;

namespace FarmASU.Inventory
{
    /// <summary>
    /// Pure C# runtime state model for a fixed-slot inventory.
    /// Manages slot array, deterministic two-pass stacking, item consumption, and capacity checks.
    /// Completely detached from MonoBehaviours and scenes for clean Save/Load serialization.
    /// </summary>
    [Serializable]
    public class InventoryState
    {
        public const int DefaultCapacity = 20;

        [SerializeField] private InventorySlot[] _slots;

        public InventoryState(int capacity = DefaultCapacity)
        {
            int size = Mathf.Max(1, capacity);
            _slots = new InventorySlot[size];
            for (int i = 0; i < size; i++)
            {
                _slots[i] = new InventorySlot(string.Empty, 0);
            }
        }

        public int SlotCount => _slots != null ? _slots.Length : 0;

        /// <summary>
        /// Retrieves a copy of the slot at the specified index.
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (_slots == null || index < 0 || index >= _slots.Length)
            {
                return new InventorySlot(string.Empty, 0);
            }

            return _slots[index];
        }

        /// <summary>
        /// Directly assigns a slot value at index.
        /// </summary>
        public void SetSlot(int index, InventorySlot slot)
        {
            if (_slots == null || index < 0 || index >= _slots.Length)
            {
                return;
            }

            _slots[index] = slot;
        }

        /// <summary>
        /// Attempts to add quantity of an item definition into the inventory.
        /// Follows a deterministic two-pass approach:
        /// 1. Merge into existing incomplete stacks.
        /// 2. Fill empty slots.
        /// Returns the remaining unadded quantity (0 if entirely added).
        /// </summary>
        public int TryAddItem(ItemDefinition item, int quantity)
        {
            if (item == null || string.IsNullOrEmpty(item.Id) || quantity <= 0)
            {
                return quantity;
            }

            int remaining = quantity;
            int maxStack = item.MaxStack;
            string itemId = item.Id;

            // Pass 1: Add to existing matching stacks that are not full
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].ItemId == itemId && _slots[i].Quantity < maxStack)
                {
                    int space = maxStack - _slots[i].Quantity;
                    int toAdd = Mathf.Min(space, remaining);
                    _slots[i].Quantity += toAdd;
                    remaining -= toAdd;

                    if (remaining <= 0)
                    {
                        return 0;
                    }
                }
            }

            // Pass 2: Fill available empty slots
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    int toAdd = Mathf.Min(maxStack, remaining);
                    _slots[i] = new InventorySlot(itemId, toAdd);
                    remaining -= toAdd;

                    if (remaining <= 0)
                    {
                        return 0;
                    }
                }
            }

            return remaining;
        }

        /// <summary>
        /// Removes the specified quantity of an item across the inventory.
        /// Returns true if the full quantity was successfully removed, false if insufficient items were available.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                return false;
            }

            if (CountItem(itemId) < quantity)
            {
                return false;
            }

            int remainingToRemove = quantity;

            for (int i = _slots.Length - 1; i >= 0; i--)
            {
                if (_slots[i].ItemId == itemId)
                {
                    if (_slots[i].Quantity <= remainingToRemove)
                    {
                        remainingToRemove -= _slots[i].Quantity;
                        _slots[i].Clear();
                    }
                    else
                    {
                        _slots[i].Quantity -= remainingToRemove;
                        remainingToRemove = 0;
                        break;
                    }

                    if (remainingToRemove <= 0)
                    {
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Counts total quantity of an item across all slots.
        /// </summary>
        public int CountItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _slots == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].ItemId == itemId)
                {
                    total += _slots[i].Quantity;
                }
            }

            return total;
        }

        /// <summary>
        /// Checks whether the inventory has at least the required quantity of an item.
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return CountItem(itemId) >= quantity;
        }

        /// <summary>
        /// Clears all slots in the inventory.
        /// </summary>
        public void Clear()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Clear();
            }
        }

        /// <summary>
        /// Deep-clones the inventory state into a new isolated instance.
        /// </summary>
        public InventoryState Clone()
        {
            InventoryState clone = new InventoryState(_slots.Length);
            for (int i = 0; i < _slots.Length; i++)
            {
                clone.SetSlot(i, new InventorySlot(_slots[i].ItemId, _slots[i].Quantity));
            }
            return clone;
        }
    }
}
