using System;
using UnityEngine;

public enum AttackType
{
    LightSingle,
    LightAOE,
    HeavySingle,
    HeavyAOE
}

[Serializable]
[CreateAssetMenu(fileName = "Player Attack", menuName = "Attacks/Player Attack")]
public class PlayerAttack : ScriptableObject
{
    private enum AttackCategory
    {
        Single,
        AOE
    }
    private enum AttackWeight
    {
        Light,
        Heavy
    }

    // ---------------------------------------------------------------------------------------------

    [Header("General Properties")]
    [SerializeField, Tooltip("Name of the attack shown in UI and logs")]
    private string _attackName;

    // If no custom name is given, use the scriptable object's name
    public string attackName { get => _attackName != "" ? _attackName : this.name; }

    [SerializeField, Tooltip("Type of attack, single target or area of effect")]
    private AttackCategory _attackType = AttackCategory.Single;
    [SerializeField, Tooltip("Weight of the attack, light or heavy")]
    private AttackWeight _attackWeight = AttackWeight.Light;

    // Combines the two private enums into one public enum for easier use
    public AttackType attackType =>
        _attackType == AttackCategory.Single ?
            (_attackWeight == AttackWeight.Light ? AttackType.LightSingle : AttackType.HeavySingle) :
            (_attackWeight == AttackWeight.Light ? AttackType.LightAOE : AttackType.HeavyAOE);

    // ---------------------------------------------------------------------------------------------

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

    // ---------------------------------------------------------------------------------------------

    [Space, Header("Range Properties")]

    [SerializeField, Tooltip("Whether or not the attack should use a custom range or the default range of 1.5")]
    private bool _customRange = false;
    private static readonly float defaultRange = 1.5f;
    [SerializeField] private float _range = 1.5f;
    public float range { get => _customRange ? _range : defaultRange; }

    [SerializeField, Tooltip("Whether or not the attack should use a custom width or the default width of 1")]
    private bool _customWidth = false;
    private static readonly float defaultWidth = 1f;
    [SerializeField] private float _width = 1f;
    public float width { get => _customWidth ? _width : defaultWidth; }

    [SerializeField, Tooltip("Whether to use the default distance from player or a custom one")]
    private bool _defaultDistanceFromPlayer = true;
    [SerializeField, Tooltip("Custom distance from player if not using default")]
    private float _customAttackDistanceFromPlayer = 0.75f;
    public float distanceFromPlayer { get => _defaultDistanceFromPlayer ? 1f : range / 2f; }

    // Returns the dimensions of the area of effect attack, or zero vector if not AOE
    public Vector2 areaOfEffectDimensions
    {
        get
        {
            if (_attackType == AttackCategory.AOE)
                return new Vector2(width, range);
            else
                return Vector2.zero;
        }
    }

    // ---------------------------------------------------------------------------------------------

    [Space, Header("Timing")]

    [SerializeField, Tooltip("How much time after the attack starts before the attack damage is applied, " +
        "also prevents another attack or action to be started before this finished " +
        "This usually would incorporate how long the attack animation is")]
    private float _startLag = 0.2f;
    public float startLag { get => _startLag; }

    [SerializeField, Tooltip("How much time after the damage is applied before the next input can be registered")]
    private float _endLag = 0.2f;
    public float endLag { get => _endLag; }

    // ---------------------------------------------------------------------------------------------

    [Space, Header("Sound Effects")]

    [SerializeField, Tooltip("Sound effect played when the attack is performed")]
    private AudioClip _attackSFX;
    public AudioClip attackSFX { get => _attackSFX; }

    [SerializeField, Tooltip("Sound effect played when the attack lands/hits an enemy")]
    private AudioClip _hitSFX;
    public AudioClip hitSFX { get => _hitSFX; }

    // ---------------------------------------------------------------------------------------------

    // Creates and returns a hitbox GameObject for this attack at the specified position and forward direction
    public GameObject createHitBox(Vector3 playerPosition, Vector3 playerForward)
    {
        GameObject hitbox = new GameObject(attackName + " Hitbox");
        hitbox.transform.position = playerPosition + (playerForward * distanceFromPlayer);
        hitbox.transform.rotation = Quaternion.LookRotation(playerForward);
        BoxCollider hb = hitbox.AddComponent<BoxCollider>();
        hb.isTrigger = true;
        hb.size = new Vector3(x: width, y: 1f, z: range);

        // add additional components here as needed, e.g., HitboxDamageManager
        HitboxDamageManager damageManager = hitbox.AddComponent<HitboxDamageManager>();

        // set damage manager properties based on this attack
        damageManager.weaponName = attackName;
        damageManager.damageAmount = damage;

        // personal tip to learn how to use HitBoxDamageManager


        // put in a gizmo drawer for visualization


        return hitbox;
    }
}
