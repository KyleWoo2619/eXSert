# Input Response Curve - Quick Reference

## Problem Fixed
- **Before:** Small joystick movements (0.1-0.3) barely affected blend tree (0.001-0.05)
- **Before:** Halfway joystick (0.5) already reached 0.8-0.9 on blend tree (huge jump)
- **After:** Low inputs spread out more evenly (0.2 input → ~0.3 blend tree)

---

## How It Works

### Power Curve Applied to Input
```csharp
curvedInput = Mathf.Pow(inputMagnitude, inputResponseCurve)
```

### Response Curve Values

| Curve Value | Effect | Example Mapping |
|-------------|--------|-----------------|
| **0.3** | Very sensitive low inputs | Input 0.2 → 0.62 blend |
| **0.5** | More sensitive low inputs | Input 0.2 → 0.45 blend |
| **0.6** (default) | Balanced sensitivity | Input 0.2 → 0.36 blend |
| **1.0** | Linear (no curve) | Input 0.2 → 0.20 blend |
| **1.5** | Less sensitive low inputs | Input 0.2 → 0.09 blend |
| **2.0** | Very gradual at low inputs | Input 0.2 → 0.04 blend |

---

## Example: Curve = 0.6 (Default)

| Joystick Input | Curved Value | Blend Tree (0-1) | Animation |
|----------------|--------------|------------------|-----------|
| 0.0 | 0.00 | 0.00 | Idle |
| 0.1 | 0.15 | ~0.15 | Light walk |
| 0.2 | 0.36 | ~0.36 | Walk |
| 0.3 | 0.52 | ~0.52 | Walk/Sprint blend |
| 0.5 | 0.73 | ~0.73 | Mostly sprint |
| 0.7 | 0.86 | ~0.86 | Sprint |
| 1.0 | 1.00 | 1.00 | Full sprint |

**Result:** Low inputs (0.1-0.3) now have meaningful impact on animation!

---

## Example: Curve = 1.0 (Linear - OLD BEHAVIOR)

| Joystick Input | Curved Value | Blend Tree (0-1) | Animation |
|----------------|--------------|------------------|-----------|
| 0.0 | 0.00 | 0.00 | Idle |
| 0.1 | 0.10 | ~0.10 | Barely visible |
| 0.2 | 0.20 | ~0.20 | Slow walk |
| 0.3 | 0.30 | ~0.30 | Walk |
| 0.5 | 0.50 | ~0.50 | Walk/Sprint blend |
| 0.7 | 0.70 | ~0.70 | Sprint |
| 1.0 | 1.00 | 1.00 | Full sprint |

**Problem:** 0.1-0.2 input barely moves blend tree, 0.5 already halfway to sprint

---

## Tuning Guide

### Want MORE sensitivity at low inputs? (Current problem)
**Set curve to 0.4-0.6**
- 0.4 = Very aggressive (0.2 input → 0.53 blend)
- 0.5 = Aggressive (0.2 input → 0.45 blend)
- 0.6 = Balanced (0.2 input → 0.36 blend) ← **Current default**

### Want LESS sensitivity at low inputs?
**Set curve to 1.2-2.0**
- 1.2 = Slightly gradual
- 1.5 = Gradual (good for precise aiming games)
- 2.0 = Very gradual (0.2 input → 0.04 blend)

---

## Inspector Setting

**Location:** Player → EnhancedPlayerMovement → Speed Thresholds
- **Input Response Curve**: 0.6 (range: 0.3 - 3.0)
  - Tooltip: "<1 = more sensitive at low inputs, >1 = less sensitive at low inputs"

---

## Debug Mode

Enable `showAnimationDebug` to see real-time values:
```
[AnimFacade] Input=0.25, Speed=2.30, Normalized=0.40, Grounded=True
```

**What to look for:**
- **Input**: Raw joystick magnitude (0-1)
- **Speed**: Actual movement speed after curve
- **Normalized**: Value sent to blend tree (0-1)

**Test sequence:**
1. Gently push joystick (Input ~0.2)
2. Check Normalized value - should be ~0.3-0.4 (visible walk)
3. Half push joystick (Input ~0.5)
4. Check Normalized value - should be ~0.7 (not 0.9!)

---

## Math Explanation

### Power Curve (Exponential)
```
y = x^n

When n < 1.0:
- Graph curves upward (convex)
- Small x values produce larger y values
- Example: 0.2^0.6 = 0.36 (bigger than 0.2)

When n > 1.0:
- Graph curves downward (concave)
- Small x values produce smaller y values
- Example: 0.2^1.5 = 0.09 (smaller than 0.2)

When n = 1.0:
- Linear (y = x)
- No curve applied
```

### Why This Fixes the Problem
1. **Original issue**: Linear mapping compressed low inputs
   - Input 0.2 → Speed 2.4 → Normalized 0.20 (too small!)
   
2. **With curve 0.6**: Exponential expansion of low inputs
   - Input 0.2 → Curved 0.36 → Speed 3.12 → Normalized 0.36 (visible!)
   
3. **Result**: Low joystick movements have meaningful animation response

---

## Common Values & Use Cases

| Curve | Use Case | Feel |
|-------|----------|------|
| **0.3-0.4** | Action games, fast movement | Very responsive at low inputs |
| **0.5-0.7** | General games, balanced | Good low-input response |
| **0.8-1.0** | Simulation, realistic | Natural, linear feel |
| **1.2-1.5** | Precision games, stealth | Gradual acceleration |
| **1.8-2.0** | Heavy characters, tanks | Very gradual, weighty |

---

## Before/After Comparison

### BEFORE (Linear, Curve = 1.0)
```
Joystick 10% → Blend 0.10 (barely visible)
Joystick 20% → Blend 0.20 (slow)
Joystick 30% → Blend 0.30 (walk)
Joystick 50% → Blend 0.50 (already halfway!)
Joystick 70% → Blend 0.70 (almost max)
```
**Problem:** Tight range at low inputs, jumps too fast mid-range

### AFTER (Curve = 0.6)
```
Joystick 10% → Blend 0.15 (visible walk)
Joystick 20% → Blend 0.36 (clear walk)
Joystick 30% → Blend 0.52 (walk/sprint blend)
Joystick 50% → Blend 0.73 (sprint)
Joystick 70% → Blend 0.86 (full sprint)
```
**Fixed:** More range at low inputs, smoother mid-range transition!

---

## Final Notes

- **Default 0.6 is a good starting point** - test and adjust to taste
- **Lower = more sensitive** at low inputs (0.4-0.6 recommended for your issue)
- **Higher = less sensitive** at low inputs (1.2-2.0 for gradual feel)
- Use debug mode to fine-tune the exact feel you want
- This curve applies BEFORE speed calculation, so it affects both movement speed AND blend tree
