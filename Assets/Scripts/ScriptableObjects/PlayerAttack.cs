using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Player Attack", menuName = "Attacks/Player Attack")]
public class PlayerAttack : ScriptableObject
{
    [Header("General Properties")]
    [SerializeField, Tooltip("Name of the attack shown in UI and logs")]
    private string _attackName;

    // If no custom name is given, use the scriptable object's name
    public string attackName { get => _attackName != "" ? _attackName : this.name; }

    [Space, Header("Damage Properties")]
    [SerializeField, Tooltip("Damage dealt by this attack")]
    private int _damage = 5;
    public int damage { get => _damage; }

    [SerializeField, Tooltip("Wether the attack should launch the enemy on hit")]
    private bool _launch = false;
    public bool launch { get => _launch; }
    [SerializeField, Tooltip("The amount of knockback applied to enemies hit")]
    private float _knockback = 3;
    public float knockback { get => _knockback; }
    [SerializeField, Tooltip("The angle at which enemies are knocked back. 0 is parallel to the ground")]
    private float _knockbackAngle = 0;
    public float knockbackAngle { get => _knockbackAngle; }

    [Space, Header("Range Properties")]
    [SerializeField, Tooltip("Whether or not the attack should use a custom range or the default range of 1.5")]
    private bool _customRange = false;
    private static readonly float defaultRange = 1.5f;
    [SerializeField] private float _range = 1.5f;
    public float range { get => _customRange ? _range : defaultRange; }

    [Space, Header("Timing")]
    [SerializeField, Tooltip("How much time after the attack starts before the attack damage is applied, " +
        "also prevents another attack or action to be started before this finished " +
        "This usually would incorporate how long the attack animation is")]
    private float _startLag = 0.2f;
    public float startLag { get => _startLag; }

    [SerializeField, Tooltip("How much time after the damage is applied before the next input can be registered")]
    private float _endLag = 0.2f;
    public float endLag { get => _endLag; }
}
