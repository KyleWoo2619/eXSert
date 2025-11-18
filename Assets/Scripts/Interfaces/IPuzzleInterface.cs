/*
    Written by Brandon Wahl

    This interface will be implented on each indivdual puzzle script to ensure they have a StartPuzzle function, so the puzzle handler
    can always call the right function.
*/

public interface IPuzzleInterface
{
   public void StartPuzzle();
}
