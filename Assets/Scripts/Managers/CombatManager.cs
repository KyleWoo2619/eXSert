using UnityEngine;
using Singletons;

public class CombatManager : Singleton<CombatManager>
{
    public static bool singleTargetMode { get; private set; } = true;
}
