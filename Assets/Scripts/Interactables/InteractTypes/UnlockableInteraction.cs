/*
    Written by Brandon Wahl

    Unified base class for gated interactions (puzzles, doors, etc).
    Any interaction that requires a prerequisite item from the inventory.
    This combines the logic for both DoorInteractions and PuzzleInteraction.
*/

using UnityEngine;

public abstract class UnlockableInteraction : InteractionManager
{
    [Tooltip("Insert the ID of the item needed to unlock this interaction; leave empty if none is needed")]
    [SerializeField] protected string requiredItemID = "";

    protected override void Awake()
    {
        base.Awake();
        
        // Normalize required item ID
        if (!string.IsNullOrEmpty(requiredItemID))
            requiredItemID = requiredItemID.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Checks if the player has the required item to unlock this interaction.
    /// </summary>
    protected bool CanUnlock()
    {
        return InternalPlayerInventory.Instance.HasItem(requiredItemID);
    }

    /// <summary>
    /// Called when the interaction is successfully unlocked.
    /// Subclasses must implement this to define what happens when unlocked.
    /// </summary>
    protected abstract void ExecuteInteraction();

    protected override void Interact()
    {
        if (CanUnlock())
        {
            ExecuteInteraction();
        }
        else
        {
            Debug.Log($"Cannot interact yet. Required item: {requiredItemID}");
        }
    }
}
