/*
    Written by Brandon Wahl

    Place this script where you want an item to be interacted with and collected into the player's inventory.
*/

using UnityEngine;

public class ItemInteractions : InteractionManager
{
    protected override void Interact()
    {
        InternalPlayerInventory.Instance.AddCollectible(this.interactId);
        DeactivateInteractable(this);

        Debug.Log("Interacted with item: " + this.interactId);
    }

}
