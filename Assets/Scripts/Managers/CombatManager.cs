/* 
 * Written by Will T
 * 
 * Manages combat stances and guarding mechanics.
 * Allows switching between single target and area of effect stances.
 * Handles guarding state and parry window timing.
 */

using UnityEngine;
using Singletons;
using System;
using System.Collections;

public class CombatManager : Singleton<CombatManager>, IHealthSystem
{
    public float maxHealth;
    public float health;

    //Assigns the variables from the health interfaces to variables in this class
    float IHealthSystem.currentHP => health;
    float IHealthSystem.maxHP => maxHealth; 
    
    // Read-only property to check if in single target mode as a boolean
    public static bool singleTargetMode { get; private set; } = true;

    // Read-only property to get current stance as a string
    public static string currentStance => singleTargetMode ? "Single Target" : "Area of Effect";

    public static bool isGuarding { get; private set; } = false;

    [SerializeField, Range(0f, 1f)] private float _parryWindow = 0.3f;
    public static bool isParrying { get; private set; } = false;

    // Unity Action event for stance change for other scripts to subscribe to
    public static event Action OnStanceChanged;

    // Unity Action event for successful parry for other scripts to subscribe to
    public static event Action OnSuccessfulParry;

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
        Debug.Log("Player is now guarding. Parry window open");

        // Start parry window
        isParrying = true;
        Instance.StartCoroutine(ParryWindowCoroutine());
    }

    public static void ExitGuard()
    {
        isGuarding = false;
        Debug.Log("Player has stopped guarding.");

        // Stop parry window if still active
        if (isParrying)
            Instance.StopCoroutine(ParryWindowCoroutine());
    }

    // Coroutine to handle parry window timing
    private static IEnumerator ParryWindowCoroutine()
    {
        yield return new WaitForSeconds(Instance._parryWindow);

        isParrying = false;
        Debug.Log("Parry window closed.");
    }

    // Call this method when a parry is successful
    public static void ParrySuccessful()
    {
        Debug.Log("Parry successful! Counterattack opportunity granted.");

        OnSuccessfulParry?.Invoke();

        // Additional logic for successful parry can be added here
    }

    public void HealHP(float hp)
    {
        health += hp;

        //prevents going over max health
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    //Grabs the function from the health interface, updates the health count, and updates the health bar
    public void LoseHP(float damage)
    {
        health -= damage;    
    }

    //On death, if this is assigned to the player it will take them to the "Gameover" screen. If it is on any other object however, they will be destroyed.
    public void OnPlayerDeath()
    {
        if (health <= 0)
        {
            
        }
    }
}
