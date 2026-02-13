/*
Written by Brandon Wahl

This script is used to determine how much damage should be dealt when collided with using the attack interface. It also counts the weapon name for debugging purposes.


*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class HitboxDamageManager : MonoBehaviour, IAttackSystem
{
    [SerializeField] private string weaponName = "";
    [SerializeField] private float damageAmount;
    [SerializeField, Tooltip("Maximum unique enemies this hitbox may damage per activation. 0 = unlimited.")]
    private int maxTargetsPerActivation;
    [SerializeField, Tooltip("Tag treated as a boss target for damage.")]
    private string bossTag = "Boss";

    private BoxCollider boxCollider;
    private HashSet<int> hitThisActivation = new HashSet<int>(); // Track which enemies were hit during this activation
    float IAttackSystem.damageAmount => damageAmount;
    string IAttackSystem.weaponName => weaponName;
    public void Configure(string weapon, float damage, int maxTargets)
    {
        weaponName = weapon;
        damageAmount = damage;
        maxTargetsPerActivation = Mathf.Max(0, maxTargets);
    }


    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        // Make sure we have a kinematic RB so trigger messages fire
        var rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    void OnEnable()  
    { 
        Debug.Log($"{weaponName} hitbox ENABLED - NEW ACTIVATION (HashSet had {hitThisActivation.Count} entries)");
        
        // Clear hit tracking for fresh attack activation
        hitThisActivation.Clear();
        Debug.Log($"{weaponName} HashSet cleared - now has {hitThisActivation.Count} entries");
        
        // Check for enemies already overlapping when hitbox activates
        // This handles cases where enemies are already in range when attack starts
        StartCoroutine(CheckInitialOverlaps());
    }
    
    // Public method to manually clear hit tracking (for debugging)
    public void ClearHitTracking()
    {
        Debug.Log($"{weaponName} MANUALLY clearing hit tracking (had {hitThisActivation.Count} entries)");
        hitThisActivation.Clear();
    }
    
    private System.Collections.IEnumerator CheckInitialOverlaps()
    {
        // Wait a frame to ensure the collider is properly enabled
        yield return null;
        
        if (!boxCollider.enabled) yield break;
        
        // Check what's currently overlapping this hitbox
        Collider[] overlapping = Physics.OverlapBox(
            boxCollider.bounds.center, 
            boxCollider.bounds.extents, 
            transform.rotation
        );
        
        // Debug.Log($"{weaponName} checking {overlapping.Length} overlapping colliders on activation");
        
        foreach (var collider in overlapping)
        {
            if (collider != boxCollider) // Don't hit ourselves
            {
                ProcessPotentialHit(collider);
            }
        }
    }
    
    void OnDisable() 
    { 
        // Debug.Log($"{weaponName} hitbox DISABLED - hit {hitThisActivation.Count} enemies during this activation");
        // Note: We keep hitThisActivation data until next OnEnable() clears it
    }


    private void ProcessPotentialHit(Collider other)
    {
        // Debug.Log($"{weaponName} processing potential hit on {other.gameObject.name} (Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        // Debug.Log($"  - Root object: {other.transform.root.name} (Tag: {other.transform.root.tag})");
        // Debug.Log($"  - All components on this object: {string.Join(", ", other.GetComponents<Component>().Select(c => c.GetType().Name))}");
        
        // IMPORTANT: Only damage enemies/bosses, never the player or player's components
        if (!IsDamageableTag(other)) 
        {
            // Debug.Log($"{weaponName} hit non-enemy object: {other.gameObject.name} with tag '{other.tag}' - ignoring");
            return;
        }
        
        if (maxTargetsPerActivation > 0 && hitThisActivation.Count >= maxTargetsPerActivation)
        {
            return;
        }

        // Additional safety: Don't hit the player even if they somehow have "Enemy" tag
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            // Debug.LogWarning($"{weaponName} blocked attempt to damage player object: {other.gameObject.name}");
            return;
        }
        
        // Find IHealthSystem on the enemy (could be on root or any parent)
        var health = other.GetComponentInParent<IHealthSystem>();
        if (health == null) 
        {
            // Debug.LogWarning($"{weaponName} hit enemy {other.gameObject.name} but no IHealthSystem found!");
            return;
        }
        
        // Additional safety: Make sure this is actually an enemy component, not player
        var healthComp = health as Component;
        if (healthComp.CompareTag("Player"))
        {
            // Debug.LogWarning($"{weaponName} tried to damage player - blocked for safety");
            return;
        }
        
        // One hit per activation: Check if this enemy was already hit during current activation
        int enemyId = healthComp.GetInstanceID();
        // Debug.Log($"{weaponName} checking enemy ID {enemyId} ({healthComp.name}) - HashSet currently has {hitThisActivation.Count} entries");
        
        if (hitThisActivation.Contains(enemyId))
        {
            // Debug.Log($"{weaponName} BLOCKED: already hit {healthComp.name} (ID: {enemyId}) during this activation");
            return;
        }
        
        // Mark this enemy as hit during this activation
        hitThisActivation.Add(enemyId);
        // Debug.Log($"{weaponName} ADDED enemy {healthComp.name} (ID: {enemyId}) to hit tracking - HashSet now has {hitThisActivation.Count} entries");
        
        // Apply damage via the interface
        float beforeHP = health.currentHP;
        health.LoseHP(damageAmount);
        float afterHP = health.currentHP;
        
        // Debug.Log($"SUCCESS: {weaponName} hit {healthComp.name} for {damageAmount} damage! Health: {beforeHP} -> {afterHP} (Max: {health.maxHP})");
        
        // Tell the enemy AI it was attacked (for state machine reactions)
        var enemy = other.GetComponentInParent<BaseEnemy<EnemyState, EnemyTrigger>>();
        if (enemy != null)
        {
            enemy.TryFireTriggerByName("Attacked");
            // Debug.Log($"{weaponName} fired 'Attacked' trigger on {enemy.name}");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Only process if hitbox is actually enabled
        if (!boxCollider.enabled) return;
        
        // Debug.Log($"{weaponName} OnTriggerEnter with {other.gameObject.name}");
        ProcessPotentialHit(other);
    }

    private bool IsDamageableTag(Component component)
    {
        if (component == null) return false;
        return component.CompareTag("Enemy") || (!string.IsNullOrWhiteSpace(bossTag) && component.CompareTag(bossTag));
    }
    

    private void OnDrawGizmos()
    {
        if (!boxCollider) return;
        
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        
        // Set color based on whether hitbox is active
        if (boxCollider.enabled)
        {
            // Active hitbox - VERY BRIGHT RED with strong fill
            Gizmos.color = new Color(1f, 0f, 0f, 0.7f); // Bright red with 70% opacity
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            
            // Thick bright outline
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            
            // Draw a yellow outline to make it REALLY stand out
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size * 1.02f);
        }
        else
        {
            // Inactive hitbox - subtle gray
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}
