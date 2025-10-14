using UnityEngine;
using Singletons;
using System;

public class CombatManager : Singleton<CombatManager>
{
    public static bool singleTargetMode { get; private set; } = true;
    public static string currentStance => singleTargetMode ? "Single Target" : "Area of Effect";

    public static bool isGuarding { get; private set; } = false;

    // Unity Action event for stance change for other scripts to subscribe to
    public static event Action OnStanceChanged;

    override protected void Awake()
    {
        base.Awake();
    }

    public static void ChangeStance()
    {
        singleTargetMode = !singleTargetMode;
        Debug.Log("Stance changed. Current Stance: " + currentStance);

        OnStanceChanged?.Invoke();
    }

    public static void EnterGuard()
    {
        isGuarding = true;
        Debug.Log("Player is now guarding.");
    }

    public static void ExitGuard()
    {
        isGuarding = false;
        Debug.Log("Player has stopped guarding.");
    }
}
