using UnityEngine;
using System.Collections;

public class ComboManager : Singletons.Singleton<ComboManager>
{
    [Header("Attack Objects")]
    [SerializeField] PlayerAttack lightSingle;
    [SerializeField] PlayerAttack lightAOE;
    [SerializeField] PlayerAttack heavySingle;
    [SerializeField] PlayerAttack heavyAOE;

    public static int comboCount { get; private set; } = 0;

    [SerializeField, Tooltip("Time in seconds before the combo resets due to inactivity"), Range(0f, 3f)]
    private static float _comboResetTime = 2f;
    public static float comboResetTime { get => _comboResetTime; }

    private static float lastAttackTime = 0f;

    protected override void Awake()
    {
        base.Awake();

        lastAttackTime = Time.time;
    }


    public static PlayerAttack Attack(AttackType attackType)
    {
        comboCount++;

        switch (attackType)
        {
            case AttackType.LightSingle:
                Debug.Log("Performing Light Single Attack: " + Instance.lightSingle.attackName);
                // Add logic to execute light single attack
                return Instance.lightSingle;

            case AttackType.LightAOE:
                Debug.Log("Performing Light AOE Attack: " + Instance.lightAOE.attackName);
                // Add logic to execute light AOE attack
                return Instance.lightAOE;

            case AttackType.HeavySingle:
                Debug.Log("Performing Heavy Single Attack: " + Instance.heavySingle.attackName);
                // Add logic to execute heavy single attack
                return Instance.heavySingle;

            case AttackType.HeavyAOE:
                Debug.Log("Performing Heavy AOE Attack: " + Instance.heavyAOE.attackName);
                // Add logic to execute heavy AOE attack
                return Instance.heavyAOE;

            default:
                Debug.LogWarning("Unknown Attack Type");
                return null;
        }
    }

    public static IEnumerator WaitForInputReset()
    {
        while (true)
        {
            if (Time.time - lastAttackTime >= comboResetTime)
            {
                comboCount = 0;
                Debug.Log("Combo reset due to inactivity.");
                yield break;
            }
            yield return null;
        }
    }
}
