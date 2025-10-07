# eXSert Health System Setup Guide

## Overview
We've implemented a unified health system using the `IHealthSystem` interface that works consistently across all enemies and integrates seamlessly with the player attack system.

## Key Components

### 1. IHealthSystem Interface
- **Location**: Implemented by `BaseEnemy` and `EnemyHealthManager`
- **Purpose**: Provides consistent `LoseHP()`, `HealHP()`, `currentHP`, and `maxHP` properties
- **Usage**: All damage systems use this interface for consistent health management

### 2. Updated PlayerAttackManager
- **Validates hitboxes**: Automatically adds kinematic Rigidbody components to hitboxes
- **Physics compliance**: Ensures trigger events fire properly between player hitboxes and enemies
- **Test methods**: Includes `TestDamageAllEnemies()` context menu for testing

### 3. Improved HitboxDamageManager
- **Smart targeting**: Uses `GetComponentInParent<IHealthSystem>()` to find health on enemy root or children
- **Damage prevention**: Prevents damage spam with `HashSet` tracking per activation
- **Enemy integration**: Automatically fires `"Attacked"` trigger on BaseEnemy for AI response
- **Tag-independent**: Works regardless of which collider is hit (child or parent)

### 4. BaseEnemy Enhancements
- **Built-in health**: Implements `IHealthSystem` directly with `maxHealth`/`currentHealth` fields
- **Hurtbox collider**: Automatically adds non-trigger CapsuleCollider for receiving damage
- **State integration**: `CheckHealthThreshold()` method triggers Death/LowHealth states appropriately

### 5. EnemyHealthBar Integration
- **Hybrid support**: Works with both `EnemyHealthManager` and `BaseEnemy` health systems
- **Interface-based**: Uses `IHealthSystem` for consistent health display
- **Auto-setup**: Can be initialized with either health system approach

## Setup Requirements

### For Enemies (BoxerEnemy, etc.)
1. **Health System**: Use BaseEnemy's built-in health system (implements IHealthSystem)
2. **Tag**: Ensure enemy has "Enemy" tag
3. **Colliders**: 
   - Hurtbox: Automatically added by BaseEnemy (CapsuleCollider, non-trigger)
   - Detection: SphereCollider (trigger, for detecting player)
   - Attack: BoxCollider (trigger, for enemy attacks)

### For Player Hitboxes
1. **BoxCollider**: Must be trigger (`isTrigger = true`)
2. **Rigidbody**: Automatically added by PlayerAttackManager (kinematic)
3. **HitboxDamageManager**: Required component with damage amount and weapon name
4. **Layer**: Should be on "Player" layer for collision matrix

### Layer Collision Matrix
Ensure the following layers can collide:
- **Player** layer ↔ **Enemy** layer
- This enables trigger events between player hitboxes and enemy hurtboxes

## Testing the System

### Method 1: Context Menu Test
1. Select the Player GameObject in hierarchy
2. Find PlayerAttackManager component
3. Click the context menu (⋮) → "Test Damage All Enemies"
4. Watch Console for damage logs and enemy health changes

### Method 2: Play Test
1. Enter Play mode
2. Approach an enemy (BoxerEnemy, TestingEnemy, etc.)
3. Press Light Attack (default: Left Mouse) or Heavy Attack (default: Right Mouse)
4. Watch for:
   - Hitbox activation logs: "Weapon hitbox just became ACTIVE"
   - Damage application: "WeaponName hit EnemyName for X damage"
   - Health updates in enemy health bars
   - Enemy AI state changes (should trigger "Attacked" → Chase state)

### Method 3: Debug Commands
```csharp
// Call from any script to check enemy health status
PlayerAttackManager attackManager = FindObjectOfType<PlayerAttackManager>();
if (attackManager != null)
    attackManager.DebugNearbyEnemyHealth();
```

## Expected Console Output
When attacks work correctly, you should see:
```
PlayerAttackManager: Hitbox 0 (LightAttack_Stance0) ready - Damage: 25, Weapon: Light Punch
Light Punch hitbox just became ACTIVE at position (1.2, 0.5, 2.1)
Light Punch hit BoxerEnemy for 25 damage! Health: 75/100
BoxerEnemy: ENTERING Chase state
Light Punch hitbox just became INACTIVE
```

## Troubleshooting

### No Damage Applied
1. Check Layer Collision Matrix: Player ↔ Enemy layers must collide
2. Verify Rigidbody: At least one object (hitbox or enemy) needs Rigidbody
3. Check tags: Enemy should have "Enemy" tag
4. Verify IHealthSystem: Enemy should implement the interface

### Damage Spam/Multiple Hits
- Fixed by `hitThisActivation` HashSet in HitboxDamageManager
- Each enemy can only be damaged once per hitbox activation

### Enemy Not Reacting
- Check if `TryFireTriggerByName("Attacked")` is being called
- Verify enemy state machine permits Attacked trigger from current state
- Ensure BaseEnemy component is present on enemy

### Health Bar Not Updating
- Verify EnemyHealthBar is set up with `SetEnemy(this)` in enemy Start()
- Check that slider component is assigned in EnemyHealthBar
- Ensure health bar Canvas is child of enemy GameObject

## File Modifications Summary

### Updated Files:
1. **PlayerAttackManager.cs**
   - Added `ValidateHitboxSetup()` with automatic Rigidbody setup
   - Added `TestDamageAllEnemies()` context menu test method
   - Added `DebugNearbyEnemyHealth()` for status checking

2. **HitboxDamageManager.cs**
   - Complete rewrite using `GetComponentInParent<IHealthSystem>()`
   - Added damage de-duplication system
   - Automatic Rigidbody setup in Awake()
   - Streamlined trigger collision handling

3. **BaseEnemy.cs**
   - Added hurtbox CapsuleCollider (non-trigger) for receiving damage
   - Ensured proper IHealthSystem implementation
   - Added health initialization in derived classes

4. **BoxerEnemy.cs**
   - Updated to use BaseEnemy health system directly
   - Proper health initialization (`currentHealth = maxHealth`)
   - Streamlined health bar setup

### Compatible Systems:
- **EnemyHealthBar.cs**: Already supports IHealthSystem interface
- **EnemyHealthManager.cs**: Alternative modular health component (optional)
- **BaseEnemy health**: Primary health system (recommended)

## Next Steps

1. **Test all 7 enemy types**: Ensure they all use BaseEnemy health system consistently
2. **Verify state machine integration**: Check that health changes trigger appropriate AI states
3. **Balance damage values**: Adjust damage amounts in HitboxDamageManager components
4. **Add visual feedback**: Consider particle effects or screen shake on successful hits
5. **Expand testing**: Create more comprehensive test scenarios for different combat situations

This unified system ensures that all enemies work consistently with the player attack system while maintaining modular, interface-based design principles.