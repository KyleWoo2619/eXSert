/*
Written by Brandon Wahl

Manages the player's ability to guard

* Edited by Will T.
* - Streamlined camera functionality to use CameraManager
* - Added parry functionality with CombatManager
* - Integrated with AnimFacade for guard animations
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class Guard : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionReference _guardActionReference;

    [Header("References")]
    [SerializeField] AnimFacade animFacade;
    [SerializeField] bool useCameraManager = true; // Toggle camera switching on/off

    private void Awake()
    {
        // Auto-get AnimFacade if not assigned
        if (animFacade == null)
            animFacade = GetComponent<AnimFacade>();
    }

    private void OnEnable()
    {
        if (_guardActionReference == null || _guardActionReference.action == null)
            Debug.LogError("Guard Input Action Reference is not set in the inspector. Player won't be able to guard.");

        else
            _guardActionReference.action.Enable();
    }

    private void Update()
    {
        if(_guardActionReference != null && _guardActionReference.action != null){
            // Check if the guard button was pressed this frame and enter guard mode
            if (_guardActionReference.action.WasPressedThisFrame() && !InputReader.inputBusy)
            {
                EnterGuardMode();
            }

            // Check if the guard button was released this frame and exit guard modes
            if (_guardActionReference.action.WasReleasedThisFrame() && CombatManager.isGuarding)
            {
                ExitGuardMode();
            }
        }
    }

    /// <summary>
    /// Enter guard mode - handles animation, combat state, and camera
    /// </summary>
    private void EnterGuardMode()
    {
        // Update combat state
        CombatManager.EnterGuard();

        // Trigger guard animation
        animFacade?.StartGuard();

        // Switch to guard camera if enabled
        if (useCameraManager && CameraManager.Instance != null)
        {
            CameraManager.Instance.SwitchToGuard();
        }

        Debug.Log("[Guard] Entered guard mode");
    }

    /// <summary>
    /// Exit guard mode - handles animation, combat state, and camera
    /// </summary>
    private void ExitGuardMode()
    {
        // Update combat state
        CombatManager.ExitGuard();

        // Exit guard animation
        animFacade?.StopGuard();

        // Return to gameplay camera if enabled
        if (useCameraManager && CameraManager.Instance != null)
        {
            CameraManager.Instance.SwitchToGameplay();
        }

        Debug.Log("[Guard] Exited guard mode");
    }

    private void OnDisable()
    {
        if (_guardActionReference != null && _guardActionReference.action != null)
            _guardActionReference.action.Disable();
    }
}
