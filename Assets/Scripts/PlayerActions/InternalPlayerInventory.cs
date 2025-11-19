/*
    Written by Brandon Wahl

    This script manages the internal inventory of the player, keeping track of collected interactable items.
    This will be called by InteractablePoint when the player collects an item.
*/

using System.Collections.Generic;
using UnityEngine;
using Singletons;
public class InternalPlayerInventory : Singleton<InternalPlayerInventory>
{
    internal List<string> collectedInteractables = new List<string>();

    protected override void Awake()
    {
        AddCollectible("null"); // Adding "null" as a default collected item

        base.Awake();
    }

    public void AddCollectible(string collectibleId)
    {
        if (!collectedInteractables.Contains(collectibleId))
        {
            collectedInteractables.Add(collectibleId);
            Debug.Log($"Collectible {collectibleId} added to inventory.");
        }
        else
        {
            Debug.Log($"Collectible {collectibleId} is already in inventory.");
        }
    }
}
