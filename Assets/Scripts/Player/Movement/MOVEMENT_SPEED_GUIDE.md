# Movement Speed & Acceleration Guide

## Changes Made

### 1. Minimum Movement Speed
**Problem:** Small joystick inputs could produce extremely slow movement (< 0.5 speed)
**Solution:** Added `minimumMoveSpeed` threshold - any input produces at least this speed

```csharp
[SerializeField, Range(0.5f, 3f)] private float minimumMoveSpeed = 1.5f;
```

**How it works:**
- Light joystick input → still moves at 1.5 speed minimum (visible, responsive)
- Full joystick input → scales up to maxSpeed (5-6)
- Result: Movement always feels responsive, even with gentle input

### 2. Gradual Acceleration/Deceleration
**Problem:** Speed jumped instantly from 1.5 to 6, causing jerky movement
**Solution:** Added `speedAcceleration` for smooth speed transitions

```csharp
[SerializeField, Range(1f, 20f)] private float speedAcceleration = 8f;
```

**How it works:**
- Speed gradually lerps to target value over time
- Higher value = snappier response (20 = almost instant)
- Lower value = smoother, more weighty feel (5 = gradual)
- Default: 8 (balanced feel)

### 3. Normalized Speed for Blend Tree
**Problem:** Blend tree expects 0-1 range, but was receiving raw speed values (0-6)
**Solution:** Normalize speed to 0-1 range before sending to AnimFacade

```csharp
// Map currentSpeed (minimumMoveSpeed to maxSpeed) to (0 to 1)
float normalizedSpeed = Mathf.InverseLerp(minimumMoveSpeed, maxSpeed, currentSpeedMagnitude);
animFacade.FeedMovement(normalizedSpeed, isGrounded, currentMovement.y);
```

**Result:**
- 0.0 = Walk animation (at minimumMoveSpeed)
- 0.5 = 50% blend between walk and sprint
- 1.0 = Full sprint animation (at maxSpeed)

---

## Inspector Settings

### Speed Thresholds (New Section)
| Parameter | Default | Range | Effect |
|-----------|---------|-------|--------|
| **Minimum Move Speed** | 1.5 | 0.5 - 3.0 | Baseline speed for any input |
| **Speed Acceleration** | 8.0 | 1.0 - 20.0 | How fast speed changes (higher = snappier) |

### Player Movement Settings (Existing)
| Parameter | Typical Value | Effect |
|-----------|---------------|--------|
| **Speed** | 6.0 | Unused now (kept for compatibility) |
| **Max Normal Speed** | 6.0 | Maximum sprint speed |
| **Max Guard Speed** | 2.5 | Maximum speed while guarding |
| **Friction** | 3.0 | How fast player stops when input released |

---

## How Speed Mapping Works

### Old System (Jumpy)
```
Input 0.1 → Speed 0.6 (too slow, barely visible)
Input 0.3 → Speed 1.8 (still slow)
Input 0.5 → Speed 3.0 (suddenly fast!)
Input 1.0 → Speed 6.0 (max)
```
**Issue:** Big jump between 0.3 and 0.5 input

### New System (Smooth)
```
Input 0.1 → Speed lerps to 2.1 (1.5 + 0.1*(6-1.5) = 1.95) → visible movement
Input 0.3 → Speed lerps to 2.85 (gradual increase)
Input 0.5 → Speed lerps to 3.75 (smooth transition)
Input 1.0 → Speed lerps to 6.0 (max)
```
**Benefits:**
- Minimum speed ensures visibility
- Lerp smooths out transitions
- Linear scaling from min to max

### Blend Tree Mapping
```
Speed 1.5 → Normalized 0.0 → Walk animation
Speed 3.75 → Normalized 0.5 → 50% walk/sprint blend
Speed 6.0 → Normalized 1.0 → Sprint animation
```

---

## Tuning Guide

### Make Movement More Responsive (Snappier)
1. **Increase Speed Acceleration** to 12-15
   - Speed changes faster
   - Less "weight" to movement
   - Good for fast-paced action games

2. **Increase Friction** to 5-8
   - Stops faster when input released
   - More precise control
   - Good for platformers

