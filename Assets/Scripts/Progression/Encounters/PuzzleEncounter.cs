using System.Collections.Generic;
using UnityEngine;

namespace Progression.Encounters
{
    public class PuzzleEncounter : BasicEncounter
    {
        protected override Color DebugColor { get => Color.purple; }

        private PuzzlePart part;
        private PuzzleInteraction interactPoint;

        /// <summary>
        /// Override of isCompleted that checks the completion status of the puzzle part instead.
        /// </summary>
        public override bool isCompleted => part.isCompleted;

        protected override void SetupEncounter()
        {
            part = FindPieces<PuzzlePart>();
            interactPoint = FindPieces<PuzzleInteraction>();

            if (part != null && interactPoint != null)
                interactPoint.ButtonPressed += part.ConsoleInteracted;
        }

        protected override void CleanupEncounter()
        {
            if (part != null && interactPoint != null)
                interactPoint.ButtonPressed -= part.ConsoleInteracted;

            part = null;
            interactPoint = null;

            base.CleanupEncounter();
        }

        /// <summary>
        /// Generic method to find the first component of type T in the child objects of this encounter. 
        /// Logs an error if none are found, and a warning if multiple are found (using the first one in that case).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T FindPieces<T>() where T : Component
        {
            T[] pieces = GetComponentsInChildren<T>();
            if (pieces.Length == 0)
            {
                Debug.LogError($"[PuzzleEncounter] No {typeof(T).Name} scripts found in child objects in encounter {gameObject.name}.");
                return null;
            }

            else if (pieces.Length > 1)
                Debug.LogWarning($"[PuzzleEncounter] Multiple {typeof(T).Name} scripts found in child objects of encounter {gameObject.name}. Using the first one found: {pieces[0].name}.");
            
            return pieces[0];
        }
    }
}
