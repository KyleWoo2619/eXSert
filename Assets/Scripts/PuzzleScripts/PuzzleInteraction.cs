/*
    Written by Brandon Wahl

    Specialized unlockable interaction for puzzles.
    Place this script where you want a puzzle to be interacted with and activated by the player.
    Don't forget to assign the puzzle script that implements IPuzzleInterface in the inspector!
    Remember this should be placed where you want the player to START the puzzle from; not necessarily where the puzzle itself is located.

    Editted by Will T
        - Added ButtonPressed event to allow for more flexible puzzle interactions
*/

using System;
using UnityEngine;

public class PuzzleInteraction : UnlockableInteraction
{
    private bool inProgress;

    public event Action ButtonPressed;

    protected override void ExecuteInteraction()
    {
        Debug.Log($"Executing puzzle interaction on {gameObject.name}.");
        if (ButtonPressed == null)
            Debug.LogWarning("ButtonPressed event has no subscribers. Make sure to subscribe to it in order for the puzzle interaction to work.");
        ButtonPressed?.Invoke();
    }
}
