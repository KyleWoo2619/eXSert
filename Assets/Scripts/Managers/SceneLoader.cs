using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Singletons;
using UnityEngine.InputSystem;

/// <summary>
/// Central scene loading system for additive scene loading with persistent player.
/// Player uses DontDestroyOnLoad and persists across additive scene loads.
/// Only cleaned up when returning to main menu or restarting.
/// Written by GitHub Copilot
/// </summary>
public class SceneLoader : Singleton<SceneLoader>
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("Prefabs (optional for non-elevator scenes)")]
    [SerializeField, Tooltip("Player prefab with PlayerPersistence component for non-elevator scenes")] private GameObject playerPrefab;
    [SerializeField, Tooltip("HUD prefab with HUDPersistence component for non-elevator scenes")] private GameObject hudPrefab;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool isLoadingScene = false;

    /// <summary>
    /// Loads the main menu and cleans up all persistent objects (player, managers, etc.)
    /// </summary>
    public void LoadMainMenu()
    {
        if (isLoadingScene) return;
        
        Log("Loading Main Menu - Cleaning up persistent objects...");
        
        StartCoroutine(LoadMainMenuCoroutine());
    }

    /// <summary>
    /// Loads the first gameplay scene from main menu.
    /// This is called when starting a new game or loading a save.
    /// </summary>
    /// <param name="sceneName">The initial scene to load</param>
    public void LoadInitialGameScene(string sceneName)
    {
        if (isLoadingScene) return;
        
        Log($"Loading initial game scene: {sceneName}");
        
        StartCoroutine(LoadInitialGameSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Loads a scene additively (for doors/transitions during gameplay).
    /// Player remains persistent.
    /// </summary>
    /// <param name="sceneName">The scene to load additively</param>
    public void LoadSceneAdditive(string sceneName)
    {
        if (isLoadingScene) return;
        
        Log($"Loading scene additively: {sceneName}");
        
        StartCoroutine(LoadSceneAdditiveCoroutine(sceneName));
    }

    /// <summary>
    /// Unloads a scene (for when player leaves an area).
    /// </summary>
    /// <param name="sceneName">The scene to unload</param>
    public void UnloadScene(string sceneName)
    {
        Log($"Unloading scene: {sceneName}");
        
        StartCoroutine(UnloadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Reloads from checkpoint - destroys player and reloads the checkpoint scene.
    /// </summary>
    public void RestartFromCheckpoint()
    {
        if (isLoadingScene) return;
        
        // Get checkpoint from system
        string checkpointScene = CheckpointSystem.Instance != null 
            ? CheckpointSystem.Instance.GetCurrentSceneName() 
            : "FP_Elevator";
        
        Log($"Restarting from checkpoint: {checkpointScene}");
        
        StartCoroutine(RestartFromCheckpointCoroutine(checkpointScene));
    }

    private IEnumerator LoadMainMenuCoroutine()
    {
        isLoadingScene = true;
        
        // Resume time in case we're paused
        Time.timeScale = 1f;
        
        // Clean up all DontDestroyOnLoad objects BEFORE loading main menu
        CleanupPersistentObjects();
        
        // Unload all loaded scenes except DontDestroyOnLoad
        int sceneCount = SceneManager.sceneCount;
        for (int i = sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != "DontDestroyOnLoad" && scene.isLoaded)
            {
                Log($"Unloading scene: {scene.name}");
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
        
        // Small delay to ensure cleanup completes
        yield return null;
        
        // Resolve a valid main menu scene name (handles different project naming)
        string targetMenu = ResolveMainMenuSceneName();
        if (string.IsNullOrEmpty(targetMenu))
        {
            Debug.LogError("[SceneLoader] Could not resolve a valid Main Menu scene name. Add your menu scene to Build Settings and set 'mainMenuSceneName' on SceneLoader.");
            isLoadingScene = false;
            yield break;
        }

        Log($"Loading main menu scene: {targetMenu}");
        // Load main menu as single scene
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetMenu, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            Debug.LogError($"[SceneLoader] LoadSceneAsync returned null for '{targetMenu}'. Is it added to Build Settings?");
            isLoadingScene = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
        
        Log("Main menu loaded successfully");
        isLoadingScene = false;
    }

    private IEnumerator LoadInitialGameSceneCoroutine(string sceneName)
    {
        isLoadingScene = true;
        
        // Resume time in case we're paused
        Time.timeScale = 1f;
        
        // Load the first gameplay scene as Single (replaces main menu)
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        
        while (!loadOperation.isDone)
        {
            yield return null;
        }
        
        // Wait for scene to initialize
        yield return null;
        
        Log($"Initial game scene {sceneName} loaded");
        
        // If this is NOT an elevator scene, ensure persistent Player/HUD are spawned
        if (!IsElevatorSceneName(sceneName))
        {
            EnsurePersistentPlayerAndHUD("default");
        }

        // Ensure InputReader is bound to the active PlayerInput and using Gameplay map
        RefreshInputToCurrentPlayer();

        // Update checkpoint
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.SetCheckpoint(sceneName, "default");
        }
        
        isLoadingScene = false;
    }

    private IEnumerator LoadSceneAdditiveCoroutine(string sceneName)
    {
        isLoadingScene = true;
        
        // Check if scene is already loaded
        Scene existingScene = SceneManager.GetSceneByName(sceneName);
        if (existingScene.isLoaded)
        {
            Log($"Scene {sceneName} is already loaded");
            isLoadingScene = false;
            yield break;
        }
        
        // Load scene additively
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        
        while (!loadOperation.isDone)
        {
            yield return null;
        }
        
        Log($"Scene {sceneName} loaded additively");
        
        // Update checkpoint to new scene
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.SetCheckpoint(sceneName, "default");
        }
        
        isLoadingScene = false;
    }

    private IEnumerator UnloadSceneCoroutine(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        
        if (!scene.isLoaded)
        {
            Log($"Scene {sceneName} is not loaded, cannot unload");
            yield break;
        }
        
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneName);
        
        while (!unloadOperation.isDone)
        {
            yield return null;
        }
        
        Log($"Scene {sceneName} unloaded");
    }

    private IEnumerator RestartFromCheckpointCoroutine(string checkpointScene)
    {
        isLoadingScene = true;
        
        // Keep current pause state; we will operate using unscaled time
        bool wasPaused = Time.timeScale == 0f;

        // Always remove any persistent Player/HUD first to avoid duplicates and stale cameras
        CleanupPersistentPlayer();
        var existingHud = FindAnyObjectByType<HUDPersistence>();
        if (existingHud != null)
        {
            Log($"Destroying persistent HUD before restart: {existingHud.gameObject.name}");
            Destroy(existingHud.gameObject);
        }

        // Small realtime delay to let destroys process while paused
        yield return new WaitForEndOfFrame();

        bool isElevator = IsElevatorSceneName(checkpointScene);

        // CRITICAL: Unload the checkpoint scene first if it's already loaded
        // This ensures a true restart rather than just reactivating the existing scene
        Scene existingScene = SceneManager.GetSceneByName(checkpointScene);
        if (existingScene.isLoaded)
        {
            Log($"Unloading existing checkpoint scene before reload: {checkpointScene}");
            yield return SceneManager.UnloadSceneAsync(existingScene);
            yield return null; // Let Unity settle
        }

        // Now load the checkpoint scene additively while paused
        Log($"Additively loading checkpoint scene: {checkpointScene}");
        AsyncOperation loadAdd = SceneManager.LoadSceneAsync(checkpointScene, LoadSceneMode.Additive);
        while (loadAdd != null && !loadAdd.isDone)
        {
            yield return null;
        }

        // Set the checkpoint scene active
        var loadedScene = SceneManager.GetSceneByName(checkpointScene);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
        }
        else
        {
            Debug.LogError($"[SceneLoader] Failed to find loaded scene '{checkpointScene}' after additive load.");
        }

        // Give Unity one short realtime tick to register the new scene (helps when duplicating by name)
        yield return new WaitForSecondsRealtime(0.01f);

        // Unload all other scenes except DontDestroyOnLoad and the exact loadedScene
        int sceneCount = SceneManager.sceneCount;
        for (int i = sceneCount - 1; i >= 0; i--)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name != "DontDestroyOnLoad" && s.handle != loadedScene.handle)
            {
                Log($"Unloading scene after additive swap: {s.name}");
                yield return SceneManager.UnloadSceneAsync(s);
            }
        }

        // One more frame to settle
        yield return null;

        if (!isElevator)
        {
            // Spawn fresh persistent Player/HUD and place at saved spawn for non-elevator scenes
            string spawnId = CheckpointSystem.Instance != null ? CheckpointSystem.Instance.GetCurrentSpawnPointID() : "default";
            EnsurePersistentPlayerAndHUD(spawnId);
        }
        else
        {
            // Elevator scenes own their player; ensure no persistent stragglers exist
            RemoveAnyPersistentPlayerDuplicates();
        }

        // Rebind InputReader to the current PlayerInput and switch to Gameplay
        RefreshInputToCurrentPlayer();

        // Optionally resume gameplay after the swap (unpause)
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }
        else
        {
            Time.timeScale = 1f;
        }

        Log($"Restarted at checkpoint: {checkpointScene} (Elevator: {isElevator}, PausedWas: {wasPaused})");
        isLoadingScene = false;
    }

    /// <summary>
    /// Removes all DontDestroyOnLoad objects (player, HUD, etc.)
    /// Call this when returning to main menu.
    /// </summary>
    private void CleanupPersistentObjects()
    {
        Log("Cleaning up all persistent objects...");
        
        // 1) Destroy ALL PlayerPersistence instances (in case of duplicates)
        var players = FindObjectsByType<PlayerPersistence>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p == null) continue;
            Log($"Destroying persistent player: {p.gameObject.name}");
            Destroy(p.gameObject);
        }

        // As a fallback, also destroy anything tagged Player (covers cases without PlayerPersistence)
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (go == null) continue;
            Log($"Destroying object with Player tag: {go.name}");
            Destroy(go);
        }
        
        // 2) Destroy ALL HUDPersistence instances (HUD canvases)
        var huds = FindObjectsByType<HUDPersistence>(FindObjectsSortMode.None);
        foreach (var hud in huds)
        {
            if (hud == null) continue;
            Log($"Destroying persistent HUD: {hud.gameObject.name}");
            Destroy(hud.gameObject);
        }

        // 3) Inspect the DontDestroyOnLoad scene roots and clean any stray player/HUD roots
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name != "DontDestroyOnLoad") continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;

                // Whitelist core singletons we want to keep alive
                if (root.GetComponent<SceneLoader>() ||
                    root.GetComponent<DataPersistenceManager>() ||
                    root.GetComponent<CheckpointSystem>() ||
                    root.GetComponent<SoundManager>())
                {
                    continue;
                }

                // If a root contains PlayerPersistence or HUDPersistence, remove it
                // But do NOT remove if it also contains a whitelisted singleton somewhere in children
                bool containsPlayerOrHud = root.GetComponentInChildren<PlayerPersistence>(true) ||
                                           root.GetComponentInChildren<HUDPersistence>(true);
                bool containsCoreSingleton = root.GetComponentInChildren<SceneLoader>(true) ||
                                              root.GetComponentInChildren<DataPersistenceManager>(true) ||
                                              root.GetComponentInChildren<CheckpointSystem>(true) ||
                                              root.GetComponentInChildren<SoundManager>(true);

                if (containsPlayerOrHud && !containsCoreSingleton)
                {
                    Log($"Destroying stray persistent root: {root.name}");
                    Destroy(root);
                }
            }
        }
        
        // IMPORTANT: Don't destroy CheckpointSystem - it needs to persist!
        // The MainMenu scene should NOT have its own CheckpointSystem GameObject.
        // CheckpointSystem is created in the first gameplay scene and persists throughout.
        
        // Essential singletons stay (DataPersistenceManager, SceneLoader, CheckpointSystem, SoundManager, SettingsManager)
    }

    /// <summary>
    /// Removes only the persistent player GameObject.
    /// Call this before restarting from checkpoint.
    /// </summary>
    private void CleanupPersistentPlayer()
    {
        Log("Cleaning up persistent player for restart...");
        
        // Find and destroy the persistent player
        var persistentPlayer = FindAnyObjectByType<PlayerPersistence>();
        if (persistentPlayer != null)
        {
            Log($"Destroying player for restart: {persistentPlayer.gameObject.name}");
            Destroy(persistentPlayer.gameObject);
        }
        
        // Also destroy any GameObjects tagged Player that live in DontDestroyOnLoad scene
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name != "DontDestroyOnLoad") continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                if (root.CompareTag("Player"))
                {
                    Log($"Destroying Player-tagged persistent root: {root.name}");
                    Destroy(root);
                }
                else
                {
                    // Also check children
                    var tagged = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in tagged)
                    {
                        if (t != null && t.CompareTag("Player"))
                        {
                            Log($"Destroying Player-tagged persistent object: {t.gameObject.name}");
                            Destroy(t.gameObject);
                        }
                    }
                }
            }
        }
        
        // Also destroy persistent HUD
        var hudPersistence = FindAnyObjectByType<HUDPersistence>();
        if (hudPersistence != null)
        {
            Log($"Destroying HUD for restart: {hudPersistence.gameObject.name}");
            Destroy(hudPersistence.gameObject);
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SceneLoader] {message}");
        }
    }

    /// <summary>
    /// Returns a loadable main menu scene name. Tries the configured name first, then common fallbacks.
    /// </summary>
    private string ResolveMainMenuSceneName()
    {
        // 1) Use configured value if it can be loaded
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            return mainMenuSceneName;
        }

        // 2) Try common project-specific names
        string[] candidates = new[] { "FP_MainMenu", "MainMenu", "Menu", "Title", "TitleScreen" };
        foreach (var candidate in candidates)
        {
            if (Application.CanStreamedLevelBeLoaded(candidate))
            {
                return candidate;
            }
        }

        // 3) Nothing found
        return null;
    }

    /// <summary>
    /// Determines if a scene name should be treated as an elevator scene (scene supplies its own Player/HUD).
    /// </summary>
    private bool IsElevatorSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        string s = sceneName.ToLowerInvariant();
        if (s.Contains("elevator")) return true;
        // Project naming conventions provided: DP_, FP_, VS_
        return s.StartsWith("fp_") || s.StartsWith("dp_") || s.StartsWith("vs_");
    }

    /// <summary>
    /// Ensures persistent Player and HUD exist for non-elevator scenes, spawning prefabs if needed.
    /// </summary>
    private void EnsurePersistentPlayerAndHUD(string spawnPointId)
    {
        // Player
        var player = FindAnyObjectByType<PlayerPersistence>();
        if (player == null)
        {
            if (playerPrefab != null)
            {
                Log("Spawning persistent Player from prefab for non-elevator scene");
                var go = Instantiate(playerPrefab);
                // Place at spawn immediately since scene is already loaded
                var sp = FindSpawnPoint(spawnPointId);
                if (sp != null)
                {
                    go.transform.SetPositionAndRotation(sp.position, sp.rotation);
                }
                else
                {
                    Log($"Spawn point '{spawnPointId}' not found, using prefab position");
                }
                // PlayerPersistence on prefab will mark DontDestroyOnLoad and optionally reposition on scene load
            }
            else
            {
                Debug.LogWarning("[SceneLoader] Player prefab is not assigned, cannot spawn persistent player for non-elevator scene.");
            }
        }

        // HUD
        var hud = FindAnyObjectByType<HUDPersistence>();
        if (hud == null)
        {
            if (hudPrefab != null)
            {
                Log("Spawning persistent HUD from prefab for non-elevator scene");
                Instantiate(hudPrefab);
            }
            else
            {
                Debug.LogWarning("[SceneLoader] HUD prefab is not assigned, cannot spawn persistent HUD for non-elevator scene.");
            }
        }
    }

    /// <summary>
    /// Find a spawn point by ID via SpawnPoint component or by tag/name fallback.
    /// </summary>
    private Transform FindSpawnPoint(string spawnPointID)
    {
        // Prefer SpawnPoint component
        var points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        foreach (var sp in points)
        {
            if (sp != null && sp.spawnPointID == spawnPointID)
                return sp.transform;
        }

        // Fallback by tag
        var tagged = GameObject.FindGameObjectsWithTag("PlayerSpawn");
        if (tagged != null && tagged.Length > 0)
        {
            if (spawnPointID == "default")
                return tagged[0].transform;

            foreach (var go in tagged)
            {
                if (go != null && go.name.IndexOf(spawnPointID, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return go.transform;
            }

            return tagged[0].transform; // last resort
        }

        return null;
    }

    /// <summary>
    /// After loading an elevator scene, ensure no persistent player is lingering in DontDestroyOnLoad.
    /// If two players exist (one in active scene and one persistent), remove the persistent one.
    /// </summary>
    private void RemoveAnyPersistentPlayerDuplicates()
    {
        // Count players in active scene
        var activeScene = SceneManager.GetActiveScene();
        var scenePlayers = new System.Collections.Generic.List<GameObject>();
        var persistentPlayers = new System.Collections.Generic.List<GameObject>();

        // Collect by PlayerPersistence and by tag "Player"
        foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            bool isPlayerLike = (go.GetComponent<PlayerPersistence>() != null) || go.CompareTag("Player");
            if (!isPlayerLike) continue;

            if (go.scene == activeScene)
                scenePlayers.Add(go);
            else if (go.scene.name == "DontDestroyOnLoad")
                persistentPlayers.Add(go);
        }

        if (scenePlayers.Count > 0 && persistentPlayers.Count > 0)
        {
            foreach (var p in persistentPlayers)
            {
                Log($"Removing lingering persistent player after elevator load: {p.name}");
                Destroy(p);
            }
        }
    }

    /// <summary>
    /// Finds the current scene's PlayerInput and rebinds the global InputReader to it,
    /// then switches to the Gameplay action map so movement/pause work immediately.
    /// </summary>
    private void RefreshInputToCurrentPlayer()
    {
        var ir = InputReader.Instance;
        if (ir == null) { Debug.LogWarning("[SceneLoader] InputReader instance not found to refresh."); return; }

        // Prefer a PlayerInput on the Player-tagged object if available
        PlayerInput pi = null;
        var playerTagged = GameObject.FindGameObjectWithTag("Player");
        if (playerTagged != null) pi = playerTagged.GetComponent<PlayerInput>();
        if (pi == null) pi = Object.FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Exclude);

        if (pi == null)
        {
            Debug.LogWarning("[SceneLoader] No PlayerInput found to bind InputReader after scene load.");
            return;
        }

        ir.RebindTo(pi, switchToGameplay: true);
        if (!pi.enabled) pi.enabled = true;
    }
}
