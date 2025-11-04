using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles common game actions like restarting, returning to menu, quitting.
/// Use this with ConfirmationDialog to execute these actions after confirmation.
/// </summary>
public class GameActionHandler : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField, Tooltip("Name of the main menu scene")]
    private string mainMenuSceneName = "MainMenu";

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
    /// TODO: Implement checkpoint system.
    /// </summary>
    public void RestartFromCheckpoint()
    {
        Debug.Log("🔄 [GameActionHandler] Restarting from checkpoint...");
        
        // Resume game first
        if (pauseManager != null)
        {
            pauseManager.ResumeGame();
        }
        
        // TODO: Implement actual checkpoint restart logic
        // For now, just reload the current scene
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("🏠 [GameActionHandler] Returning to main menu...");
        
        // Resume time (important before loading new scene)
        Time.timeScale = 1f;
        
        // Load main menu
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("[GameActionHandler] Main menu scene name not set!");
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
