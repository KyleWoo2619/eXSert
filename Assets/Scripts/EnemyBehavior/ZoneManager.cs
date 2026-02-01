using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Caches Zone references to avoid repeated FindObjectsByType allocations.
/// Add this component to a persistent GameObject in your scene.
/// </summary>
public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    private Zone[] cachedZones;
    private readonly List<Zone> tempZoneList = new List<Zone>(16);

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }
#endif

    private void Awake()
    {
        Instance = this;
        RefreshZoneCache();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            cachedZones = null;
        }
    }

    public void RefreshZoneCache()
    {
        cachedZones = FindObjectsByType<Zone>(FindObjectsSortMode.None);
    }

    public Zone[] GetAllZones() => cachedZones ?? System.Array.Empty<Zone>();

    /// <summary>
    /// Returns a read-only list of zones excluding the specified zone.
    /// Note: The returned list is reused - do not cache the reference.
    /// </summary>
    public IReadOnlyList<Zone> GetOtherZones(Zone currentZone)
    {
        tempZoneList.Clear();
        if (cachedZones == null) return tempZoneList;
        
        foreach (var zone in cachedZones)
        {
            if (zone != currentZone)
                tempZoneList.Add(zone);
        }
        return tempZoneList;
    }
}
