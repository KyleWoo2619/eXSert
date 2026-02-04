# Locomotion Animation Setup Guide

## Problem
Walk_Windup state was causing slow startup and didn't connect directly to BT_Locomotion_Normal, making movement feel sluggish.

## Solution
Make locomotion **code-driven** like dash and jump - play BT_Locomotion_Normal directly when player starts moving.

---

## How It Works

### Code-Driven Locomotion (AnimFacade.cs)

```csharp
// Locomotion plays when:
bool shouldBeInLocomotion = isGrounded && speed > 0.01f && !movementLocked;

// Prevented when in:
- Jump states (Jump_Start, AirJump_Start, Jump_AirLoop, Jump_Land)
- Dash state (Dash_Forward)
- Attack states (SX1-4, AX1-4, SY1-4, AY1-4)
```

**Automatic playback:**
- Player starts moving (speed > 0.01) on ground → `CrossFade("BT_Locomotion_Normal", 0.15f)`
- Player stops moving (speed ≤ 0.01) → naturally transitions to idle via Speed parameter
- Jump/dash/attack → locomotion paused, resumes after action completes

---

## Animator Controller Changes

### Optional: Disable Walk_Windup

Since locomotion is now code-driven, you can:

1. **Option A: Delete Walk_Windup state** (recommended)
   - Remove Walk_Windup state entirely
   - Remove all transitions involving Walk_Windup
   - BT_Locomotion_Normal will be played directly from code

2. **Option B: Leave Walk_Windup but never use it**
   - Keep it for potential future use
   - Code will bypass it and play BT_Locomotion_Normal directly
   - Walk_Windup transitions will never fire

### Keep These Transitions (Still Used)

Keep any transitions **from** BT_Locomotion_Normal to other states:
- BT_Locomotion_Normal → ST_Idle_WC (when Speed drops to 0)
- BT_Locomotion_Normal → Jump_Start (optional backup)
- BT_Locomotion_Normal → Attack states (optional backup)

Code handles starting locomotion; Animator handles stopping it naturally.

---

## Blend Tree Setup (BT_Locomotion_Normal)

**Current setup (from screenshot):**
- Blend parameter: `Speed`
- Range: 0 to 1
- Animations:
  - **0.0**: p_Walk (walking animation)
  - **1.0**: p_Sprint (sprinting animation)

**How it works:**
- Speed = 0.5 → 50% blend between walk and sprint
- Speed = 1.0 → full sprint
- CharacterController speed is normalized to 0-1 range in EnhancedPlayerMovement

---

## Code Summary

### AnimFacade.FeedMovement()
```csharp
// Detects when to start locomotion
if (shouldBeInLocomotion && !isInLocomotion && !inJumpState && !inDashState && !inAttackState)
{
    anim.CrossFade("BT_Locomotion_Normal", 0.15f, 0);
    isInLocomotion = true;
    Debug.Log("[AnimFacade] Started locomotion (code-driven)");
}

// Stops tracking when player stops or enters other state
else if (isInLocomotion && (!shouldBeInLocomotion || inJumpState || inDashState || inAttackState))
{
    isInLocomotion = false;
    // Animator naturally transitions to idle when Speed drops
}
```

---

## Testing Checklist

1. **Instant response**: Movement animation should start **immediately** when moving (no Walk_Windup delay)
2. **Smooth blending**: Walk → Sprint should blend smoothly as speed increases
3. **Natural stopping**: Should smoothly transition to idle when stopping (not code-forced)
4. **Jump priority**: Jumping should interrupt locomotion instantly
5. **Dash priority**: Dashing should interrupt locomotion instantly
6. **Attack priority**: Attacks should interrupt locomotion (movement locked during attacks)
7. **Resume after actions**: Locomotion should resume automatically when continuing to move after jump/dash/attack

---

## Troubleshooting

### Problem: Still seeing Walk_Windup delay
**Fix**: 
- Check console for "[AnimFacade] Started locomotion" message
- Ensure Walk_Windup → BT_Locomotion_Normal transition doesn't have higher priority
- Consider deleting Walk_Windup state entirely

### Problem: Animation stutters when starting to move
**Fix**: 
- Increase CrossFade duration: Change `0.15f` to `0.25f` in AnimFacade.cs line ~176
- Or decrease it to `0.05f` for snappier response

### Problem: Walk/Sprint blend is wrong (always walking or always sprinting)
**Fix**: 
- Check Speed parameter is being set correctly in FeedMovement
- Verify blend tree threshold values match your speed ranges
- Check EnhancedPlayerMovement is normalizing speed to 0-1 range

### Problem: Can't stop moving (animation keeps playing)
**Fix**: 
- Ensure Speed parameter drops to 0 when not moving
- Check BT_Locomotion_Normal → Idle transitions have proper conditions (Speed < 0.01)

### Problem: Locomotion plays during attacks
**Fix**: 
- Verify `freezeLocomotionWhenLocked` is enabled in AnimFacade
- Check LockMovementOn/Off events are properly set in attack animations
- Verify attack state name matches list in FeedMovement (line ~168-170)

---

## Parameters Used

| Parameter | Type | Purpose |
|-----------|------|---------|
| `Speed` | Float | Controls walk/sprint blend (0 = walk, 1 = sprint) |
| `IsGrounded` | Bool | Prevents locomotion in air |

**Note:** Code checks these parameters but doesn't rely on transitions. Locomotion starts/stops via `CrossFade()` calls.

---

## Final Notes

- Locomotion has **medium priority**: lower than jumps/dash/attacks, higher than idle
- Starting movement is **code-driven** (CrossFade)
- Stopping movement is **Animator-driven** (natural transition when Speed drops)
- Speed parameter still controls walk/sprint blending in the blend tree
- Walk_Windup is now **obsolete** with this setup (can be deleted)
