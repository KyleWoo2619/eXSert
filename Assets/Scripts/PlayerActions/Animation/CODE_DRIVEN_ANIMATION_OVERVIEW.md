# Code-Driven Animation System - Complete Overview

## Philosophy

**Traditional Approach (Transition-Heavy):**
- Animator Controller defines complex state machines with many transitions
- Each transition has multiple conditions, exit times, blend durations
- Issues: Slow response, unexpected state conflicts, hard to debug

**Code-Driven Approach (Used Here):**
- Minimal Animator transitions (only for natural fallbacks)
- Direct animation playback via `animator.CrossFade(stateName, duration, layer)`
- Code handles state logic, Animator handles visual blending
- Result: Instant response, predictable behavior, easy to debug

---

## System Architecture

### AnimFacade.cs - Central Animation Controller

**Role:** Single source of truth for all animation state
**Pattern:** `CrossFade()` for instant playback, bypassing most Animator transitions

```csharp
// Example: Direct state playback
animator.CrossFade("Jump_Start", 0.0f, 0);  // Layer 0, instant (0 duration)
animator.CrossFade("Dash_Forward", 0.0f, 0);
animator.CrossFade("BT_Locomotion_Normal", 0.15f, 0); // 0.15s blend for smoothness
```

**Key Methods:**
- `FeedMovement()` - Called every frame; handles locomotion, jump landing, fall detection
- `RequestJump()` - Called on jump input; plays Jump_Start or AirJump_Start
- `RequestDash()` - Called on dash input; plays Dash_Forward
- `PressLight/Heavy()` - Routes to attack manager; locks movement during attacks

---

## Animation Systems (Priority Order)

### 1. Attacks (Highest Priority)
**Playback:** Hybrid (parameters + CrossFade optional)
**Entry:** Input buffering system in EnhancedPlayerAttackManager
**Lock:** `LockMovementOn()` at attack start → Speed forced to 0 → prevents locomotion
**Exit:** `LockMovementOff()` at attack end (via AnimationEvent or CloseChainWindow)
**States:** SX1-4, AX1-4, SY1-4, AY1-4

```csharp
// Attack flow:
InputX/Y parameters → Animator transitions → Attack plays
Movement locked → Speed = 0 → No locomotion interruption
CloseChainWindow → Unlock movement → Resume locomotion
```

### 2. Dash (Very High Priority)
**Playback:** Fully code-driven
**Entry:** `CrossFade("Dash_Forward", 0.0f)` on dash input
**Physics:** CharacterController.Move() in DashCoroutine (bypasses Speed parameter)
**Exit:** Dash animation completes → CanDash cooldown → returns to locomotion/idle

```csharp
// Dash flow:
OnDash() input → CrossFade("Dash_Forward", 0.0f)
DashCoroutine → CharacterController.Move() directly
Animation completes → CanDash = false (cooldown)
```

**Why code-driven:** Needed instant response without Speed checks or transition delays

### 3. Jump (Very High Priority)
**Playback:** Fully code-driven
**Entry:** `CrossFade("Jump_Start")` or `CrossFade("AirJump_Start")` on jump input
**Sequencing:** Automatic via coroutines
**Exit:** Automatic landing detection → Jump_Land → idle/locomotion

```csharp
// Jump flow:
RequestJump() → CrossFade("Jump_Start", 0.0f) or CrossFade("AirJump_Start", 0.0f)
TransitionToAirLoopAfterClip() coroutine → waits for clip 95% done
Auto-CrossFade("Jump_AirLoop") → loops while airborne
FeedMovement detects landing → CrossFade("Jump_Land", 0.05f)
Jump_Land completes → returns to idle/locomotion
```

**Special Cases:**
- **Fall off ledge:** `grounded → !grounded` detected → auto-play Jump_AirLoop
- **Landing:** `!grounded → grounded` detected → auto-play Jump_Land (if not already in it)

**Why code-driven:** Transition loops with shared triggers (Jump/DJump), unreliable landing detection

### 4. Locomotion (Medium Priority)
**Playback:** Code-driven start, Animator-driven stop
**Entry:** `CrossFade("BT_Locomotion_Normal", 0.15f)` when speed > 0.01 on ground
**Blending:** Speed parameter controls walk/sprint blend (0-1 range)
**Exit:** Natural Animator transition to idle when Speed drops to 0

```csharp
// Locomotion flow:
FeedMovement(speed > 0.01, grounded=true) → CrossFade("BT_Locomotion_Normal", 0.15f)
Speed parameter updated each frame → blend tree adjusts walk/sprint
Player stops → Speed = 0 → Animator transitions to idle naturally
```

**Why code-driven start:** Walk_Windup delay was too slow, direct entry feels responsive

### 5. Idle (Lowest Priority)
**Playback:** Animator-driven (natural fallback)
**Entry:** When Speed = 0, not in any other state
**States:** ST_Idle_WC, AOE_Idle_WC, ST_Idle_Combat, AOE_Idle_Combat

---

## Parameter Usage

### Still Used by Animator Transitions
- **Speed** (Float): Controls walk/sprint blend tree, idle transitions
- **IsGrounded** (Bool): Prevents invalid state transitions
- **VertSpeed** (Float): Optional - for future use
- **InCombat** (Bool): Switches idle/locomotion styles
- **Stance** (Int): 0=Single Target, 1=AOE, 2=Guard
- **InputX/Y, BufferedX/Y** (Float): Attack combo routing
- **CanChain** (Bool): Attack cancel windows
- **ComboStage** (Int): Tracks combo progression

