using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu controller. Handles New Game, Load Game, and Quit buttons.
/// Updated to work with new SceneLoader and CheckpointSystem.
/// </summary>
public class MainMenu : Menu
{
    [Header("Menu Navigation")]
    [SerializeField] private SaveSlotsMenu saveSlotsMenu;

    [SerializeField] private Button loadGame;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // Disable load game button if no save data exists
        if (!DataPersistenceManager.instance.HasGameData())
        {
            loadGame.interactable = false;
        }
        
        // Hook up button listeners if not already done in inspector
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameSelected);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitGameClicked);
        }
    }

    /// <summary>
    /// Called when New Game button is clicked.
    /// Opens save slot selection for new game.
    /// </summary>
    public void OnNewGameSelected()
    {
        saveSlotsMenu.ActivateMenu(false);
        this.DeactivateMenu();
    }
    
    /// <summary>
    /// Called when Load Game button is clicked.
    /// Opens save slot selection for loading existing game.
    /// </summary>
    public void OnLoadGameClicked()
    {
        saveSlotsMenu.ActivateMenu(true);
        this.DeactivateMenu();
    }

    /// <summary>
    /// Called when Quit button is clicked.
    /// Quits the application.
    /// </summary>
    public void OnQuitGameClicked()
    {
        Debug.Log("[MainMenu] Quit button clicked");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void ActivateMenu()
    {
        this.gameObject.SetActive(true);
    }

    public void DeactivateMenu()
    {
        this.gameObject.SetActive(false);
    }
}
