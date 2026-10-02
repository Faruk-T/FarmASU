using System;
using UnityEngine;

namespace FarmASU.Inventory
{
    /// <summary>
    /// Player-facing inventory component.
    /// Bridges the pure C# InventoryState model to Unity lifecycle, inspector, and events.
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Total number of inventory slots.")]
        [SerializeField] private int _capacity = InventoryState.DefaultCapacity;

        [Header("Runtime State (Debug Only)")]
        [SerializeField] private InventoryState _state;

        /// <summary>
        /// Fired whenever a specific slot changes. Parameters: (slotIndex, newSlot)
        /// </summary>
        public event Action<int, InventorySlot> OnSlotChanged;

        /// <summary>
        /// Fired whenever any change occurs in the inventory.
        /// </summary>
        public event Action OnInventoryUpdated;

        public int SlotCount => _state != null ? _state.SlotCount : _capacity;

        private void Awake()
        {
            if (_state == null || _state.SlotCount != _capacity)
            {
                _state = new InventoryState(_capacity);
            }
        }

        /// <summary>
        /// Attempts to add an item into the inventory.
        /// Returns remaining unadded count (0 if fully added).
        /// </summary>
        public int TryAddItem(ItemDefinition item, int quantity)
        {
            if (_state == null)
            {
                _state = new InventoryState(_capacity);
            }

            int unadded = _state.TryAddItem(item, quantity);
            if (unadded < quantity)
            {
                OnInventoryUpdated?.Invoke();
            }

            return unadded;
        }

        /// <summary>
        /// Removes a quantity of an item from the inventory.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity)
        {
            if (_state == null) return false;

            bool success = _state.RemoveItem(itemId, quantity);
            if (success)
            {
                OnInventoryUpdated?.Invoke();
            }

            return success;
        }

        public int CountItem(string itemId) => _state != null ? _state.CountItem(itemId) : 0;

        public bool HasItem(string itemId, int quantity = 1) => _state != null && _state.HasItem(itemId, quantity);

        public InventorySlot GetSlot(int index) => _state != null ? _state.GetSlot(index) : new InventorySlot(string.Empty, 0);

        public void SetSlot(int index, InventorySlot slot)
        {
            if (_state == null) return;
            _state.SetSlot(index, slot);
            OnSlotChanged?.Invoke(index, slot);
            OnInventoryUpdated?.Invoke();
        }

        /// <summary>
        /// Returns an isolated snapshot of the active inventory state.
        /// </summary>
        public InventoryState GetStateCopy()
        {
            return _state != null ? _state.Clone() : new InventoryState(_capacity);
        }

        /// <summary>
        /// Restores inventory state from a saved or cloned state.
        /// </summary>
        public void LoadState(InventoryState newState)
        {
            if (newState == null) return;
            _state = newState.Clone();
            OnInventoryUpdated?.Invoke();
        }

        /// <summary>
        /// Clears all inventory contents.
        /// </summary>
        public void Clear()
        {
            if (_state == null) return;
            _state.Clear();
            OnInventoryUpdated?.Invoke();
        }
    }
}
