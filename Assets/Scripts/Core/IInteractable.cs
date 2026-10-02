using UnityEngine;

namespace FarmASU.Core
{
    /// <summary>
    /// Contract implemented by any world object that the player can physically interact with.
    /// Decouples physical detection from concrete object types (pickups, crops, NPCs, doors).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Text prompt displayed when the player targets this object (e.g. "Pick Up Stone [E]").
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Whether the interactor can currently interact with this object.
        /// </summary>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Executes the physical interaction.
        /// </summary>
        void Interact(GameObject interactor);
    }
}
