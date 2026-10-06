using FarmASU.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmASU.UI
{
    /// <summary>
    /// Visual component for a single slot in the Hotbar UI.
    /// Displays item icon, quantity badge, hotkey number, and selection border.
    /// </summary>
    public class HotbarSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _slotBackground;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _quantityText;
        [SerializeField] private TextMeshProUGUI _keyNumberText;
        [SerializeField] private Image _selectionHighlight;

        [Header("Colors & Styling")]
        [SerializeField] private Color _normalBgColor = new Color(0.12f, 0.12f, 0.14f, 0.75f);
        [SerializeField] private Color _selectedBgColor = new Color(0.24f, 0.22f, 0.18f, 0.90f);
        [SerializeField] private Color _selectedHighlightColor = new Color(1.0f, 0.85f, 0.35f, 1.0f);

        private int _slotIndex;

        public int SlotIndex => _slotIndex;

        public void Initialize(int slotIndex, string keyLabel)
        {
            _slotIndex = slotIndex;
            if (_keyNumberText != null)
            {
                _keyNumberText.text = keyLabel;
            }
        }

        public void SetSlot(InventorySlot slot, ItemDefinition itemDef, bool isSelected)
        {
            // Selection highlight
            if (_selectionHighlight != null)
            {
                _selectionHighlight.gameObject.SetActive(isSelected);
                if (isSelected)
                {
                    _selectionHighlight.color = _selectedHighlightColor;
                }
            }

            if (_slotBackground != null)
            {
                _slotBackground.color = isSelected ? _selectedBgColor : _normalBgColor;
            }

            // Empty slot
            if (slot.IsEmpty || itemDef == null)
            {
                if (_itemIcon != null)
                {
                    _itemIcon.gameObject.SetActive(false);
                    _itemIcon.sprite = null;
                }

                if (_quantityText != null)
                {
                    _quantityText.gameObject.SetActive(false);
                    _quantityText.text = string.Empty;
                }
                return;
            }

            // Populated slot
            if (_itemIcon != null)
            {
                if (itemDef.Icon != null)
                {
                    _itemIcon.gameObject.SetActive(true);
                    _itemIcon.sprite = itemDef.Icon;
                    _itemIcon.color = Color.white;
                }
                else
                {
                    // Fallback if no sprite assigned yet
                    _itemIcon.gameObject.SetActive(true);
                    _itemIcon.sprite = null;
                    _itemIcon.color = new Color(0.8f, 0.65f, 0.45f, 0.8f);
                }
            }

            if (_quantityText != null)
            {
                if (slot.Quantity > 1)
                {
                    _quantityText.gameObject.SetActive(true);
                    _quantityText.text = slot.Quantity.ToString();
                }
                else
                {
                    _quantityText.gameObject.SetActive(false);
                    _quantityText.text = string.Empty;
                }
            }
        }
    }
}
