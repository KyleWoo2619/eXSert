using System.Collections.Generic;
using UnityEngine;

namespace Progression.Encounters
{
    public class PuzzleEncounter : BasicEncounter
    {
        protected override Color DebugColor { get => Color.purple; }

        private PuzzlePart part;
        public override bool isCompleted => part.isCompleted;

        protected override void SetupEncounter()
        {
            List<PuzzlePart> parts = new List<PuzzlePart>(GetComponentsInChildren<PuzzlePart>());
            if (parts.Count == 0)
            {
                Debug.LogError($"[PuzzleEncounter] No PuzzlePart scripts found in child objects in encounter {gameObject.name}.");
                return;
            }
            else if (parts.Count > 1)
            {
                Debug.LogWarning($"[PuzzleEncounter] Multiple PuzzlePart scripts found in child objects of encounter {gameObject.name}. Using the first one found.");
            }
            else
            {
                Debug.Log($"[PuzzleEncounter] Found PuzzlePart script in encounter {gameObject.name}.");
            }
            part = parts[0];
        }
    }
}

