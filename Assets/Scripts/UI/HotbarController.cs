using System;
using FarmASU.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmASU.UI
{
    /// <summary>
    /// Controls the on-screen Hotbar UI.
    /// Bridges InventoryController data to the visual HotbarSlotUI slots.
    /// Manages slot selection via number keys (1-8) and mouse scroll wheel.
    /// </summary>
    public class HotbarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private ItemDatabase _itemDatabase;
        [SerializeField] private HotbarSlotUI[] _slotViews;

        [Header("Configuration")]
        [Tooltip("Number of slots displayed on the active hotbar.")]
        [SerializeField] private int _hotbarSize = 8;

        private int _selectedSlotIndex = 0;

        public int SelectedSlotIndex => _selectedSlotIndex;

        /// <summary>
        /// Fired whenever the active selected slot changes. Parameters: (slotIndex, slot, itemDef)
        /// </summary>
        public event Action<int, InventorySlot, ItemDefinition> OnSelectedSlotChanged;

        private void Start()
        {
            if (_inventory == null)
            {
                _inventory = FindAnyObjectByType<InventoryController>();
            }

            if (_itemDatabase == null)
            {
                _itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
            }

            if (_inventory != null)
            {
                _inventory.OnInventoryUpdated += RefreshAllSlots;
                _inventory.OnSlotChanged += OnInventorySlotChanged;
            }

            InitializeSlots();
            RefreshAllSlots();
            NotifySelectionChanged();
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryUpdated -= RefreshAllSlots;
                _inventory.OnSlotChanged -= OnInventorySlotChanged;
            }
        }

        private void Update()
        {
            HandleSlotSelectionInput();
        }

        private void InitializeSlots()
        {
            if (_slotViews == null) return;

            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] != null)
                {
                    _slotViews[i].Initialize(i, (i + 1).ToString());
                }
            }
        }

        private void HandleSlotSelectionInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) SelectSlot(2);
                else if (keyboard.digit4Key.wasPressedThisFrame) SelectSlot(3);
                else if (keyboard.digit5Key.wasPressedThisFrame) SelectSlot(4);
                else if (keyboard.digit6Key.wasPressedThisFrame) SelectSlot(5);
                else if (keyboard.digit7Key.wasPressedThisFrame) SelectSlot(6);
                else if (keyboard.digit8Key.wasPressedThisFrame) SelectSlot(7);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.05f)
                {
                    // Scroll up -> previous slot
                    int prev = (_selectedSlotIndex - 1 + _hotbarSize) % _hotbarSize;
                    SelectSlot(prev);
                }
                else if (scroll < -0.05f)
                {
                    // Scroll down -> next slot
                    int next = (_selectedSlotIndex + 1) % _hotbarSize;
                    SelectSlot(next);
                }
            }
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= _hotbarSize) return;
            if (_selectedSlotIndex == index) return;

            _selectedSlotIndex = index;
            RefreshAllSlots();
            NotifySelectionChanged();
        }

        public void RefreshAllSlots()
        {
            if (_slotViews == null) return;

            for (int i = 0; i < _slotViews.Length; i++)
            {
                UpdateSlotView(i);
            }
        }

        private void OnInventorySlotChanged(int index, InventorySlot slot)
        {
            if (index < _hotbarSize)
            {
                UpdateSlotView(index);
                if (index == _selectedSlotIndex)
                {
                    NotifySelectionChanged();
                }
            }
        }

        private void UpdateSlotView(int index)
        {
            if (index < 0 || _slotViews == null || index >= _slotViews.Length) return;

            HotbarSlotUI slotView = _slotViews[index];
            if (slotView == null) return;

            InventorySlot slot = (_inventory != null && index < _inventory.SlotCount)
                ? _inventory.GetSlot(index)
                : new InventorySlot(string.Empty, 0);

            ItemDefinition itemDef = null;
            if (!slot.IsEmpty && _itemDatabase != null)
            {
                itemDef = _itemDatabase.GetItem(slot.ItemId);
            }

            bool isSelected = (index == _selectedSlotIndex);
            slotView.SetSlot(slot, itemDef, isSelected);
        }

        private void NotifySelectionChanged()
        {
            InventorySlot slot = (_inventory != null && _selectedSlotIndex < _inventory.SlotCount)
                ? _inventory.GetSlot(_selectedSlotIndex)
                : new InventorySlot(string.Empty, 0);

            ItemDefinition itemDef = null;
            if (!slot.IsEmpty && _itemDatabase != null)
            {
                itemDef = _itemDatabase.GetItem(slot.ItemId);
            }

            OnSelectedSlotChanged?.Invoke(_selectedSlotIndex, slot, itemDef);
        }

        /// <summary>
        /// Gets the currently selected inventory slot and item definition.
        /// </summary>
        public (InventorySlot slot, ItemDefinition itemDef) GetSelectedItem()
        {
            InventorySlot slot = (_inventory != null && _selectedSlotIndex < _inventory.SlotCount)
                ? _inventory.GetSlot(_selectedSlotIndex)
                : new InventorySlot(string.Empty, 0);

            ItemDefinition itemDef = (!slot.IsEmpty && _itemDatabase != null)
                ? _itemDatabase.GetItem(slot.ItemId)
                : null;

            return (slot, itemDef);
        }
    }
}
