using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles common game actions like restarting, returning to menu, quitting.
/// Use this with ConfirmationDialog to execute these actions after confirmation.
/// Updated to work with SceneLoader and CheckpointSystem.
/// </summary>
public class GameActionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Reference to PauseManager (optional)")]
    private PauseManager pauseManager;

    private void Start()
    {
        // Try to find PauseManager if not assigned
        if (pauseManager == null)
        {
            pauseManager = PauseManager.Instance;
        }
    }

    /// <summary>
    /// Restarts the game from the last checkpoint.
    /// Uses CheckpointSystem to reload the proper scene and spawn point.
    /// </summary>
    public void RestartFromCheckpoint()
    {
        Debug.Log("🔄 [GameActionHandler] Restarting from checkpoint...");
        
        // Resume game first
        if (pauseManager != null)
        {
            pauseManager.ResumeGame();
        }
        
        // Use SceneLoader to restart from checkpoint
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.RestartFromCheckpoint();
        }
        else
        {
            // Fallback: just reload current scene
            Debug.LogWarning("[GameActionHandler] SceneLoader not found, using fallback reload");
            string currentScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentScene);
        }
    }

    /// <summary>
    /// Returns to the main menu.
    /// Properly cleans up DontDestroyOnLoad objects.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("🏠 [GameActionHandler] Returning to main menu...");
        
        // Resume time (important before loading new scene)
        Time.timeScale = 1f;
        
        // Use SceneLoader to properly clean up and load main menu
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMainMenu();
        }
        else
        {
            // Fallback: just load main menu scene
            Debug.LogWarning("[GameActionHandler] SceneLoader not found, using fallback load");
            SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// Quits the game application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("❌ [GameActionHandler] Quitting game...");
        
        #if UNITY_EDITOR
        // Stop playing in editor
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // Quit the application
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Just closes the dialog without doing anything (for cancel actions)
    /// </summary>
    public void OnDialogCanceled()
    {
        Debug.Log("↩️ [GameActionHandler] Dialog canceled");
        // Nothing to do, just logging
    }
}
