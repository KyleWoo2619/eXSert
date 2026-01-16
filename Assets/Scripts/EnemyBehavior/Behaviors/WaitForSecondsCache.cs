using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Caches WaitForSeconds instances to avoid garbage collection from repeated allocations.
/// </summary>
public static class WaitForSecondsCache
{
    private static readonly Dictionary<float, WaitForSeconds> cache = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds Get(float seconds)
    {
        if (!cache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            cache[seconds] = wait;
        }
        return wait;
    }
}
