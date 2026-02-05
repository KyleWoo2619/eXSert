/*
    Written by Brandon Wahl

    Specialized unlockable interaction for puzzles.
    Place this script where you want a puzzle to be interacted with and activated by the player.
    Don't forget to assign the puzzle script that implements IPuzzleInterface in the inspector!
    Remember this should be placed where you want the player to START the puzzle from; not necessarily where the puzzle itself is located.
*/

using System;
using UnityEngine;

public class PuzzleInteraction : UnlockableInteraction
{
    private bool inProgress;

    public event Action ButtonPressed;

    protected override void ExecuteInteraction()
    {
        ButtonPressed?.Invoke();
    }
}
