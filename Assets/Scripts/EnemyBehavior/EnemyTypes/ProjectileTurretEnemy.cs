using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ProjectileTurretEnemy : BaseTurretEnemy
{
    [Header("Projectile Turret")]
    [Tooltip("Override default fire cooldown for the standard projectile turret.")]
    [SerializeField] private float overrideFireCooldown = -1f;

    protected override void Awake()
    {
        base.Awake();
        // Optionally override fire rate in inspector for this turret type
        if (overrideFireCooldown > 0f)
        {
            // Use reflection-safe pattern: fireCooldown is protected in base
            var field = typeof(BaseTurretEnemy).GetField("fireCooldown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(this, overrideFireCooldown);
        }
    }
}