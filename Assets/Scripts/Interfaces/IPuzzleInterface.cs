/*
    Written by Brandon Wahl

    This interface will be implemented on each indivdual puzzle script to ensure they have a StartPuzzle function, so the puzzle handler
    can always call the right function.
*/

using System;
using Unity.VisualScripting;

public interface IPuzzleInterface
{
   bool isCompleted { get; set; }
   
   void StartPuzzle();

   void EndPuzzle();
}
