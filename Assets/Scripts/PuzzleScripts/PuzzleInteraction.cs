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
    [Tooltip("Insert puzzle script that derives from PuzzlePart")]
    [SerializeField] private MonoBehaviour puzzleScript;

    private bool inProgress;

    private PuzzlePart _puzzlePart;

    protected override void Awake()
    {
        base.Awake();
        
        // Cache interface reference
        if (puzzleScript != null)
            _puzzlePart = puzzleScript.GetComponent<PuzzlePart>();
    }

    protected override void ExecuteInteraction()
    {
        if (_puzzlePart == null)
        {
            Debug.LogError($"Puzzle script does not implement IPuzzleInterface on {gameObject.name}");
            return;
        }

        if (_puzzlePart.isCompleted)
        {
            _puzzlePart.EndPuzzle();
            inProgress = false;
        }
        else if(!inProgress)
        {
            _puzzlePart.StartPuzzle();
            inProgress = true;
        }
    }
}
