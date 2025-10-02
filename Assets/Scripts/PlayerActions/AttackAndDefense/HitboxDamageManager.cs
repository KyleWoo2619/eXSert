using UnityEngine;

public class HitboxDamageManager : MonoBehaviour, IAttackSystem
{
    [SerializeField] internal string weaponName = "";
    [SerializeField] internal float damageAmount;

    float IAttackSystem.damageAmount => damageAmount;
    string IAttackSystem.weaponName => weaponName;
}
