using System.Collections.Generic;
using UnityEngine;

namespace Progression.Encounters
{
    public class PuzzleEncounter : BasicEncounter
    {
        protected override Color DebugColor { get => Color.purple; }

        private PuzzlePart part;
        private IConsoleSelectable consoleSelectable;
        private PuzzleInteraction[] interactPoints;

        /// <summary>
        /// Override of isCompleted that checks the completion status of the puzzle part instead.
        /// </summary>
        public override bool isCompleted => part.isCompleted;

        protected override void SetupEncounter()
        {
            part = FindPieces<PuzzlePart>();
            consoleSelectable = part as IConsoleSelectable;
            interactPoints = GetComponentsInChildren<PuzzleInteraction>();

            if (part == null)
                return;

            if (interactPoints == null || interactPoints.Length == 0)
            {
                Debug.LogError($"[PuzzleEncounter] No {nameof(PuzzleInteraction)} scripts found in child objects in encounter {gameObject.name}.");
                return;
            }

            foreach (var interactPoint in interactPoints)
            {
                if (interactPoint == null)
                    continue;

                if (consoleSelectable != null)
                    interactPoint.ButtonPressedWithSender += consoleSelectable.ConsoleInteracted;
                else
                    interactPoint.ButtonPressed += part.ConsoleInteracted;
            }
        }

        protected override void CleanupEncounter()
        {
            if (part != null && interactPoints != null)
            {
                foreach (var interactPoint in interactPoints)
                {
                    if (interactPoint == null)
                        continue;

                    if (consoleSelectable != null)
                        interactPoint.ButtonPressedWithSender -= consoleSelectable.ConsoleInteracted;
                    else
                        interactPoint.ButtonPressed -= part.ConsoleInteracted;
                }
            }

            part = null;
            consoleSelectable = null;
            interactPoints = null;

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
