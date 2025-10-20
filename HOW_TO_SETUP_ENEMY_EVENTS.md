# How to Set Up EnemyHealthManager Unity Events

## What Are Unity Events?

Unity Events are visual connections you make in the Inspector to trigger actions when something happens. Think of them like "when X happens, do Y."

## Setting Up Your Enemy Health Events

### 1. **On Death Event** 
**What to put here:** Actions that happen when the enemy dies
- **Particle effects** (explosion, death particles)
- **Sound effects** (death sound, scream)
- **Animation triggers** (death animation, ragdoll activation)
- **Game logic** (drop items, update score, spawn loot)

**Example Setup:**
```
+ On Death (UnityEvent)
  - GameObject: [ParticleSystem]    Function: ParticleSystem.Play()
  - GameObject: [AudioSource]      Function: AudioSource.PlayOneShot(AudioClip)
  - GameObject: [Animator]         Function: Animator.SetTrigger(string) → "Die"
```

### 2. **On Health Changed (Single)** 
**What to put here:** Actions that happen whenever health changes (takes damage OR heals)
- **UI updates** (health bar color changes)
- **Visual feedback** (screen flash, damage indicator)
- **Conditional effects** (low health warning sounds)

**Parameters:** This passes a float (0.0 to 1.0) representing health percentage
- 1.0 = full health
- 0.5 = half health  
- 0.0 = no health

**Example Setup:**
```
+ On Health Changed (UnityEvent<Single>)
  - GameObject: [Image]             Function: Image.fillAmount(Single) 
  - GameObject: [HealthBarScript]   Function: YourScript.UpdateHealthColor(Single)
```

### 3. **On Take Damage Event**
**What to put here:** Actions that happen ONLY when taking damage (not healing)
- **Damage effects** (hit spark, blood splatter)
- **Hit sounds** (grunt, impact sound)
- **Screen shake** (camera shake on hit)
- **Hit reaction animations** (flinch, stagger)

**Example Setup:**
```
+ On Take Damage (UnityEvent)
  - GameObject: [ParticleSystem]    Function: ParticleSystem.Play() → Hit Spark Effect
  - GameObject: [AudioSource]      Function: AudioSource.Play() → Hit Sound
  - GameObject: [CameraShake]      Function: YourScript.Shake()
```

## Step-by-Step Inspector Setup

1. **Select your enemy GameObject** with EnemyHealthManager
2. **Look at the EnemyHealthManager component** in Inspector
3. **Click the "+" button** on each event you want to use
4. **Drag the GameObject** that has the component you want to trigger
5. **Select the function** from the dropdown (No Function → ComponentName → FunctionName)

## Common Mistakes to Avoid

❌ **Don't put the same effect in multiple events**
- Put hit effects in "On Take Damage" only
- Put death effects in "On Death" only

❌ **Don't forget to assign the AudioClip/Particle/etc.**
- If you call AudioSource.Play() but no clip is assigned, nothing happens

✅ **Do test each event separately**
- Use the "Test Enemy Death" button in Inspector to test death effects
- Damage the enemy in play mode to test damage effects

## Why This System is Better

✅ **Visual Setup:** No coding required - drag and drop in Inspector
✅ **Reusable:** Copy the same setup to all 7 enemy types
✅ **Flexible:** Easy to add/remove effects without touching code
✅ **Organized:** Clear separation between damage effects and death effects

## Quick Setup for Your BoxerEnemy

1. **Add these components to your BoxerEnemy:**
   - ParticleSystem (for hit/death effects)
   - AudioSource (for sounds)
   - Keep your existing Animator

2. **In EnemyHealthManager Events:**
   - **On Take Damage:** → ParticleSystem.Play() (hit spark)
   - **On Death:** → ParticleSystem.Play() (explosion), AudioSource.PlayOneShot(deathSound)
   - **On Health Changed:** → (optional) any UI feedback you want

3. **Test:** Hit the enemy and watch the effects trigger automatically!