using UnityEngine;

public class CriticalReference : PropertyAttribute
{
    /// Optional flag you might extend in the future:
    public bool addIfMissing = false;

    public CriticalReference() { }
    public CriticalReference(bool addIfMissing)
    {
        this.addIfMissing = addIfMissing;
    }
}
