# Scene Loading & Checkpoint System - Setup Guide

## Overview
This new system replaces the DontDestroyOnLoad approach with a cleaner scene loading system that properly manages player spawning and scene transitions.

**Your Setup: Each scene = One checkpoint**
- Elevator → 1 spawn point
- Hanger → 1 spawn point  
- Crew Quarters → 1 spawn point
- Boss → 1 spawn point

When player completes a scene, you load the next scene. That becomes the new checkpoint automatically!

## Core Components

### 1. **SceneLoader** (Singleton)
- Handles all scene transitions
- Cleans up DontDestroyOnLoad objects when returning to main menu
- Spawns player at correct spawn points

### 2. **CheckpointSystem** (Singleton)
- Tracks player's progress through the game
- Saves/loads checkpoint data to save system
- Integrates with ObjectiveManager

### 3. **PlayerSpawner** (Singleton)
- Instantiates player prefab at spawn points
- Removes old player instances before spawning
- Supports multiple spawn points per scene

### 4. **SpawnPoint** (Component)
- Marks where players should spawn
- Place on empty GameObjects in your scenes
- Has a `spawnPointID` field (default, checkpoint1, etc.)

### 5. **CheckpointTrigger** (Component)
- Automatically sets checkpoints when player enters
- Place on trigger volumes at important progress points
- Saves progress automatically


## Setup Instructions

### A. Main Menu Scene Setup

1. **Add Singleton GameObjects** (if not already present):
   - Create empty GameObject named "SceneLoader"
     - Add `SceneLoader` component
     - Set main menu scene name to "MainMenu"
   
   - Create empty GameObject named "CheckpointSystem"
     - Add `CheckpointSystem` component
     - Configure scene progression array: ["FP_Elevator", "FP_Hanger", "FP_Crew_Quarters"]

2. **Update MainMenu Buttons**:
   - Select your MainMenu GameObject
   - In MainMenu component:
     - Assign New Game Button reference
     - Assign Quit Button reference
   - The buttons will automatically hook up in Start()

3. **Your existing SaveSlotsMenu and DataPersistenceManager stay as-is**
   - They now use the new SceneLoader system


### B. Gameplay Scenes Setup (Elevator, Hanger, Crew Quarters)

1. **Add PlayerSpawner** (do this in EACH gameplay scene):
   - Create empty GameObject named "PlayerSpawner"
   - Add `PlayerSpawner` component
   - Assign your Player Prefab reference
   - Configure spawn point tag (default: "PlayerSpawn")

2. **Add Spawn Point** (ONE per scene - each scene is a checkpoint):
   - Create empty GameObject named "SpawnPoint"
   - Add `SpawnPoint` component
   - Set `spawnPointID` to **"default"**
   - Tag it with "PlayerSpawn" (for fallback support)
   - Position it where you want the player to spawn in this scene

   Example structure for each scene:
   ```
   Scene: FP_Elevator
     - SpawnPoint (SpawnPoint component, ID: "default")
   
   Scene: FP_Hanger
     - SpawnPoint (SpawnPoint component, ID: "default")
   
   Scene: FP_Crew_Quarters
     - SpawnPoint (SpawnPoint component, ID: "default")
   ```

3. **Checkpoint System** (automatic - no triggers needed):
   - Each scene IS a checkpoint
   - When a scene loads, CheckpointSystem automatically saves it as the checkpoint
   - Player will restart from the beginning of whatever scene they're in
   - No need for CheckpointTrigger components

4. **Add Singletons** (these will persist across scenes):
   - In the FIRST scene (Elevator), create:
     - SceneLoader GameObject → SceneLoader component
     - CheckpointSystem GameObject → CheckpointSystem component
     - PlayerSpawner GameObject → PlayerSpawner component
   
   - These will automatically persist across scene loads
   - You DON'T need to add them to other scenes

5. **Scene Progression Setup**:
   - Each scene acts as its own checkpoint
   - When you complete a scene and load the next one, use:
     ```csharp
     SceneLoader.Instance.LoadGameScene("FP_Hanger", "default");
     ```
   - The CheckpointSystem will automatically save the new scene as the checkpoint


### C. Remove Old DontDestroyOnLoad Player

1. **In your Player Prefab**:
   - Find `PlayerPersistence` component
   - Either:
     - Remove the component entirely, OR
     - Disable it (uncheck the box)
   
2. **This is important!** The new system spawns fresh player instances per scene, so we don't want DontDestroyOnLoad anymore in gameplay scenes.


### D. Update Existing Scripts (if needed)

1. **PauseManager** - Already updated ✅
   - Uses SceneLoader.LoadMainMenu() when returning to menu

2. **GameActionHandler** - Already updated ✅
   - RestartFromCheckpoint() now uses CheckpointSystem
   - ReturnToMainMenu() uses SceneLoader

3. **SaveDataManager** - No changes needed
   - CheckpointSystem automatically saves to GameData


## How It Works

### Starting New Game:
1. Player clicks "New Game" in main menu
2. Selects save slot
3. SaveSlotsMenu calls SceneLoader.LoadGameScene("FP_Elevator", "default")
4. SceneLoader cleans up any DontDestroyOnLoad objects
5. Loads Elevator scene
6. PlayerSpawner spawns player at "default" spawn point
7. CheckpointSystem saves "FP_Elevator" as current checkpoint

