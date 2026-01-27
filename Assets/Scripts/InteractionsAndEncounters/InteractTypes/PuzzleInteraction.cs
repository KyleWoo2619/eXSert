/*
    Written by Brandon Wahl

    Specialized unlockable interaction for puzzles.
    Place this script where you want a puzzle to be interacted with and activated by the player.
    Don't forget to assign the puzzle script that implements IPuzzleInterface in the inspector!
    Remember this should be placed where you want the player to START the puzzle from; not necessarily where the puzzle itself is located.
*/

using UnityEngine;

public class PuzzleInteraction : UnlockableInteraction
{
    [Tooltip("Insert puzzle script that implements IPuzzleInterface")]
    [SerializeField] private MonoBehaviour puzzleScript;

    private IPuzzleInterface _puzzleInterface;

    protected override void Awake()
    {
        base.Awake();
        
        // Cache interface reference
        if (puzzleScript != null)
            _puzzleInterface = puzzleScript.GetComponent<IPuzzleInterface>();
    }

    protected override void ExecuteInteraction()
    {
        if (_puzzleInterface == null)
        {
            Debug.LogError($"Puzzle script does not implement IPuzzleInterface on {gameObject.name}");
            return;
        }

        if (_puzzleInterface.isCompleted)
            _puzzleInterface.EndPuzzle();
        else
            _puzzleInterface.StartPuzzle();
    }
}