### Make Movement More Weighty (Smoother)
1. **Decrease Speed Acceleration** to 4-6
   - Speed changes slower
   - More momentum feeling
   - Good for realistic movement

2. **Decrease Friction** to 1-2
   - Slides more when stopping
   - More "heavy" character feel
   - Good for tank-like characters

### Adjust Walk/Sprint Feel
1. **Lower Minimum Move Speed** (1.0-1.2)
   - Slower walk animation at low input
   - More range between walk and sprint
   - Good for stealth games

2. **Raise Minimum Move Speed** (2.0-2.5)
   - Always moving at a decent pace
   - Less "creeping" feel
   - Good for action games

---

## Testing Checklist

### Basic Movement
- [ ] Light joystick input produces visible movement (not too slow)
- [ ] Full joystick input reaches max speed smoothly (no sudden jumps)
- [ ] Releasing joystick stops character at expected rate
- [ ] Character accelerates smoothly from stop to run

### Animation Blending
- [ ] Walk animation plays at low speeds
- [ ] Sprint animation plays at high speeds
- [ ] Transition between walk and sprint is smooth (no popping)
- [ ] Animation speed matches character movement speed

### Edge Cases
- [ ] Keyboard movement (digital input) still works
- [ ] Guarding reduces max speed but keeps smooth acceleration
- [ ] Turning while moving doesn't cause speed jumps
- [ ] Stopping while locked (during attacks) doesn't cause issues

---

## Debug Mode

Enable `showAnimationDebug` in Inspector to see real-time values:

```
[AnimFacade] Speed=3.45, Normalized=0.52, Grounded=True, VertSpeed=0.00, InputBusy=False
```

**What to look for:**
- **Speed**: Actual movement speed (should lerp smoothly)
- **Normalized**: Value sent to blend tree (should be 0-1)
- **Grounded**: Should be true when on ground
- **VertSpeed**: Vertical velocity (for jump/fall)

---

## Common Issues

### Movement still feels too slow
**Fix:** Increase `minimumMoveSpeed` to 2.0-2.5

### Movement feels too fast at start
**Fix:** Decrease `minimumMoveSpeed` to 1.0-1.2

### Acceleration too sudden
**Fix:** Decrease `speedAcceleration` to 5-6

### Character feels sluggish
**Fix:** Increase `speedAcceleration` to 12-15

### Walk/Sprint blend is wrong
**Check:** 
1. Normalized speed is actually 0-1 range (enable debug)
2. Blend tree threshold is set to 0 (walk) and 1 (sprint)
3. Blend parameter is "Speed"

### Character slides too much
**Fix:** Increase `friction` to 5-8

---

## Code Summary

### EnhancedPlayerMovement.cs Changes

**New Variables:**
```csharp
[SerializeField, Range(0.5f, 3f)] private float minimumMoveSpeed = 1.5f;
[SerializeField, Range(1f, 20f)] private float speedAcceleration = 8f;
private float currentSpeed = 0f; // Smoothed speed value
```

**Move() Method:**
```csharp
// Calculate target speed (minimumMoveSpeed to maxSpeed)
float targetSpeed = Mathf.Lerp(minimumMoveSpeed, maxSpeed, inputMagnitude);

// Gradually accelerate/decelerate
currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedAcceleration * Time.deltaTime);

// Move with gradual speed
Vector3 horizontalMovement = moveDirection * currentSpeed;
```

**UpdateAnimFacade() Method:**
```csharp
// Normalize speed to 0-1 for blend tree
float normalizedSpeed = Mathf.InverseLerp(minimumMoveSpeed, maxSpeed, currentSpeedMagnitude);
animFacade.FeedMovement(normalizedSpeed, isGrounded, currentMovement.y);
```

---

## Final Notes

- Movement is now **smooth and gradual** instead of instant jumps
- **Minimum speed** ensures small inputs still produce visible movement
- **Normalized speed** properly drives walk/sprint blend tree (0-1 range)
- All values **tunable in Inspector** without code changes
- Compatible with **keyboard (digital)** and **gamepad (analog)** input
