# Two-Speed Walk/Run System - Setup Guide

## What Changed

### OLD SYSTEM (Smooth Blend)
- Blend tree continuously blended between walk and sprint (0.0 to 1.0)
- Movement speed scaled with input (1.5 to 6.0)
- **Problem:** Animation speed didn't match movement speed → sliding/skating

### NEW SYSTEM (Two Speeds)
- Only TWO states: Walk (0) or Run (1)
- Movement speed LOCKED: Walk = 2.5, Run = 5.0
- Input threshold determines which state (default: 0.5 = halfway)
- **Result:** Animation speed ALWAYS matches movement speed → no sliding!

---

## How It Works

### Input Threshold System
```
Joystick Input < 0.5 → WALK mode (speed locked at 2.5)
Joystick Input ≥ 0.5 → RUN mode (speed locked at 5.0)
```

### Blend Tree Values Sent
```
Speed parameter = 0.0 → Walk animation plays
Speed parameter = 1.0 → Run animation plays
```

No in-between values = no blending = perfect animation/movement sync!

---

## Inspector Settings

### Player Movement Settings
| Parameter | Default | Purpose |
|-----------|---------|---------|
| **Walk Speed** | 2.5 | Locked movement speed for walk (match to animation!) |
| **Run Speed** | 5.0 | Locked movement speed for run (match to animation!) |
| **Walk To Run Threshold** | 0.5 | Input threshold (0-1) to switch modes |
| **Max Guard Speed** | 2.5 | Speed when guarding |

### Speed Transitions
| Parameter | Default | Purpose |
|-----------|---------|---------|
| **Speed Transition Speed** | 15 | How fast it snaps between walk/run (higher = snappier) |

---

## Animator Setup Required

### Blend Tree Changes

Your blend tree needs to be set up with TWO discrete threshold values:

**BT_Locomotion_Normal blend tree:**
1. **Threshold 0:** Walk animation (p_Walk)
2. **Threshold 1:** Run/Sprint animation (p_Sprint)

**Remove any intermediate thresholds!** The system only sends 0 or 1.

### Example Setup
```
Parameter: Speed (Float)
Blend Type: 1D

Motion          | Threshold
----------------|----------
p_Walk          | 0.0
p_Sprint        | 1.0
```

---

## Matching Animation Speed to Movement Speed

### Critical: Speed Values MUST Match!

To eliminate sliding, your animation speed in Unity must match the movement speed:

1. **Select p_Walk animation** in Project
2. Check **Inspector → Average Velocity**
   - Should be ~2.5 units/sec
3. **Select p_Sprint animation**
   - Should be ~5.0 units/sec

### If Speeds Don't Match

**Option A: Adjust Animation Speed Multiplier**
1. Select animation
2. Animations tab → Speed multiplier
3. Adjust until velocity matches movement speed

**Option B: Adjust Movement Speed in Code**
1. Test in-game with debug enabled
2. Note the animation's actual speed
3. Adjust `walkSpeed` and `runSpeed` to match

---

## Tuning Guide

### Walk Speed Feels Too Fast
- Lower `walkSpeed` to 2.0 or 1.8
- **Also reduce p_Walk animation speed multiplier to match!**

### Run Speed Feels Too Slow
- Increase `runSpeed` to 6.0 or 7.0
- **Also increase p_Sprint animation speed multiplier to match!**

### Threshold Too Sensitive (Runs Too Early)
- Increase `walkToRunThreshold` to 0.6 or 0.7
- Now requires more joystick push to run

### Threshold Not Sensitive Enough (Hard to Run)
- Decrease `walkToRunThreshold` to 0.4 or 0.3
- Easier to trigger run mode

### Transition Too Abrupt
- Increase `speedTransitionSpeed` to 20-25 for instant snap
- Decrease to 10-12 for slightly smoother transition
- **Note:** This only affects speed lerp, not animation switching

---

## Debug Mode

Enable `showAnimationDebug` to verify values:
```
[AnimFacade] Input=0.35, Speed=2.50, AnimSpeed=0 (WALK), Grounded=True
[AnimFacade] Input=0.65, Speed=5.00, AnimSpeed=1 (RUN), Grounded=True
```

