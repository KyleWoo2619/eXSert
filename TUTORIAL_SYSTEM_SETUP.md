# Tutorial System Setup Guide

## 📋 Quick Overview
The tutorial system has 4 main components:
1. **ObjectiveManager** - Tracks progress, manages text, counts player actions
2. **TutorialUI** - Updates UI text based on ObjectiveManager
3. **WaveBasedProgression** - Spawns enemy waves, monitors completion
4. **PlayerActionTracker** - Tracks player button presses

---

## 🎯 Scene Setup Instructions

### Step 1: Create ObjectiveManager GameObject
1. In your **FP_Elevator** scene, create an empty GameObject
2. Name it **"ObjectiveManager"**
3. Add the **ObjectiveManager** component
4. Assign an **objective change sound** (optional notification sound)
5. Adjust **sfxVolume** if needed (default 0.7)

### Step 2: Setup TutorialUI
1. Find your Canvas with the **Objective** and **Notice** TextMeshPro elements
2. Add the **TutorialUI** component to the Canvas (or a child GameObject)
3. Drag your **Objective TextMeshPro** to the `Objective Text` field
4. Drag your **Notice TextMeshPro** to the `Notice Text` field
5. Enable/disable fade animation as desired

### Step 3: Organize Enemy Waves
For each wave, create a parent GameObject containing the enemies:

```
Hierarchy Example:
├─ Wave1Parent (GameObject)
│  ├─ BoxerEnemy1
│  └─ BoxerEnemy2
├─ Wave2Parent (GameObject)
│  ├─ CrawlerEnemy1
│  └─ CrawlerEnemy2
├─ Wave3Parent (GameObject)
│  ├─ BoxerEnemy3
│  └─ DroneEnemy1
```

**All wave parents should start DISABLED in the hierarchy!**

### Step 4: Setup WaveBasedProgression
1. Create an empty GameObject named **"WaveManager"**
2. Add the **WaveBasedProgression** component
3. Configure the **Enemy Waves** array:
   - Set **Size** to the number of waves (e.g., 3)
   - For each wave:
     - Assign the **Wave Parent** GameObject
     - Set **Notice Text** (e.g., "Use the X Button to Light Attack!")
     - Set **Spawn Delay** (3 seconds recommended for waves after the first)

4. Configure **Objective Settings**:
   - Set **Objective Text** to "Eliminate ALL ENEMIES"

5. Configure **Progression Settings**:
   - Assign **Elevator Wall Closed** (wall blocking elevator)
   - Assign **Elevator Wall Open** (invisible/disabled wall)
   - Set **Next Scene Name** to "FP_Bridge"
   - Set **Load Scene Additively** (recommended: checked)
   - Set **Delay Before Scene Load** (2 seconds recommended)

### Step 5: Add PlayerActionTracker to Player
1. Find your **Player** GameObject in the scene
2. Add the **PlayerActionTracker** component
3. The component will automatically find **InputReader.Instance**
4. No additional setup needed!

### Step 6: Verify SoundManager Exists
- Make sure you have a **SoundManager** GameObject in the scene
- If not, use the **SoundManagerSetupHelper** menu: `Tools > Setup SoundManager`

---

## 🎮 How It Works

### Wave Flow:
1. **Wave 1 spawns immediately** when scene starts
   - Notice: "Use the X Button to Light Attack!"
   - 1-2 enemies appear
   
2. **Wave 1 cleared** → Wait 3 seconds
   - Objective change sound plays
   - Notice updates: "Use the Y Button to Heavy Attack!"
   
3. **Wave 2 spawns** → 1-2 new enemies appear
   
4. **Wave 2 cleared** → Wait 3 seconds
   - Notice updates: "Use the LB Button to Change Stance!"
   
5. **Wave 3 spawns** → 1-2 new enemies appear
   
6. **Wave 3 cleared** → All complete!
   - Elevator door opens
   - Objective: "Proceed Forward"
   - Load **FP_Bridge** scene after 2 seconds

### Player Action Tracking:
- Every time player presses **X** (Light Attack), ObjectiveManager tracks it
- Every time player presses **Y** (Heavy Attack), ObjectiveManager tracks it
- Every time player presses **LB** (Stance Change), ObjectiveManager tracks it
- You can add logic later to change notices based on action counts

---

## 🔧 Testing & Debugging

### Enable Debug Logs:
All components have extensive debug logging with emoji markers:
- 📋 Objective updates
- 💡 Notice updates
- 🌊 Wave spawning/clearing
- ⚔️ Player action tracking
- 👾 Enemy registration

### Manual Testing:
1. Play the scene
2. Check Console for:
   - "📋 [ObjectiveManager] Objective Updated: Eliminate ALL ENEMIES"
   - "🌊 [WaveBasedProgression] Starting Wave 1/3"
   - "👥 [WaveBasedProgression] Wave 1 has X enemies"
3. Defeat enemies and watch for:
   - "✅ [WaveBasedProgression] Wave 1 cleared!"
   - "⏳ [WaveBasedProgression] Waiting 3s before spawning wave..."
4. Attack and watch for:
   - "⚔️ [ObjectiveManager] Light Attack #1"
   - "⚔️ [ObjectiveManager] Heavy Attack #1"

### Common Issues:
- **No enemies spawn**: Check that wave parent GameObjects are assigned and contain enemies with IHealthSystem
- **Text doesn't update**: Verify TutorialUI has TextMeshPro references assigned
- **No sound plays**: Ensure ObjectiveManager has an AudioClip assigned + SoundManager exists
- **Actions not tracked**: Make sure PlayerActionTracker is on Player GameObject

---

## 🚀 Advanced Customization

### Change Notice Mid-Wave Based on Actions:
Add this to **WaveBasedProgression.cs** in the `MonitorCurrentWave()` coroutine:

```csharp
// Inside the while loop, add:
if (currentWaveIndex == 0 && ObjectiveManager.Instance.GetLightAttackCount() >= 3)
{
    ObjectiveManager.Instance.UpdateNotice("Good! Now try Heavy Attack (Y)!", true);
}
```

### Add More Waves:
Just increase the **Enemy Waves** array size and assign more parent GameObjects!

### Custom Wave Delays:
Each wave can have a different spawn delay - just adjust the **Spawn Delay** field per wave.

---

## 📝 Example Wave Configuration

### Wave 1 (Learn Light Attack):
- **Wave Parent**: Wave1Parent (2 BoxerEnemies)
- **Notice Text**: "Use the X Button to Light Attack!"
- **Spawn Delay**: 0

### Wave 2 (Learn Heavy Attack):
- **Wave Parent**: Wave2Parent (1 CrawlerEnemy)
- **Notice Text**: "Use the Y Button to Heavy Attack!"
- **Spawn Delay**: 3

### Wave 3 (Learn Stance Change):
- **Wave Parent**: Wave3Parent (2 BoxerEnemies)
- **Notice Text**: "Use the LB Button to Change Stance!"
- **Spawn Delay**: 3

---

That's it! Once configured, the system will automatically:
✅ Spawn waves sequentially
✅ Update notice text between waves
✅ Play notification sounds
✅ Track player actions
✅ Open elevator when complete
✅ Load next scene

Let me know if you need any adjustments! 🎉
