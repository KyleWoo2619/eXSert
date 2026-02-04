using System.Collections.Generic;
using UnityEngine;

namespace Progression.Encounters
{
    public class PuzzleEncounter : BasicEncounter
    {
        protected override Color DebugColor { get => Color.purple; }

        private PuzzlePart _partPuzzle;
        private PuzzleInteraction _puzzleInteraction;

        protected override bool isCompleted => _partPuzzle?.isCompleted ?? false;

        protected override void SetupEncounter()
        {
            _partPuzzle = FindPiece<PuzzlePart>();
            _puzzleInteraction = FindPiece<PuzzleInteraction>();
        }

        /// <summary>
        /// Searches the child objects of the current GameObject for a component of type <typeparamref name="T"/> and
        /// returns the first instance found.
        /// </summary>
        /// <remarks>If multiple components of type <typeparamref name="T"/> are found, only the first one
        /// is returned. A warning is logged in this case. If no component is found, an error is logged and <see
        /// langword="null"/> is returned.</remarks>
        /// <typeparam name="T">The type of <see cref="Component"/> to search for in the child objects.</typeparam>
        /// <param name="includeInactive"><see langword="true"/> to include inactive child GameObjects in the search; otherwise, <see
        /// langword="false"/> to search only active children. The default is <see langword="false"/>.</param>
        /// <returns>The first component of type <typeparamref name="T"/> found in the child objects, or <see langword="null"/>
        /// if no such component exists.</returns>
        private T FindPiece<T>(bool includeInactive = false) where T : Component
        {
            T[] pieces = GetComponentsInChildren<T>(includeInactive);
            if (pieces == null || pieces.Length == 0)
            {
                Debug.LogError($"[PuzzleEncounter] No {typeof(T).Name} scripts found in child objects in encounter {gameObject.name}.");
                return null;
            }
            if (pieces.Length > 1)
            {
                Debug.LogWarning($"[PuzzleEncounter] Multiple {typeof(T).Name} scripts found in child objects of encounter {gameObject.name}. Using the first one found.");
            }
            return pieces[0];
        }
    }
}

