/*
 * Attack Database
 * 
 * ScriptableObject-based database for all player attacks.
 * Stores attack data by ID (e.g., "SX1", "AY2") for easy lookup.
 * 
 * Create in Unity: Right-click in Project > Create > Combat > Attack Database
 */

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AttackDatabase", menuName = "Combat/Attack Database")]
public class AttackDatabase : ScriptableObject
{
    [System.Serializable]
    public class AttackEntry
    {
        public string attackId;           // e.g., "SX1", "AY2"
        public PlayerAttack attackData;   // Reference to PlayerAttack ScriptableObject
    }

    [SerializeField] private List<AttackEntry> attacks = new List<AttackEntry>();

    private Dictionary<string, PlayerAttack> attackLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        attackLookup = new Dictionary<string, PlayerAttack>();
        
        foreach (var entry in attacks)
        {
            if (!string.IsNullOrEmpty(entry.attackId) && entry.attackData != null)
            {
                if (attackLookup.ContainsKey(entry.attackId))
                    Debug.LogWarning($"Duplicate attack ID: {entry.attackId}");
                else
                    attackLookup[entry.attackId] = entry.attackData;
            }
        }
    }

    /// <summary>
    /// Get attack data by ID.
    /// </summary>
    public PlayerAttack GetAttack(string attackId)
    {
        if (attackLookup == null || attackLookup.Count == 0)
            BuildLookup();

        if (attackLookup.TryGetValue(attackId, out PlayerAttack attack))
            return attack;

        Debug.LogWarning($"Attack not found: {attackId}");
        return null;
    }

    /// <summary>
    /// Check if an attack exists in the database.
    /// </summary>
    public bool HasAttack(string attackId)
    {
        if (attackLookup == null || attackLookup.Count == 0)
            BuildLookup();

        return attackLookup.ContainsKey(attackId);
    }

    /// <summary>
    /// Get all attack IDs (for debugging/editor tools).
    /// </summary>
    public List<string> GetAllAttackIds()
    {
        if (attackLookup == null || attackLookup.Count == 0)
            BuildLookup();

        return new List<string>(attackLookup.Keys);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor utility: Add attack to database.
    /// </summary>
    public void AddAttack(string attackId, PlayerAttack attackData)
    {
        attacks.Add(new AttackEntry { attackId = attackId, attackData = attackData });
        BuildLookup();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Editor utility: Remove attack from database.
    /// </summary>
    public void RemoveAttack(string attackId)
    {
        attacks.RemoveAll(e => e.attackId == attackId);
        BuildLookup();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Editor utility: Validate database.
    /// </summary>
    [ContextMenu("Validate Database")]
    public void ValidateDatabase()
    {
        BuildLookup();
        
        Debug.Log($"Attack Database: {attacks.Count} entries, {attackLookup.Count} valid");
        
        // Check for missing data
        int missingCount = 0;
        foreach (var entry in attacks)
        {
            if (string.IsNullOrEmpty(entry.attackId))
            {
                Debug.LogWarning("Found entry with empty attackId");
                missingCount++;
            }
            if (entry.attackData == null)
            {
                Debug.LogWarning($"Attack {entry.attackId} has null attackData");
                missingCount++;
            }
        }
        
        if (missingCount == 0)
            Debug.Log("✓ Database validation passed!");
        else
            Debug.LogWarning($"⚠ Found {missingCount} issues in database");
    }
#endif
}
