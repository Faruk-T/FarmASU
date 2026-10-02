using UnityEngine;

namespace FarmASU.Inventory
{
    /// <summary>
    /// Static definition asset for items in FarmASU.
    /// ScriptableObject containing immutable design properties.
    /// Holds no runtime state.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "FarmASU/Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable, unique string identifier used in saves (e.g. 'stone', 'wood').")]
        [SerializeField] private string _id;

        [Tooltip("User-facing display name of the item.")]
        [SerializeField] private string _displayName;

        [Header("Inventory Properties")]
        [Tooltip("Maximum quantity of this item that can stack in a single inventory slot.")]
        [Range(1, 999)]
        [SerializeField] private int _maxStack = 99;

        public string Id => _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? _id : _displayName;
        public int MaxStack => Mathf.Max(1, _maxStack);

        /// <summary>
        /// Editor helper or runtime initializer for unit tests.
        /// </summary>
        public void Initialize(string id, string displayName, int maxStack)
        {
            _id = id;
            _displayName = displayName;
            _maxStack = Mathf.Max(1, maxStack);
        }
    }
}
