# Enemy Attack System Setup Guide

## What We've Created

I've created a modular enemy health system that works with your PlayerAttackManager. Here's what was implemented:

### 1. **EnemyHealthManager.cs** - New Script
- **Location**: `Assets/Scripts/EnemyBehavior/EnemyHealthManager.cs`
- **Purpose**: Handles all health logic for enemies, separate from enemy behavior
- **Features**:
  - Implements `IHealthSystem` interface
  - Unity Events for health changes, damage taken, and death
  - Auto-triggers enemy death state when health reaches 0
  - Configurable max health and death behavior

### 2. **Updated HitboxDamageManager.cs**
- **Purpose**: Now detects enemies and damages them automatically
- **How it works**:
  - When player attack hitbox collides with anything tagged "Enemy"
  - First looks for `EnemyHealthManager` component (preferred)
  - Falls back to any `IHealthSystem` component (legacy support)
  - Applies damage using the `damageAmount` from the hitbox

### 3. **Updated EnemyHealthBar.cs**
- **Purpose**: Now supports both EnemyHealthManager and direct enemy references
- **Features**:
  - `SetEnemyHealthManager()` method for new system
  - `SetEnemy()` method for backward compatibility
  - Automatically updates UI based on either health system

### 4. **Updated BoxerEnemy.cs**
- **Purpose**: Now supports both health management approaches
- **Features**:
  - Auto-detects if EnemyHealthManager is present
  - Uses EnemyHealthManager if available, falls back to direct enemy health

### 5. **Updated BaseEnemy.cs**
- **Purpose**: Added method for external death triggering
- **New method**: `TriggerEnemyDeath()` - allows EnemyHealthManager to trigger death state

## How to Set Up Each Enemy

### For New Enemies (Recommended):

1. **Add the EnemyHealthManager component** to your enemy GameObject:
   ```csharp
   // In Inspector:
   - Max Health: 100 (or whatever you want)
   - Destroy On Death: true
   - Destroy Delay: 2f
   ```

2. **Make sure your enemy has the "Enemy" tag**

3. **Set up the health bar Canvas** (if using health bars):
   - The BoxerEnemy will auto-detect EnemyHealthManager and set up the health bar

### For Existing Enemies (Legacy Support):

- Your existing enemies will continue to work without changes
- The system will fall back to using the enemy's direct health implementation
- You can optionally add EnemyHealthManager later for additional features

## How Player Attacks Work Now

1. **Player uses PlayerAttackManager** (your existing code)
2. **Hitboxes activate** with HitboxDamageManager components
3. **When hitbox collides with "Enemy" tagged object**:
   - Finds EnemyHealthManager component
   - Calls `LoseHP(damageAmount)`
   - EnemyHealthManager handles damage and triggers death if needed
   - Health bar automatically updates
   - Enemy death state triggers if health reaches 0

## Debug Messages

The system includes helpful debug messages:
- `"[WeaponName] hit [EnemyName] with EnemyHealthManager for [X] damage"`
- `"[EnemyName] took [X] damage. Current HP: [Y]/[Z]"`
- `"[EnemyName] has died!"`

## Key Benefits

1. **Modular**: Health logic separated from enemy behavior
2. **Reusable**: Same EnemyHealthManager works for all 7 enemy types
3. **Backward Compatible**: Existing enemies still work
4. **Event-Driven**: Unity Events for health changes (can hook up particles, sounds, etc.)
5. **Automatic**: Player attacks automatically damage enemies on contact

## Next Steps

1. **Add EnemyHealthManager to your other 6 enemy types**
2. **Configure health values** for each enemy type in the Inspector
3. **Test the combat system** in Play mode
4. **Optional**: Hook up Unity Events for visual/audio feedback when enemies take damage or die

The system is ready to use! Your PlayerAttackManager will now automatically damage enemies when the hitboxes collide with them.