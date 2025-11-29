using UnityEngine;
using Singletons;
using System.Collections.Generic;

public class PlayerProgressionDetector : Singleton<PlayerProgressionDetector>
{
    internal Dictionary<int, bool> actCompletionMap = new Dictionary<int, bool>()
    {
        { 0, false },
        { 1, false },
        { 2, false },
        { 3, false },
        { 4, false }
    };


    protected override void Awake()
    {
        base.Awake();
    }
}
