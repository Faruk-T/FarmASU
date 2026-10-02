using System;
using UnityEngine;

namespace FarmASU.Inventory
{
    /// <summary>
    /// Pure C# data structure representing a single slot within an inventory.
    /// Serializable for Unity inspector and future Save/Load systems.
    /// </summary>
    [Serializable]
    public struct InventorySlot
    {
        [Tooltip("Stable string ID of the item residing in this slot.")]
        [SerializeField] private string _itemId;

        [Tooltip("Current stack quantity in this slot.")]
        [SerializeField] private int _quantity;

        public InventorySlot(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
            {
                _itemId = string.Empty;
                _quantity = 0;
            }
            else
            {
                _itemId = itemId;
                _quantity = quantity;
            }
        }

        public string ItemId
        {
            get => _itemId;
            set => _itemId = value;
        }

        public int Quantity
        {
            get => _quantity;
            set => _quantity = Mathf.Max(0, value);
        }

        /// <summary>
        /// True if no item occupies this slot or quantity is zero.
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(_itemId) || _quantity <= 0;

        /// <summary>
        /// Resets the slot to empty state.
        /// </summary>
        public void Clear()
        {
            _itemId = string.Empty;
            _quantity = 0;
        }

        public override string ToString()
        {
            return IsEmpty ? "[Empty]" : $"{_itemId} x{_quantity}";
        }
    }
}
