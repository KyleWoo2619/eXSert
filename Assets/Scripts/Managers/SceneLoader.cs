using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Singletons;

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
        
        // Resume time
        Time.timeScale = 1f;
        
        // Clean up persistent objects (player, HUD, etc.)
        CleanupPersistentPlayer();
        
        // Also clean up HUD
        var hudPersistence = FindAnyObjectByType<HUDPersistence>();
        if (hudPersistence != null)
        {
            Log($"Destroying persistent HUD on restart: {hudPersistence.gameObject.name}");
            Destroy(hudPersistence.gameObject);
        }
        
        // Unload all gameplay scenes
        int sceneCount = SceneManager.sceneCount;
        for (int i = sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != "DontDestroyOnLoad" && scene.isLoaded)
            {
                Log($"Unloading scene for restart: {scene.name}");
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
        
        // Load checkpoint scene as Single
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(checkpointScene, LoadSceneMode.Single);
        
        while (!loadOperation.isDone)
        {
            yield return null;
        }
        
        Log($"Restarted at checkpoint: {checkpointScene}");
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
}
