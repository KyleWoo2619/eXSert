/*
    Written by Brandon Wahl

    This script handles puzzle activation based on collected interactables in the player's inventory.
    This will not be the only script on the puzzle, but it will handle checking if the puzzle can be activated.
    If the puzzle can be activated, it will trigger the puzzle logic from another script.
*/

using UnityEditor.EditorTools;
using UnityEngine;

public class PuzzleHandler : MonoBehaviour
{
    [Tooltip("Insert the ID of the puzzle needed to activate this puzzle; enter null if none is needed")][SerializeField] private string puzzleIDNeeded;
    [Tooltip("Insert puzzle script that implements IPuzzleInterface")][SerializeField] private MonoBehaviour puzzleScript;
    internal void ActivatePuzzle()
    {
        if(CheckIfPuzzleCanBeActivated())
        {
            Debug.Log("Activating Puzzle");
            // Add logic to activate the puzzle
        }
        else
        {
            Debug.Log("Puzzle cannot be activated yet. Required items not collected.");
        }
    }

    private bool CheckIfPuzzleCanBeActivated()
    {
        foreach(string collectedID in InternalPlayerInventory.Instance.collectedInteractables)
        {
            if(collectedID == puzzleIDNeeded)
            {
                Debug.Log("Puzzle Unlocked");
                // Add logic to unlock or activate the puzzle
                return true;
            }
        }
        return false;
    }
}
