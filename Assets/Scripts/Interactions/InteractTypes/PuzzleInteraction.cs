/*
    Written by Brandon Wahl

    Place this script where you want a puzzle to be interacted with and activated by the player.
    Don't forget to assign the puzzle script that implements IPuzzleInterface in the inspector!
    Make sure to also assign the puzzleIDNeeded if the puzzle requires an item to be collected first, if none is needed, than make it null.
    Remember this should be placed where you want the player to START the puzzle from; not necessarily where the puzzle itself is located.
*/

using UnityEngine;

public class PuzzleInteraction : InteractionManager
{
    [Tooltip("Insert the ID of the puzzle needed to activate this puzzle; enter null if none is needed")][SerializeField] internal string puzzleIDNeeded;
    [Tooltip("Insert puzzle script that implements IPuzzleInterface")][SerializeField] private MonoBehaviour puzzleScript;

    internal void ActivatePuzzle()
    {
        if(CheckIfPuzzleCanBeActivated())
        {
            if(puzzleScript.GetComponent<IPuzzleInterface>().isCompleted)
            {
                puzzleScript.GetComponent<IPuzzleInterface>().EndPuzzle();
                return;
            }
            else
            {
                puzzleScript.GetComponent<IPuzzleInterface>().StartPuzzle();  
                return;
            }
            
        }
        else
        {
            Debug.Log("Puzzle cannot be activated yet. Required items not collected.");
            return;
        }
    }

    private bool CheckIfPuzzleCanBeActivated()
    {
        foreach(string collectedID in InternalPlayerInventory.Instance.collectedInteractables)
        {
            if(collectedID == puzzleIDNeeded)
            {
                Debug.Log("Puzzle Unlocked");
                return true;
            }
        }
        return false;
    }

    protected override void Interact()
    {
        bool has = InternalPlayerInventory.Instance.collectedInteractables.Contains(puzzleIDNeeded);

        if(has)
        {
            ActivatePuzzle();
        }
        else
        {
            Debug.Log("Cannot interact with puzzle yet. Required items not collected.");
        }
    }

}
