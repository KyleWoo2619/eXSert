# Attack System Debug Guide

## Issues Addressed

### ❌ **Problem 1**: Enemy damaged immediately when attack button pressed (no collision required)
**Root Cause**: Enemies already overlapping hitbox area when attack activates, but `OnTriggerEnter` doesn't fire for pre-existing overlaps.

**✅ Solution**: Added `CheckInitialOverlaps()` method that scans for enemies already in hitbox area when attack activates.

### ❌ **Problem 2**: Enemy only takes damage once, then no further attacks work
**Root Cause**: `hitThisActivation` HashSet not being cleared properly between attack activations.

**✅ Solution**: Enhanced `OnEnable()`/`OnDisable()` methods with proper HashSet clearing and debug logging.

### ❌ **Problem 3**: Player sometimes takes damage from own attacks
**Root Cause**: Player implements `IHealthSystem` interface, so hitbox damage system could target player.

**✅ Solution**: Added multiple safety checks:
- Only target objects with "Enemy" tag
- Block any objects with "Player" tag or root
- Additional component-level safety checks

## Updated Components

### 1. **HitboxDamageManager.cs** - Complete Rewrite
```csharp
Key Changes:
- Added CheckInitialOverlaps() for pre-existing enemy overlaps
- Enhanced safety checks to prevent player damage
- Centralized damage logic in ProcessPotentialHit()
- Improved debug logging throughout
- Only uses OnTriggerEnter (no OnTriggerStay to prevent spam)
```

### 2. **PlayerAttackManager.cs** - Enhanced Debugging
```csharp
Key Changes:
- Added detailed hitbox activation logging
- New context menu: "Test Hitbox Overlaps" for debugging
- Enhanced "Test Damage All Enemies" method
- Better validation and safety checks
```

## Testing Procedures

### **Test 1: Basic Attack Collision**
1. **Setup**: Place enemy within attack range but not touching player
2. **Action**: Press Light/Heavy Attack
3. **Expected**: 
   - Console shows: "PlayerAttackManager: Activating [Attack Type] Hitbox"
   - Console shows: "SUCCESS: [Weapon] hit [Enemy] for [X] damage"
   - Enemy health bar decreases
   - Enemy AI reacts (enters Chase state)

### **Test 2: Pre-existing Overlap**
1. **Setup**: Stand very close to enemy (overlapping hitbox area)
2. **Action**: Press Light/Heavy Attack
3. **Expected**: 
   - Console shows: "checking X overlapping colliders on activation"
   - Enemy still takes damage even though already overlapping
   - Only one damage instance per attack

### **Test 3: Multiple Attack Prevention**
1. **Setup**: Stand near enemy
2. **Action**: Press attack multiple times rapidly
3. **Expected**: 
   - Each attack press creates new hitbox activation
   - Enemy takes damage from each separate attack
   - No damage spam during single attack duration

### **Test 4: Player Safety Check**
1. **Setup**: Ensure player has IHealthSystem component
2. **Action**: Attack near player's own colliders
3. **Expected**: 
   - Console shows: "hit non-enemy object" or "blocked attempt to damage player"
   - Player health remains unchanged

### **Context Menu Tests**
Right-click PlayerAttackManager component:

#### **"Test Damage All Enemies"**
- Directly damages all enemies using IHealthSystem interface
- Bypasses collision detection
- Good for testing health system functionality

#### **"Test Hitbox Overlaps"** 
- Shows current hitbox positions and bounds
- Lists all overlapping objects
- Good for debugging collision setup

## Expected Console Output (Normal Flow)

```
PlayerAttackManager: Activating Light Attack Hitbox 0 (Stance 0)
Light Punch hitbox ENABLED - cleared hit tracking
Light Punch checking 1 overlapping colliders on activation
Light Punch processing potential hit on BoxerEnemy (Tag: Enemy, Layer: Default)
SUCCESS: Light Punch hit BoxerEnemy for 25 damage! Health: 100 -> 75 (Max: 100)
Light Punch fired 'Attacked' trigger on BoxerEnemy
BoxerEnemy: ENTERING Chase state
PlayerAttackManager: Deactivating hitbox LightAttack_Stance0
Light Punch hitbox DISABLED - cleared hit tracking
```

## Troubleshooting

### **Issue**: No damage on any attacks
**Check**:
1. Layer Collision Matrix (Player ↔ Enemy layers)
2. Enemy has "Enemy" tag
3. Hitbox has kinematic Rigidbody
4. Enemy has IHealthSystem component

**Debug Steps**:
1. Use "Test Hitbox Overlaps" - are enemies detected?
2. Use "Test Damage All Enemies" - does health system work?
3. Check console for safety blocks ("hit non-enemy object")

### **Issue**: Player takes self-damage
**Check**:
1. Player should have "Player" tag (not "Enemy")
2. Console should show "blocked attempt to damage player"
3. Remove any "Enemy" tags from player objects

### **Issue**: Damage still happens without collision
**Check**:
1. Verify hitbox bounds are reasonable (use Scene view gizmos)
2. Check "Test Hitbox Overlaps" output - what's overlapping?
3. Ensure enemies aren't positioned inside hitbox areas

### **Issue**: Only first attack works
**Check**:
1. Console should show "hitbox ENABLED/DISABLED - cleared hit tracking"
2. Verify each attack creates new hitbox activation
3. Check that HashSet is being cleared between attacks

## Physics Setup Checklist

### **Player Hitboxes**:
- ✅ BoxCollider with isTrigger = true
- ✅ Kinematic Rigidbody (auto-added by ValidateHitboxSetup)
- ✅ HitboxDamageManager component
- ✅ On "Player" layer

### **Enemy Setup**:
- ✅ "Enemy" tag
- ✅ IHealthSystem implementation (BaseEnemy or EnemyHealthManager)
- ✅ Non-trigger collider for receiving hits (auto-added by BaseEnemy)
- ✅ On "Enemy" layer

### **Layer Collision Matrix**:
- ✅ Player layer collides with Enemy layer
- ✅ Other layers configured as needed

## Advanced Debugging

### **Physics Overlap Visualization**:
```csharp
// Add this to any script for real-time overlap checking
void OnDrawGizmos() {
    if (hitboxCollider != null) {
        Gizmos.color = hitboxCollider.enabled ? Color.red : Color.gray;
        Gizmos.DrawWireCube(hitboxCollider.bounds.center, hitboxCollider.bounds.size);
    }
}
```

### **Manual Damage Test**:
```csharp
// Test IHealthSystem directly
GameObject enemy = GameObject.FindWithTag("Enemy");
IHealthSystem health = enemy.GetComponent<IHealthSystem>();
if (health != null) {
    health.LoseHP(10f);
    Debug.Log($"Enemy health: {health.currentHP}/{health.maxHP}");
}
```

This system should now handle all the edge cases and provide clear debugging information for any remaining issues.