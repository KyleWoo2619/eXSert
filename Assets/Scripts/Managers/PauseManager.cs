using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : Singletons.Singleton<PauseManager>
{
    [Header("UI GameObjects")]
    [SerializeField] private GameObject pauseMenuHolder;
    [SerializeField] private GameObject navigationMenuHolder;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _pauseActionReference;
    [SerializeField] private InputActionReference _navigationMenuActionReference;
    [SerializeField] private InputActionReference _swapMenuActionReference;
    [SerializeField] private InputActionReference _backActionReference; // UI/Back button

    public static bool IsPaused { get; private set; } = false;
    
    private enum ActiveMenu
    {
        None,
        PauseMenu,
        NavigationMenu
    }
    
    private ActiveMenu currentActiveMenu = ActiveMenu.None;

    private void OnEnable()
    {
        // Pause action
        if (_pauseActionReference == null || _pauseActionReference.action == null)
            Debug.LogWarning($"Pause Input Action Reference is not set in the inspector. Keyboard/Controller Input won't pause the game properly");
        else
            _pauseActionReference.action.performed += OnPause;

        // Navigation Menu action
        if (_navigationMenuActionReference == null || _navigationMenuActionReference.action == null)
            Debug.LogWarning($"Navigation Menu Input Action Reference is not set in the inspector. Keyboard/Controller Input won't open navigation menu properly");
        else
            _navigationMenuActionReference.action.performed += OnNavigationMenu;

        // Swap Menu action
        if (_swapMenuActionReference == null || _swapMenuActionReference.action == null)
            Debug.LogWarning($"Swap Menu Input Action Reference is not set in the inspector. UI swapping won't work properly");
        else
            _swapMenuActionReference.action.performed += OnSwapMenu;

        // Back action (UI/Back button)
        if (_backActionReference == null || _backActionReference.action == null)
            Debug.LogWarning($"Back Input Action Reference is not set in the inspector. UI/Back won't work properly");
        else
            _backActionReference.action.performed += OnBackButton;
    }

    private void OnDisable()
    {
        if (_pauseActionReference != null && _pauseActionReference.action != null)
            _pauseActionReference.action.performed -= OnPause;

        if (_navigationMenuActionReference != null && _navigationMenuActionReference.action != null)
            _navigationMenuActionReference.action.performed -= OnNavigationMenu;

        if (_swapMenuActionReference != null && _swapMenuActionReference.action != null)
            _swapMenuActionReference.action.performed -= OnSwapMenu;

        if (_backActionReference != null && _backActionReference.action != null)
            _backActionReference.action.performed -= OnBackButton;
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        Debug.Log($"[PauseManager] OnPause called - Current menu: {currentActiveMenu}, IsPaused: {IsPaused}");
        
        if (currentActiveMenu == ActiveMenu.None)
        {
            // Open pause menu
            ShowPauseMenu();
        }
        else if (currentActiveMenu == ActiveMenu.PauseMenu)
        {
            // Close pause menu and resume game (same button to toggle)
            ResumeGame();
        }
        // If navigation menu is active, pause button is ignored (locked)
    }

    private void OnNavigationMenu(InputAction.CallbackContext context)
    {
        Debug.Log($"[PauseManager] OnNavigationMenu called - Current menu: {currentActiveMenu}, IsPaused: {IsPaused}");
        
        if (currentActiveMenu == ActiveMenu.None)
        {
            // Open navigation menu
            ShowNavigationMenu();
        }
        else if (currentActiveMenu == ActiveMenu.NavigationMenu)
        {
            // Close navigation menu and resume game (same button to toggle)
            ResumeGame();
        }
        // If pause menu is active, navigation menu button is ignored (locked)
    }

    private void OnBackButton(InputAction.CallbackContext context)
    {
        Debug.Log($"[PauseManager] OnBackButton called - Current menu: {currentActiveMenu}");
        
        // Back button closes whatever menu is currently open
        if (currentActiveMenu != ActiveMenu.None)
        {
            ResumeGame();
        }
    }

    private void OnSwapMenu(InputAction.CallbackContext context)
    {
        // Only swap if game is paused and a menu is active
        if (!IsPaused || currentActiveMenu == ActiveMenu.None)
            return;

        if (currentActiveMenu == ActiveMenu.PauseMenu)
        {
            // Switch from pause menu to navigation menu
            SwapToNavigationMenu();
        }
        else if (currentActiveMenu == ActiveMenu.NavigationMenu)
        {
            // Switch from navigation menu to pause menu
            SwapToPauseMenu();
        }
    }

    private void ShowPauseMenu()
    {
        Time.timeScale = 0f;
        IsPaused = true;
        currentActiveMenu = ActiveMenu.PauseMenu;

        if (pauseMenuHolder != null)
            pauseMenuHolder.SetActive(true);
        
        if (navigationMenuHolder != null)
            navigationMenuHolder.SetActive(false);

        Debug.Log("Pause Menu Opened");
        
        // Switch to UI input - make sure actions remain subscribed
        if (InputReader.playerInput != null)
        {
            InputReader.playerInput.SwitchCurrentActionMap("UI");
        }
    }

    private void ShowNavigationMenu()
    {
        Time.timeScale = 0f;
        IsPaused = true;
        currentActiveMenu = ActiveMenu.NavigationMenu;

        if (navigationMenuHolder != null)
            navigationMenuHolder.SetActive(true);
        
        if (pauseMenuHolder != null)
            pauseMenuHolder.SetActive(false);

        Debug.Log("Navigation Menu Opened");
        
        // Switch to UI input
        if (InputReader.playerInput != null)
        {
            InputReader.playerInput.SwitchCurrentActionMap("UI");
        }
    }

    private void SwapToPauseMenu()
    {
        currentActiveMenu = ActiveMenu.PauseMenu;

        if (pauseMenuHolder != null)
            pauseMenuHolder.SetActive(true);
        
        if (navigationMenuHolder != null)
            navigationMenuHolder.SetActive(false);

        Debug.Log("Swapped to Pause Menu");
    }

    private void SwapToNavigationMenu()
    {
        currentActiveMenu = ActiveMenu.NavigationMenu;

        if (navigationMenuHolder != null)
            navigationMenuHolder.SetActive(true);
        
        if (pauseMenuHolder != null)
            pauseMenuHolder.SetActive(false);

        Debug.Log("Swapped to Navigation Menu");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        currentActiveMenu = ActiveMenu.None;

        if (pauseMenuHolder != null)
            pauseMenuHolder.SetActive(false);
        
        if (navigationMenuHolder != null)
            navigationMenuHolder.SetActive(false);

        Debug.Log("Game Resumed");
        
        // Switch back to Gameplay input
        if (InputReader.playerInput != null)
        {
            InputReader.playerInput.SwitchCurrentActionMap("Gameplay");
        }
    }

    // Public methods for UI buttons to call
    public void OnResumeButtonClicked()
    {
        ResumeGame();
    }

    public void OnSwapMenuButtonClicked()
    {
        OnSwapMenu(new InputAction.CallbackContext());
    }
}

