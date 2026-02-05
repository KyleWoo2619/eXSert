# Jump System Fix Guide

## 🐛 Current Problems

### Problem 1: Landing goes Jump_Start → AirLoop → Land (weird restart)
**Cause**: AnyState → Jump_Start has condition `IsGrounded == true`, which fires when landing during Jump_AirLoop, restarting the entire jump sequence.

### Problem 2: Can jump infinitely in air
**Cause**: AnyState → AirJump_Start doesn't check the `AirJumps` parameter, so it fires even when counter is 0.

---

## ✅ Animator Transition Fixes

### 1. AnyState → Jump_Start (Ground Jump)

**Current conditions** (from screenshot):
- `Jump` trigger
- `IsGrounded == true`

**PROBLEM**: This fires when landing during an air jump, restarting the jump cycle.

**FIX - Add one of these extra conditions**:

#### Option A (Recommended): Check vertical speed
```
Jump = true
IsGrounded = true
VertSpeed Greater -0.5  ← ADD THIS (prevents firing when falling fast)
```

#### Option B: Use a state machine check
```
Jump = true
IsGrounded = true
(Remove the IsGrounded condition from AnyState entirely, only keep it in Entry → Jump_Start)
```

#### Option C: Add Interruption guards
- Set **Interruption Source** to "None" or "Next State Then Current State"
- This prevents AnyState from interrupting Jump_AirLoop → Jump_Land

---

### 2. AnyState → AirJump_Start (Air Jump)

**Current conditions** (likely):
- `Jump` trigger
- `IsGrounded == false`

**PROBLEM**: Missing check for available air jumps.

**FIX - Add this condition**:
```
Jump = true
IsGrounded = false
AirJumps Greater 0  ← ADD THIS (only allow air jump if counter > 0)
```

This ensures the transition **only fires when air jumps are available**.

---

### 3. Jump_AirLoop → Jump_Land (Landing)

**Current conditions** (from screenshot):
- `IsGrounded == true`
- `VertSpeed Less -0.01`

**STATUS**: This is correct! The problem is that AnyState is stealing priority.

**FIX**: Ensure this transition has **higher priority** than AnyState:
- In Animator window, this transition should be **above** AnyState → Jump_Start in the list
- Or adjust Interruption Source settings on AnyState transitions

---

## 🎯 Quick Fix Steps (In Order)

### Step 1: Fix AnyState → Jump_Start
1. Select the transition in Animator
2. In Conditions, add:
   - `VertSpeed` Greater `-0.5`
3. This prevents it from firing when you're falling fast (landing)

### Step 2: Fix AnyState → AirJump_Start
1. Select the transition
2. In Conditions, verify or add:
   - `AirJumps` Greater `0`
3. This ensures it only fires when air jumps are available

### Step 3: Verify Jump_AirLoop → Jump_Land
1. This transition should have **Has Exit Time OFF**
2. Should have **no exit time delay**
3. Conditions:
   - `IsGrounded` == true
   - `VertSpeed` Less `-0.01` (optional, for smooth landing detection)

---

## 🧪 Testing Sequence

After applying fixes, test this exact sequence:

1. **Ground Jump**:
   - Press jump on ground
   - Should see: Jump_Start → Jump_AirLoop
   - Console: `"Ground jump requested"`

2. **Air Jump (First)**:
   - Press jump while in air
   - Should see: Jump_AirLoop → AirJump_Start → Jump_AirLoop
   - Console: `"Air jump used, remaining: 0"`
   - **Parameters**: `AirJumps` should show `0`

3. **Try Air Jump Again (Should Fail)**:
   - Press jump while still in air
   - Should NOT transition anywhere (stay in Jump_AirLoop)
   - Console: `"No air jumps left, buffering jump for landing"`
   - **Parameters**: `AirJumps` still `0`

4. **Landing**:
   - Touch ground while in Jump_AirLoop
   - Should see: Jump_AirLoop → Jump_Land (smooth, no restart)
   - Console: `"Landed - air jumps reset to 1"`
   - **Parameters**: `AirJumps` should show `1` again

5. **Repeat**:
   - Should work correctly every cycle

---

## 📊 Expected Console Output

**Good sequence**:
```
[AnimFacade] Ground jump requested
[AnimFacade] Air jump used, remaining: 0
[AnimFacade] No air jumps left, buffering jump for landing
[AnimFacade] Landed - air jumps reset to 1
```

**Bad sequence (current)**:
```
[AnimFacade] Ground jump requested
[AnimFacade] Air jump used, remaining: 0
[AnimFacade] Air jump used, remaining: -1  ← WRONG (infinite jumps)
[AnimFacade] Ground jump requested  ← WRONG (restart on landing)
[AnimFacade] Landed - air jumps reset to 1
```

---

## 🎮 Animator Parameter Reference

Make sure these are set correctly in your Animator:

| Parameter | Type | Current Value (Play Mode) |
|-----------|------|---------------------------|
| `Jump` | Trigger | Fires then resets |
| `IsGrounded` | Bool | true when on ground, false in air |
| `AirJumps` | Int | 1 (start) → 0 (after air jump) → 1 (after landing) |
| `VertSpeed` | Float | Negative when falling, positive when rising |

---

## 🔧 Alternative: Play Jumps Directly from Code (Like Dash)

If transitions keep causing issues, you can bypass them entirely:

```csharp
// In AnimFacade.RequestJump(), replace animator triggers with direct play:
if (grounded)
{
    animator.CrossFade("Jump_Start", 0.0f, 0);
}
else if (airJumps > 0)
{
    airJumps--;
    anim.SetInteger(AirJumpsH, airJumps);
    animator.CrossFade("AirJump_Start", 0.0f, 0);
}
```

This gives you **frame-perfect jump response** (like we did for dash).

---

## 🎯 Summary: Two Changes Needed

1. **AnyState → Jump_Start**: Add `VertSpeed Greater -0.5` to prevent firing during landing
2. **AnyState → AirJump_Start**: Add `AirJumps Greater 0` to prevent infinite air jumps

That's it! These two simple condition changes will fix both issues.