### Set But Mostly Ignored by Code-Driven Systems
- **Jump/DJump** (Trigger): Set for potential fallback transitions, but code uses CrossFade
- **Dash** (Trigger): Set for potential fallback transitions, but code uses CrossFade
- **CanDash** (Bool): Set for cooldown tracking, used in transitions

---

## State Detection Pattern

All code-driven systems check current state before acting:

```csharp
var currentState = anim.GetCurrentAnimatorStateInfo(0); // Layer 0

bool inJumpState = currentState.IsName("Jump_Start") 
                || currentState.IsName("AirJump_Start") 
                || currentState.IsName("Jump_AirLoop") 
                || currentState.IsName("Jump_Land");

bool inDashState = currentState.IsName("Dash_Forward");

bool inAttackState = currentState.IsName("SX1") || currentState.IsName("SX2") 
                  || /* ... all attack states ... */;

// Use these to prevent conflicts:
if (!inJumpState && !inDashState && !inAttackState)
{
    // Safe to start locomotion
}
```

---

## Movement Lock System

**Purpose:** Prevent locomotion from interrupting attacks

```csharp
// In AnimFacade:
public void LockMovementOn()  { movementLocked = true;  }
public void LockMovementOff() { movementLocked = false; }

// In FeedMovement():
float animSpeed = (freezeLocomotionWhenLocked && movementLocked) ? 0f : speed;
anim.SetFloat(SpeedH, animSpeed); // Speed forced to 0 when locked
```

**Usage:**
- Attack animation events: `LockMovementOn` at start, `LockMovementOff` at end
- Alternative: EnhancedPlayerAttackManager can lock/unlock programmatically
- Result: Speed = 0 during attacks → no locomotion transitions → smooth attack animations

---

## Debugging Tools

### Console Logging
All code-driven systems log when playing animations:
```
[AnimFacade] Ground jump - playing Jump_Start directly
[AnimFacade] Jump_Start finished - transitioning to Jump_AirLoop
[AnimFacade] Became airborne (fall) - playing Jump_AirLoop
[AnimFacade] Landed - playing Jump_Land
[AnimFacade] Started locomotion (code-driven) - playing BT_Locomotion_Normal
[AnimFacade] Stopped locomotion (returning to idle/other state)
```

### AnimatorStateLogger.cs
Attach to player GameObject to log Animator state each frame:
- Current state name and hash
- Normalized time (0-1 = one loop)
- Active transition info

### Unity Animator Window
- **Parameters tab:** See real-time parameter values
- **Layers tab:** See current state and active transitions
- **Red recording circle:** Enable to see parameter changes in real-time during play

---

## Benefits of Code-Driven Approach

1. **Instant Response:** CrossFade(duration: 0.0f) = 0-frame playback
2. **Predictable Behavior:** No hidden transition conditions causing conflicts
3. **Easy Debugging:** Console logs show exactly when/why animations play
4. **Flexible Sequencing:** Coroutines handle automatic state progression
5. **Reduced Complexity:** Fewer Animator transitions to maintain
6. **State Awareness:** Code can check "am I already in this state?" before playing

---

## When to Use Animator Transitions vs. Code

### Use Animator Transitions For:
- ✅ Idle variations (blinking, fidgeting)
- ✅ Blend tree control (walk/sprint blending)
- ✅ Natural fallbacks (any state → idle when Speed = 0)
- ✅ Complex multi-state sequences that don't need instant response

### Use Code-Driven CrossFade For:
- ✅ Actions requiring instant response (dash, jump, dodge)
- ✅ States that need conflict detection (locomotion vs. attacks)
- ✅ Automatic sequencing (jump start → air loop → land)
- ✅ Input-driven state changes (button press → animation)
- ✅ Complex conditions that Animator can't express well

---

## Migration Path (From Transition-Heavy to Code-Driven)

1. **Identify Problem:** Animation feels sluggish, transitions conflict, hard to debug
2. **Add State Hash:** `static readonly int StateH = Animator.StringToHash("StateName");`
3. **Replace Transition Logic with Code:**
   ```csharp
   // Old: Set trigger and hope Animator transitions correctly
   animator.SetTrigger("Jump");
   
   // New: Play animation directly
   animator.CrossFade("Jump_Start", 0.0f, 0);
   ```
4. **Add State Detection:** Check if already in state before playing
5. **Add Console Logs:** Debug.Log when playing animations
6. **Remove Old Transitions:** Delete redundant AnyState transitions from Animator
7. **Test:** Verify instant response and no conflicts
8. **Keep Fallbacks:** Leave some transitions for edge cases

---

## File Reference

- **AnimFacade.cs** - Central animation controller (code-driven hub)
- **EnhancedPlayerMovement.cs** - Feeds locomotion data, handles jump/dash physics
- **EnhancedPlayerAttackManager.cs** - Handles attack inputs, locks movement
- **AnimEventRelay.cs** - Forwards AnimationEvents from Animator GO to AnimFacade
- **AnimatorStateLogger.cs** - Debug tool for logging Animator state
- **DASH_SETUP_GUIDE.md** - Dash system setup and troubleshooting
- **LOCOMOTION_SETUP_GUIDE.md** - Locomotion system setup and troubleshooting

---

## Summary

This project uses a **hybrid animation system:**
- **Code-driven** for actions requiring instant response (dash, jump, locomotion start)
- **Animator-driven** for natural blending (walk/sprint, idle variations, smooth stops)
- **Parameter-driven** for complex routing (attack combos)

Key insight: **Code handles when to play animations; Animator handles how to blend them.**