### Loading Existing Game:
1. Player clicks "Load Game" in main menu
2. Selects save slot with existing data
3. DataPersistenceManager loads GameData
4. CheckpointSystem reads currentSceneName from save (e.g., "FP_Hanger")
5. SceneLoader loads the saved scene
6. PlayerSpawner spawns player at "default" spawn point

### Progressing to Next Scene:
1. Player completes objective (e.g., all waves in Elevator)
2. Your progression script calls:
   ```csharp
   SceneLoader.Instance.LoadGameScene("FP_Hanger", "default");
   ```
3. SceneLoader loads new scene
4. PlayerSpawner spawns player at Hanger's spawn point
5. CheckpointSystem automatically saves "FP_Hanger" as new checkpoint

### Restarting from Checkpoint:
1. Player dies or pauses and clicks "Restart from Checkpoint"
2. GameActionHandler calls SceneLoader.ReloadCurrentScene()
3. SceneLoader reloads current scene (e.g., "FP_Hanger")
4. Cleans up old player
5. PlayerSpawner spawns player at beginning of that scene
6. Player restarts the entire scene from the beginning

### Returning to Main Menu:
1. Player pauses and clicks "Return to Main Menu"
2. GameActionHandler calls SceneLoader.LoadMainMenu()
3. SceneLoader destroys ALL DontDestroyOnLoad objects (player, HUD, etc.)
4. EXCEPT essential singletons (DataPersistenceManager, SceneLoader, SoundManager, SettingsManager)
5. Loads main menu scene clean


## Testing Checklist

- [ ] Main menu New Game button works
- [ ] Main menu Load Game button works (when save exists)
- [ ] Main menu Quit button works
- [ ] New game starts at Elevator with player spawned at SpawnPoint
- [ ] Player spawns at correct location (SpawnPoint position)
- [ ] Completing Elevator and loading Hanger works
- [ ] Pause menu "Restart from Checkpoint" restarts current scene
- [ ] Pause menu "Return to Main Menu" removes player from scene
- [ ] Loading existing save spawns player in saved scene (e.g., Hanger if that's where they were)
- [ ] Player doesn't persist when returning to main menu
- [ ] Settings/singletons DO persist across scenes
- [ ] Each scene (Elevator, Hanger, Crew Quarters, Boss) has one SpawnPoint


## Debug Tools

### CheckpointSystem Editor Commands:
- Right-click CheckpointSystem in inspector
- "Reset Progress (Editor Only)" - Reset to beginning
- "Print Current Checkpoint" - Shows current save state

### Scene Hierarchy Debugging:
During play mode, look for:
- "DontDestroyOnLoad" scene in hierarchy
- Should contain: DataPersistenceManager, SceneLoader, CheckpointSystem, PlayerSpawner, SoundManager, SettingsManager
- Should NOT contain: Player, HUD (after returning to main menu)


## Common Issues & Solutions

### Issue: Player doesn't spawn
- Check PlayerSpawner has player prefab assigned
- Check SpawnPoint exists with spawnPointID = "default"
- Check SpawnPoint is tagged "PlayerSpawn"
- Check console for PlayerSpawner debug logs

### Issue: Player persists in main menu
- Make sure PlayerPersistence component is removed/disabled from prefab
- Check SceneLoader is properly cleaning up (enable debug logs)

### Issue: Checkpoint doesn't save scene progress
- Check CheckpointSystem is in the first scene (Elevator)
- Check DataPersistenceManager exists
- Progress is saved automatically when scene loads
- Look for "Checkpoint saved to save data" in console

### Issue: Load game spawns at wrong scene
- Check GameData has currentSceneName saved
- Use "Print Current Checkpoint" debug command on CheckpointSystem
- Make sure you progressed to that scene and it was saved

### Issue: Settings lost between scenes
- SettingsManager should be in DontDestroyOnLoad
- SceneLoader preserves SettingsManager during cleanup
- Don't have multiple SettingsManager instances


## Integration with Scene Progression

When you complete a scene (e.g., all waves cleared in Elevator), load the next scene:

```csharp
// In your WaveBasedProgression or ElevatorProgression script:
void OnAllWavesComplete()
{
    Debug.Log("All waves complete! Moving to next scene...");
    
    // Load the next scene
    if (SceneLoader.Instance != null)
    {
        SceneLoader.Instance.LoadGameScene("FP_Hanger", "default");
    }
}
```

The checkpoint will automatically be saved when the new scene loads. No need for CheckpointTriggers!


## Future Enhancements

Ideas for expansion:
- Additive scene loading for larger levels
- Auto-save on scene transitions
- Checkpoint menu showing all unlocked checkpoints
- Visual checkpoint activation effects
- Checkpoint names/descriptions for UI
- Checkpoint-based level select


## Questions?

If you encounter issues:
1. Enable debug logs on all components (showDebugLogs = true)
2. Check Unity console for detailed messages
3. Verify all singleton instances exist in DontDestroyOnLoad scene
4. Make sure player prefab has no PlayerPersistence component active
