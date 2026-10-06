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

        private void Update()
        {
            if (_hotbar == null)
            {
                _hotbar = FindAnyObjectByType<HotbarController>();
                if (_hotbar != null)
                {
                    _hotbar.OnSelectedSlotChanged += OnSelectedSlotChanged;
                    var (slot, itemDef) = _hotbar.GetSelectedItem();
                    UpdateHeldItem(slot, itemDef);
                }
            }
        }

        [Header("Mount Configuration")]
        [Tooltip("Local offset of the mount point relative to RightHand bone (Mixamo palm center).")]
        [SerializeField] private Vector3 _mountLocalPosition = new Vector3(-0.02f, 0.10f, 0.02f);
        [SerializeField] private Vector3 _mountLocalRotation = Vector3.zero;

        [System.Serializable]
        public struct ItemOffsetConfig
        {
            public string itemId;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale;
        }

        [Header("Per-Item Calibration")]
        [SerializeField] private ItemOffsetConfig[] _customItemConfigs = new ItemOffsetConfig[]
        {
            new ItemOffsetConfig
            {
                itemId = "wood",
                localPosition = new Vector3(0f, 0f, 0f),
                localEulerAngles = new Vector3(0f, 0f, 90f),
                localScale = new Vector3(0.35f, 0.35f, 0.35f)
            },
            new ItemOffsetConfig
            {
                itemId = "stone",
                localPosition = new Vector3(0f, 0f, 0f),
                localEulerAngles = Vector3.zero,
                localScale = new Vector3(0.22f, 0.22f, 0.22f)
            }
        };

        private void FindOrCreateHandMount()
        {
            if (_handMount != null) return;

            if (_animator != null && _animator.avatar != null && _animator.avatar.isHuman)
            {
                Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand != null)
                {
                    Transform existing = rightHand.Find("HeldItemMount");
                    if (existing != null)
                    {
                        _handMount = existing;
                        _handMount.localPosition = _mountLocalPosition;
                        _handMount.localRotation = Quaternion.Euler(_mountLocalRotation);
                        return;
                    }
                    GameObject mount = new GameObject("HeldItemMount");
                    mount.transform.SetParent(rightHand, false);
                    mount.transform.localPosition = _mountLocalPosition;
                    mount.transform.localRotation = Quaternion.Euler(_mountLocalRotation);
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
                    mount.transform.localPosition = _mountLocalPosition;
                    mount.transform.localRotation = Quaternion.Euler(_mountLocalRotation);
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
                
                // Apply per-item calibration if configured
                ItemOffsetConfig config = GetConfigFor(itemDef.Id);
                _currentHeldInstance.transform.localPosition = config.localPosition;
                _currentHeldInstance.transform.localRotation = Quaternion.Euler(config.localEulerAngles);
                if (config.localScale != Vector3.zero)
                {
                    _currentHeldInstance.transform.localScale = config.localScale;
                }

                // Disable colliders on held item so it doesn't mess with physics or interaction
                Collider[] cols = _currentHeldInstance.GetComponentsInChildren<Collider>();
                foreach (var c in cols)
                {
                    c.enabled = false;
                }

                _currentHeldItemId = itemDef.Id;
            }
        }

        private ItemOffsetConfig GetConfigFor(string itemId)
        {
            if (_customItemConfigs != null)
            {
                foreach (var cfg in _customItemConfigs)
                {
                    if (string.Equals(cfg.itemId, itemId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return cfg;
                    }
                }
            }

            return new ItemOffsetConfig
            {
                itemId = itemId,
                localPosition = Vector3.zero,
                localEulerAngles = Vector3.zero,
                localScale = Vector3.one
            };
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
