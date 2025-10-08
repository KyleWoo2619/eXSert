using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : Singletons.Singleton<PauseManager>
{
    [Header("Input")]
    [SerializeField] private InputActionReference _pauseActionReference;
    [SerializeField] private InputActionReference _resumeActionReference;

    public static bool IsPaused { get; private set; } = false;

    private void OnEnable()
    {
        if (_pauseActionReference == null || _pauseActionReference.action == null)
            Debug.LogWarning($"Pause Input Action Reference is not set in the inspector. Keyboard/Controller Input won't pause the game properly");
        else
            _pauseActionReference.action.performed += OnPause;


        if (_resumeActionReference == null || _resumeActionReference.action == null)
            Debug.LogWarning($"Resume Input Action Reference is not set in the inspector. Keyboard/Controller Input won't resume the game properly");
        else
            _resumeActionReference.action.performed += OnResume;
    }

    private void OnDisable()
    {
        if (_pauseActionReference != null && _pauseActionReference.action != null)
            _pauseActionReference.action.performed -= OnPause;


        if (_resumeActionReference != null && _resumeActionReference.action != null)
            _resumeActionReference.action.performed -= OnResume;
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (!IsPaused)
            PauseGame();
    }

    private void OnResume(InputAction.CallbackContext context)
    {
        if (IsPaused)
            ResumeGame();
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        IsPaused = true;

        Debug.Log("Game Paused");
        // disables gameplay input and enables UI input
        InputReader.playerInput.SwitchCurrentActionMap("UI");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;

        Debug.Log("Game Resumed");
        // disables UI input and enables gameplay input
        InputReader.playerInput.SwitchCurrentActionMap("Gameplay");

    }
}
