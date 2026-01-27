using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Caches WaitForSeconds instances to avoid garbage collection from repeated allocations.
/// Call ClearCache() when exiting play mode or changing scenes to prevent memory leaks.
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

    /// <summary>
    /// Clears the cache. Call this when exiting play mode or unloading scenes.
    /// </summary>
    public static void ClearCache()
    {
        cache.Clear();
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Called when entering play mode in editor - clear any stale references
        cache.Clear();
    }
#endif
}
