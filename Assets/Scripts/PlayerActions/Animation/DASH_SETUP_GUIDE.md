# Dash Animation Setup Guide

## Problem
Dash was transitioning through idle before playing, making it feel slow and unresponsive.

## Solution
Make dash **instantly interrupt all states** with highest priority.

---

## Animator Controller Changes

### 1. AnyState → Dash_Forward Transition

**Current conditions you have** (from screenshot):
- `Dash` (trigger) = true
- `CanDash` (bool) = true

**IMPORTANT: Remove or modify these conditions if present:**
- ❌ Remove: `Speed < 0.01` (this causes idle snap)
- ❌ Remove: `InCombat == false` (dash should work in combat)
- ❌ Remove: `CanChain == false` (dash should interrupt attacks)

**Settings to check:**
- ✅ **Has Exit Time**: OFF (unchecked)
- ✅ **Fixed Duration**: OFF (for instant transition)
- ✅ **Transition Duration**: 0.0 seconds
- ✅ **Transition Offset**: 0.0
- ✅ **Interruption Source**: None (or Current State if you want dash to interrupt itself)
- ✅ **Ordered Interruption**: OFF

### 2. Dash_Forward → Other States

**Exiting dash** (to locomotion/idle):
- Condition: `CanDash == false` (set by cooldown)
- OR: Use animation event `LockMovementOff` at end of clip to manually control exit timing

---

## Animation Events (Optional - for precise control)

If dash animation ends too early or gets interrupted, add these events to **Dash_Forward** clip:

| Frame | Event Name | Purpose |
|-------|------------|---------|
| 0-2 | `LockMovementOn` | Prevent attacks from interrupting |
| Near end (80-90%) | `LockMovementOff` | Allow exit after animation completes |

These events tell AnimFacade to ignore other inputs during dash window.

---

## Code Changes (Already Applied)

### EnhancedPlayerMovement.cs
- ✅ Removed `LockMovementOn()` from dash start (was causing Speed=0 before transition)
- ✅ Dash trigger fires **immediately** when button pressed
- ✅ CharacterController.Move() handles dash movement directly (bypasses Speed parameter)

### AnimFacade.cs
- ✅ `RequestDash()` sets **both** Dash trigger AND CanDash bool instantly
- ✅ CanDash stays true during dash, false only during cooldown

---

## Testing Checklist

1. **Instant response**: Dash animation should start **within 1 frame** of button press
2. **No idle flash**: Should NOT see idle animation between current state and dash
3. **Full animation plays**: Dash should complete its full animation (not cut short)
4. **Works from any state**: Test dashing while:
   - Standing idle
   - Running
   - Mid-attack (should interrupt and dash)
   - In air (air dash)
5. **Cooldown works**: After dash completes, can't dash again until cooldown expires

---

## Troubleshooting

### Problem: Still seeing idle flash before dash
**Fix**: Check AnyState → Dash transition:
- Ensure "Has Exit Time" is OFF
- Ensure "Transition Duration" is 0.0
- Remove any Speed or InCombat conditions

### Problem: Dash animation ends instantly
**Fix**: 
- Check Dash_Forward → Locomotion transitions have "Has Exit Time" ON
- OR add `LockMovementOff` event at end of Dash_Forward clip

### Problem: Can't dash during attacks
**Fix**: Remove `InCombat == false` or `CanChain == false` from dash transition conditions

### Problem: Dash doesn't move character
**Fix**: Already handled in code - CharacterController.Move() bypasses Speed parameter

---

## Parameters Summary

| Parameter | Type | Purpose |
|-----------|------|---------|
| `Dash` | Trigger | Fires dash transition |
| `CanDash` | Bool | True = dash available, False = cooldown |
| `Speed` | Float | Ignored during dash (movement is code-driven) |

---

## Final Notes

- Dash has **highest priority** via AnyState with minimal conditions
- Movement during dash is **code-driven** (EnhancedPlayerMovement.DashCoroutine)
- Animator only handles **visual feedback** (playing the animation)
- CanDash bool prevents spam, NOT the transition conditions
