using UnityEngine;
using System.Collections;

/// <summary>
/// Lightweight global coroutine runner so we can run coroutines even when
/// the caller GameObject is disabled/inactive (e.g., after closing a dialog panel).
/// </summary>
public static class CoroutineRunner
{
    private class RunnerBehaviour : MonoBehaviour { }

    private static RunnerBehaviour _runner;

    private static void EnsureRunner()
    {
        if (_runner != null) return;
        var go = new GameObject("_CoroutineRunner");
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<RunnerBehaviour>();
    }

    public static Coroutine Run(IEnumerator routine)
    {
        EnsureRunner();
        return _runner.StartCoroutine(routine);
    }

    public static void Stop(Coroutine routine)
    {
        if (_runner != null && routine != null)
        {
            _runner.StopCoroutine(routine);
        }
    }
}
