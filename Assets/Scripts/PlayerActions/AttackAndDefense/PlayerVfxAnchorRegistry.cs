using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains a list of named VFX anchor transforms on the player rig so attacks can spawn
/// their particles in the correct spot (hands, weapon tips, feet, etc.).
/// </summary>
public class PlayerVfxAnchorRegistry : MonoBehaviour
{
    [Serializable]
    private struct AnchorEntry
    {
        public string id;
        public Transform anchor;
    }

    [SerializeField]
    private AnchorEntry[] anchors;

    private Dictionary<string, Transform> lookup;

    public Transform ResolveAnchor(string anchorId)
    {
        if (string.IsNullOrWhiteSpace(anchorId))
            return null;

        EnsureLookup();
        if (lookup.TryGetValue(anchorId, out var anchor) && anchor != null)
            return anchor;

        return null;
    }

    private void EnsureLookup()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
        if (anchors == null)
            return;

        for (int i = 0; i < anchors.Length; i++)
        {
            var entry = anchors[i];
            if (string.IsNullOrWhiteSpace(entry.id) || entry.anchor == null)
                continue;

            if (!lookup.ContainsKey(entry.id))
                lookup.Add(entry.id, entry.anchor);
            else
                lookup[entry.id] = entry.anchor; // last one wins to allow quick overrides
        }
    }

    private void OnValidate()
    {
        lookup = null;
        EnsureLookup();
    }
}
