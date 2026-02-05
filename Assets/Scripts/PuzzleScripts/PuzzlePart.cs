/*
 * Written by Will T, inspired by Brandon's Puzzle Interface script
 * 
 * Abstract class for puzzle parts across different puzzles.
 * Helps organize puzzle-related scripts for PuzzleEncounter script.
 */

using UnityEngine;

public abstract class PuzzlePart : MonoBehaviour
{
    public bool isCompleted { get; set; }

    public abstract void EndPuzzle();
    public abstract void StartPuzzle();
    public abstract void ConsoleInteracted();
}
