using FarmASU.Inventory;
using UnityEngine;

namespace FarmASU.Core
{
    /// <summary>
    /// Physical item dropped in the game world that can be collected via player interaction.
    /// Implements IInteractable. Supports complete and partial collections with zero item loss.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldPickup : MonoBehaviour, IInteractable
    {
        [Header("Item Data")]
        [Tooltip("The static item definition representing this pickup.")]
        [SerializeField] private ItemDefinition _itemDefinition;

        [Tooltip("The quantity of items contained in this world pickup.")]
        [Range(1, 999)]
        [SerializeField] private int _quantity = 1;

        public ItemDefinition ItemDefinition => _itemDefinition;
        public int Quantity => _quantity;

        public string InteractionPrompt
        {
            get
            {
                if (_itemDefinition == null) return "Pick up [E]";
                return _quantity > 1
                    ? $"Pick up {_itemDefinition.DisplayName} (x{_quantity}) [E]"
                    : $"Pick up {_itemDefinition.DisplayName} [E]";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            return _itemDefinition != null && _quantity > 0;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            InventoryController inventory = interactor.GetComponentInChildren<InventoryController>();
            if (inventory == null)
            {
                inventory = interactor.GetComponentInParent<InventoryController>();
            }

            if (inventory == null)
            {
                Debug.LogWarning($"[WorldPickup] Interactor '{interactor.name}' does not have an InventoryController component.");
                return;
            }

            int unadded = inventory.TryAddItem(_itemDefinition, _quantity);

            if (unadded <= 0)
            {
                // Full collection: destroy the pickup object
                Destroy(gameObject);
            }
            else if (unadded < _quantity)
            {
                // Partial collection: retain remaining quantity in the world
                _quantity = unadded;
                Debug.Log($"[WorldPickup] Partially collected. {_quantity} remaining in world.");
            }
            else
            {
                // Rejection: inventory full
                Debug.Log($"[WorldPickup] Inventory is full. Could not collect {_itemDefinition.DisplayName}.");
            }
        }

        /// <summary>
        /// Initializer used when dynamically spawning pickups or running tests.
        /// </summary>
        public void Initialize(ItemDefinition item, int quantity)
        {
            _itemDefinition = item;
            _quantity = Mathf.Max(1, quantity);
        }
    }
}
