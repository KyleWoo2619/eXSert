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

    private void Awake()
    {
        Instance = this;
        RefreshZoneCache();
    }

    public void RefreshZoneCache()
    {
        cachedZones = FindObjectsByType<Zone>(FindObjectsSortMode.None);
    }

    public Zone[] GetAllZones() => cachedZones;

    public Zone[] GetOtherZones(Zone currentZone)
    {
        tempZoneList.Clear();
        foreach (var zone in cachedZones)
        {
            if (zone != currentZone)
                tempZoneList.Add(zone);
        }
        return tempZoneList.ToArray();
    }
}