**What to look for:**
- **Input < 0.5** → Speed should be ~2.5, AnimSpeed = 0 (WALK)
- **Input ≥ 0.5** → Speed should be ~5.0, AnimSpeed = 1 (RUN)
- **Speed value** should match walkSpeed or runSpeed exactly when stable
- **AnimSpeed** should only be 0 or 1 (never 0.5, 0.7, etc.)

---

## Testing Checklist

### Basic Functionality
- [ ] Light joystick input → walks at 2.5 speed
- [ ] Push joystick past halfway → switches to run at 5.0 speed
- [ ] Release joystick → decelerates smoothly to stop
- [ ] Walk animation plays when walking (no sliding)
- [ ] Run animation plays when running (no sliding)

### No Sliding/Skating
- [ ] Feet don't slide during walk animation
- [ ] Feet don't slide during run animation
- [ ] Transition between walk/run is clean (quick speed change)
- [ ] Character stops without sliding

### Threshold Behavior
- [ ] Walk mode consistent below threshold
- [ ] Run mode consistent above threshold
- [ ] Crossing threshold changes mode instantly
- [ ] Debug shows correct mode (WALK/RUN)

---

## Common Issues

### Still Seeing Sliding
**Cause:** Animation speed doesn't match movement speed
**Fix:** 
1. Enable debug mode
2. Note actual movement speed (Speed=X.XX)
3. Check animation Average Velocity in Inspector
4. Adjust animation Speed Multiplier OR movement speed values to match

### Transition Feels Weird
**Cause:** Speed snaps too fast or too slow
**Fix:** Adjust `speedTransitionSpeed`
- Too jarring? Lower to 10-12
- Too slow? Increase to 20-25

### Always Running (Never Walks)
**Cause:** Input threshold too low or input magnitude calculation issue
**Fix:**
1. Check debug: Input value should be < 0.5 for walk
2. Increase `walkToRunThreshold` to 0.6-0.7
3. Verify controller deadzone settings

### Always Walking (Never Runs)
**Cause:** Input threshold too high or not reaching threshold
**Fix:**
1. Check debug: Input value should reach ≥ 0.5 for run
2. Decrease `walkToRunThreshold` to 0.3-0.4
3. Test with keyboard (should be 1.0 input)

---

## Advanced: Three-Speed System

If you want Walk / Jog / Sprint:

1. Add `jogSpeed` variable (e.g., 3.5)
2. Add `walkToJogThreshold` (e.g., 0.35)
3. Add `jogToRunThreshold` (e.g., 0.7)
4. Modify Move() to check multiple thresholds
5. Send 0 (walk), 0.5 (jog), or 1.0 (run) to blend tree
6. Update blend tree with three thresholds

---

## Comparison to Old System

### OLD (Smooth Blend)
```
Input 0.2 → Speed 2.4 → AnimSpeed 0.30 → Walk animation at 30% speed
Input 0.5 → Speed 4.0 → AnimSpeed 0.65 → Blended walk/run
Input 0.8 → Speed 5.2 → AnimSpeed 0.85 → Mostly run animation
```
**Result:** Animation constantly blending, speed constantly changing, sliding common

### NEW (Two-Speed)
```
Input 0.2 → Speed 2.5 → AnimSpeed 0.0 → Walk animation at 100% speed
Input 0.5 → Speed 5.0 → AnimSpeed 1.0 → Run animation at 100% speed
Input 0.8 → Speed 5.0 → AnimSpeed 1.0 → Run animation at 100% speed
```
**Result:** Animation always at 100%, speed locked, no sliding!

---

## Final Notes

- **Speed values are LOCKED** - no more gradual acceleration based on input
- **Threshold creates HARD CUT** - walk OR run, not blend
- **Animation must match movement** - adjust one or the other to sync
- This system prioritizes **visual quality** (no sliding) over **granular control**
- Perfect for games where animation matching is critical (action, fighting, etc.)
