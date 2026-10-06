using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmASU.Inventory
{
    /// <summary>
    /// Central catalog asset mapping stable string IDs to their static ItemDefinition assets.
    /// Used by Save/Load systems and spawning to resolve definitions from saved string IDs.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "FarmASU/Inventory/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [Tooltip("List of all recognized item definitions in the project.")]
        [SerializeField] private ItemDefinition[] _items = Array.Empty<ItemDefinition>();

        private readonly Dictionary<string, ItemDefinition> _lookup = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        private bool _isInitialized;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup.Clear();
            if (_items == null) return;

            foreach (var item in _items)
            {
                if (item != null && !string.IsNullOrEmpty(item.Id))
                {
                    _lookup[item.Id] = item;
                }
            }
            _isInitialized = true;
        }

        /// <summary>
        /// Retrieves an ItemDefinition matching the specified string ID.
        /// Returns null if not found.
        /// </summary>
        public ItemDefinition GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (!_isInitialized || _lookup.Count == 0 && _items.Length > 0)
            {
                BuildLookup();
            }

            _lookup.TryGetValue(id, out var item);
            return item;
        }

        /// <summary>
        /// Test or runtime helper to inject items dynamically into database.
        /// </summary>
        public void SetItemsForTesting(ItemDefinition[] items)
        {
            _items = items;
            BuildLookup();
        }
    }
}
