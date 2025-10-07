/*
Written by Brandon Wahl

This script is the framework for eXsert's combo system. Here it juggles between the four hitboxes used and activates and deactivates them based on player input. 
It also checks for inactivity between inputs so the combo resets.


*/

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttackManager : MonoBehaviour { 

    [SerializeField] private int maxComboAmount = 5;
    [SerializeField] private float amountOfTimeBetweenAttacks = 1.5f;
    [SerializeField] private float hitboxActiveDuration = 0.2f; // How long each hitbox stays active
    protected float lastAttackPressTime;

    private InputReader input;
    private ChangeStance changeStance;

    [SerializeField] private BoxCollider[] comboHitboxes;

    //The list is used to easily track which number of the combo the player is on
    private List<BoxCollider> currentComboAmount = new List<BoxCollider>();

    private void Start()
    {
        input = InputReader.Instance;
        lastAttackPressTime = Time.time;
        changeStance = GetComponent<ChangeStance>();
        
        ValidateHitboxSetup();
    }
    
    /// <summary>
    /// Validates that all hitboxes have proper HitboxDamageManager components
    /// </summary>
    private void ValidateHitboxSetup()
    {
        Debug.Log($"PlayerAttackManager: Validating {comboHitboxes.Length} hitboxes...");
        
        for (int i = 0; i < comboHitboxes.Length; i++)
        {
            var col = comboHitboxes[i];
            if (col == null)
            {
                Debug.LogError($"PlayerAttackManager: Hitbox {i} is null! Please assign all hitbox colliders.");
                continue;
            }
            
            // Ensure trigger
            col.isTrigger = true;
            
            // NEW: ensure a kinematic rigidbody is present (required for trigger events)
            var rb = col.GetComponent<Rigidbody>();
            if (!rb)
            {
                rb = col.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Debug.Log($"PlayerAttackManager: Added kinematic Rigidbody to hitbox {i} ({col.name})");
            }
            
            // Validate HitboxDamageManager component
            HitboxDamageManager damageManager = col.GetComponent<HitboxDamageManager>();
            if (damageManager == null)
            {
                Debug.LogError($"PlayerAttackManager: Hitbox {i} ({col.name}) is missing HitboxDamageManager component!");
            }
            else
            {
                Debug.Log($"PlayerAttackManager: Hitbox {i} ({col.name}) ready - Damage: {damageManager.damageAmount}, Weapon: {damageManager.weaponName}");
            }
            
            // Optional but helpful: put on a dedicated layer
            // col.gameObject.layer = LayerMask.NameToLayer("PlayerAttack");
            
            // Ensure hitbox starts disabled
            col.enabled = false;
        }
    }

    private void Update()
    {
        InactivityCheck();
        Attack();
    }

    private void Attack()
    {
        //First determines whether the heavy or light input is detected
        if (input.LightAttackTrigger)
        {
            lastAttackPressTime = Time.time;
            input.LightAttackTrigger = false;

            Debug.Log("Combo Amount: " + currentComboAmount.Count);

            //Then checks which stance the player is in to properly activated a hitbox
            if (changeStance.currentStance == 0)
            {
                currentComboAmount.Add(comboHitboxes[0]);
                Debug.Log($"PlayerAttackManager: Activating Light Attack Hitbox 0 (Stance 0)");
                
                // Ensure hit tracking is cleared before enabling
                HitboxDamageManager dmg0 = comboHitboxes[0].GetComponent<HitboxDamageManager>();
                if (dmg0 != null) dmg0.ClearHitTracking();
                
                comboHitboxes[0].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[0]));
            }
            else
            {
                currentComboAmount.Add(comboHitboxes[1]);
                Debug.Log($"PlayerAttackManager: Activating Light Attack Hitbox 1 (Stance 1)");
                
                // Ensure hit tracking is cleared before enabling
                HitboxDamageManager dmg1 = comboHitboxes[1].GetComponent<HitboxDamageManager>();
                if (dmg1 != null) dmg1.ClearHitTracking();
                
                comboHitboxes[1].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[1]));
            }

        }
        else if (input.HeavyAttackTrigger)
        {
            lastAttackPressTime = Time.time;
            input.HeavyAttackTrigger = false;

            Debug.Log("Combo Amount: " + currentComboAmount.Count);

            if (changeStance.currentStance == 0)
            {
                currentComboAmount.Add(comboHitboxes[2]);
                Debug.Log($"PlayerAttackManager: Activating Heavy Attack Hitbox 2 (Stance 0)");
                
                // Ensure hit tracking is cleared before enabling
                HitboxDamageManager dmg2 = comboHitboxes[2].GetComponent<HitboxDamageManager>();
                if (dmg2 != null) dmg2.ClearHitTracking();
                
                comboHitboxes[2].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[2]));
            }
            else
            {
                currentComboAmount.Add(comboHitboxes[3]);
                Debug.Log($"PlayerAttackManager: Activating Heavy Attack Hitbox 3 (Stance 1)");
                
                // Ensure hit tracking is cleared before enabling
                HitboxDamageManager dmg3 = comboHitboxes[3].GetComponent<HitboxDamageManager>();
                if (dmg3 != null) dmg3.ClearHitTracking();
                
                comboHitboxes[3].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[3]));
            }
        }

        /*If the player goes over the designated combo limit then it is reset back to 0. The "- 1" is added since combos technically starts at 0, but, for QOL, whoever is editing can
          input whatever limit they like without thinking of the technical details.
        */
        if (currentComboAmount.Count > maxComboAmount - 1) 
        {
            ResetCombo();
            Debug.Log("Combo Complete!");
        }
    }

    //If the player doesn't make an input within the designated amount of time, then it is reset
    private void InactivityCheck()
    {
        if(Time.time - lastAttackPressTime > amountOfTimeBetweenAttacks)
        {
            ResetCombo();
        }
    }

    //Clears the list which essentially clears the combo counter
    private void ResetCombo()
    {
        // Debug.Log("Combo Reset");
        currentComboAmount.Clear();
    }

    //Turns off the hitbox
    private IEnumerator TurnOffHitboxes(BoxCollider box)
    {
        yield return new WaitForSeconds(hitboxActiveDuration);
        Debug.Log($"PlayerAttackManager: Deactivating hitbox {box.name} after {hitboxActiveDuration}s (was enabled: {box.enabled})");
        box.enabled = false;
        Debug.Log($"PlayerAttackManager: Hitbox {box.name} is now disabled: {!box.enabled}");
    }
    
    /// <summary>
    /// Debug method to get enemy health status near the player
    /// </summary>
    public void DebugNearbyEnemyHealth()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"PlayerAttackManager: Found {enemies.Length} enemies nearby:");
        
        foreach (GameObject enemy in enemies)
        {
            IHealthSystem healthSystem = enemy.GetComponent<IHealthSystem>();
            if (healthSystem != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                Debug.Log($"  {enemy.name}: {healthSystem.currentHP}/{healthSystem.maxHP} HP (Distance: {distance:F1}m)");
            }
            else
            {
                Debug.LogWarning($"  {enemy.name}: No IHealthSystem found!");
            }
        }
    }
    
    /// <summary>
    /// Test method to damage all nearby enemies (for testing purposes)
    /// </summary>
    [ContextMenu("Test Damage All Enemies")]
    public void TestDamageAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"PlayerAttackManager: Testing damage on {enemies.Length} enemies...");
        
        foreach (GameObject enemy in enemies)
        {
            IHealthSystem healthSystem = enemy.GetComponent<IHealthSystem>();
            if (healthSystem != null)
            {
                float testDamage = 25f;
                float beforeHP = healthSystem.currentHP;
                healthSystem.LoseHP(testDamage);
                float afterHP = healthSystem.currentHP;
                
                Debug.Log($"  {enemy.name}: {beforeHP} -> {afterHP} HP (damage: {testDamage})");
                
                // Also fire attacked trigger
                BaseEnemy<EnemyState, EnemyTrigger> baseEnemy = enemy.GetComponent<BaseEnemy<EnemyState, EnemyTrigger>>();
                if (baseEnemy != null)
                {
                    baseEnemy.TryFireTriggerByName("Attacked");
                }
            }
            else
            {
                Debug.LogWarning($"  {enemy.name}: No IHealthSystem found!");
            }
        }
    }
    
    /// <summary>
    /// Debug method to manually clear all hitbox hit tracking
    /// </summary>
    [ContextMenu("Clear All Hit Tracking")]
    public void ClearAllHitTracking()
    {
        Debug.Log("=== MANUALLY CLEARING ALL HIT TRACKING ===");
        
        for (int i = 0; i < comboHitboxes.Length; i++)
        {
            if (comboHitboxes[i] != null)
            {
                HitboxDamageManager damageManager = comboHitboxes[i].GetComponent<HitboxDamageManager>();
                if (damageManager != null)
                {
                    damageManager.ClearHitTracking();
                }
            }
        }
        Debug.Log("=== FINISHED CLEARING HIT TRACKING ===");
    }
    
    /// <summary>
    /// Debug method to test hitbox overlap detection
    /// </summary>
    [ContextMenu("Test Hitbox Overlaps")]
    public void TestHitboxOverlaps()
    {
        Debug.Log("=== HITBOX OVERLAP TEST ===");
        
        for (int i = 0; i < comboHitboxes.Length; i++)
        {
            var hitbox = comboHitboxes[i];
            if (hitbox == null) continue;
            
            Debug.Log($"Testing hitbox {i} ({hitbox.name}):");
            Debug.Log($"  Position: {hitbox.transform.position}");
            Debug.Log($"  Bounds: {hitbox.bounds.center} (size: {hitbox.bounds.size})");
            Debug.Log($"  Enabled: {hitbox.enabled}");
            Debug.Log($"  IsTrigger: {hitbox.isTrigger}");
            
            // Check what's currently overlapping
            Collider[] overlapping = Physics.OverlapBox(
                hitbox.bounds.center, 
                hitbox.bounds.extents, 
                hitbox.transform.rotation
            );
            
            Debug.Log($"  Overlapping objects: {overlapping.Length}");
            foreach (var col in overlapping)
            {
                if (col != hitbox)
                {
                    Debug.Log($"    - {col.name} (Tag: {col.tag}, Layer: {LayerMask.LayerToName(col.gameObject.layer)})");
                }
            }
        }
        Debug.Log("=== END HITBOX TEST ===");
    }

}
