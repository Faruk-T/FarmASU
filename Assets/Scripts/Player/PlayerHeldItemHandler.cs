using FarmASU.Inventory;
using FarmASU.UI;
using UnityEngine;

namespace FarmASU.Player
{
    /// <summary>
    /// Attaches to the Player.
    /// Listens to HotbarController to dynamically spawn or display the currently selected item
    /// in the character's right hand.
    /// </summary>
    public class PlayerHeldItemHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HotbarController _hotbar;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _handMount;

        private GameObject _currentHeldInstance;
        private string _currentHeldItemId;

        private void Start()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_hotbar == null)
            {
                _hotbar = FindAnyObjectByType<HotbarController>();
            }

            FindOrCreateHandMount();

            if (_hotbar != null)
            {
                _hotbar.OnSelectedSlotChanged += OnSelectedSlotChanged;
                var (slot, itemDef) = _hotbar.GetSelectedItem();
                UpdateHeldItem(slot, itemDef);
            }
        }

        private void OnDestroy()
        {
            if (_hotbar != null)
            {
                _hotbar.OnSelectedSlotChanged -= OnSelectedSlotChanged;
            }
        }

        private void FindOrCreateHandMount()
        {
            if (_handMount != null) return;

            if (_animator != null && _animator.avatar != null && _animator.avatar.isHuman)
            {
                Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand != null)
                {
                    GameObject mount = new GameObject("HeldItemMount");
                    mount.transform.SetParent(rightHand, false);
                    mount.transform.localPosition = new Vector3(0.05f, 0.05f, 0.05f);
                    mount.transform.localRotation = Quaternion.Euler(0, 90f, 90f);
                    _handMount = mount.transform;
                    return;
                }
            }

            // Fallback search by bone name
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                string lower = child.name.ToLower();
                if (lower.Contains("righthand") || lower.Contains("hand.r") || lower.Contains("hand_r"))
                {
                    GameObject mount = new GameObject("HeldItemMount");
                    mount.transform.SetParent(child, false);
                    mount.transform.localPosition = new Vector3(0.05f, 0.05f, 0.05f);
                    _handMount = mount.transform;
                    return;
                }
            }

            _handMount = transform;
        }

        private void OnSelectedSlotChanged(int slotIndex, InventorySlot slot, ItemDefinition itemDef)
        {
            UpdateHeldItem(slot, itemDef);
        }

        private void UpdateHeldItem(InventorySlot slot, ItemDefinition itemDef)
        {
            if (slot.IsEmpty || itemDef == null || itemDef.HeldPrefab == null)
            {
                ClearHeldItem();
                return;
            }

            if (_currentHeldItemId == itemDef.Id && _currentHeldInstance != null)
            {
                // Already holding this item
                return;
            }

            ClearHeldItem();

            if (_handMount != null && itemDef.HeldPrefab != null)
            {
                _currentHeldInstance = Instantiate(itemDef.HeldPrefab, _handMount);
                _currentHeldInstance.transform.localPosition = Vector3.zero;
                _currentHeldInstance.transform.localRotation = Quaternion.identity;

                // Disable colliders on held item so it doesn't mess with physics or interaction
                Collider[] cols = _currentHeldInstance.GetComponentsInChildren<Collider>();
                foreach (var c in cols)
                {
                    c.enabled = false;
                }

                _currentHeldItemId = itemDef.Id;
            }
        }

        private void ClearHeldItem()
        {
            if (_currentHeldInstance != null)
            {
                Destroy(_currentHeldInstance);
                _currentHeldInstance = null;
            }
            _currentHeldItemId = null;
        }
    }
}
