/*
 * Written by Brandon Wahl
 * 
 * Assigns events to their action in the player's action map
 * 
 * 
 * 
 * Editted by Will T
 * 
 * removed event assignments and now just reads input values directly from actions
 * tried to simplify input management
*/

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Singletons;

public class InputReader : Singleton<InputReader>
{
    [SerializeField] private InputActionAsset _playerControls;
    [SerializeField] internal PlayerInput _playerInput;

    [SerializeField] internal string activeControlScheme;

    private static InputActionAsset playerControls;
    public static PlayerInput playerInput {get; private set; }
    
    public bool ableToGuard;
    internal float mouseSens;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lookAction;
    private InputAction changeStanceAction;
    private InputAction guardAction;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private InputAction dashAction;
    private InputAction navigationMenuAction;
    private InputAction interactAction;
    private InputAction escapePuzzleAction;

    public static bool inputBusy = false;

    [Header("DeadzoneValues")]
    [SerializeField] internal float leftStickDeadzoneValue;

    // Gets the input and sets the variable
    public static Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool DashTrigger { get; private set; } = false;

    // Reset methods for triggers that need manual resetting
    public void ResetDashTrigger()
    {
        DashTrigger = false;
    }

    override protected void Awake()
    {
        base.Awake(); // Call singleton Awake first

        if (_playerInput == null)
        {
            Debug.LogError("Player Input component not found. Input won't work.");
            return; // Exit early if no PlayerInput
        }
        else
            playerInput = _playerInput;

        // Initialize activeControlScheme immediately so other scripts can read it during Awake/Start
        try
        {
            activeControlScheme = playerInput != null ? playerInput.currentControlScheme : string.Empty;
        }
        catch
        {
            activeControlScheme = string.Empty;
        }


        if (_playerControls == null)
        {
            Debug.LogError("Player Controls Input Action component not found. Input won't work.");
            return; // Exit early if no controls
        }
        else
            playerControls = _playerControls;

        // Make sure PlayerInput is enabled before accessing actions
        if (!playerInput.enabled)
        {
            Debug.LogWarning("PlayerInput is not enabled. Enabling now...");
            playerInput.enabled = true;
        }

        // Switch to Gameplay action map only (disable UI to prevent errors)
        try
        {
            playerInput.SwitchCurrentActionMap("Gameplay");
            Debug.Log("Switched to Gameplay action map");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not switch to Gameplay action map: {e.Message}");
        }

        // Assigns the input action variables to the action in the action map
        try
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            lookAction = playerInput.actions["Look"];
            changeStanceAction = playerInput.actions["ChangeStance"];
            guardAction = playerInput.actions["Guard"];
            lightAttackAction = playerInput.actions["LightAttack"];
            heavyAttackAction = playerInput.actions["HeavyAttack"];
            dashAction = playerInput.actions["Dash"];
            interactAction = playerInput.actions["Interact"];
            escapePuzzleAction = playerInput.actions["EscapePuzzle"];
            
            // Try to get NavigationMenu, but don't fail if it doesn't exist
            try
            {
                navigationMenuAction = playerInput.actions["NavigationMenu"];
            }
            catch
            {
                Debug.LogWarning("NavigationMenu action not found - continuing without it");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to assign input actions: {e.Message}");
            return;
        }

        //RegisterInputAction();
            
        //Sets gamepad deadzone
        InputSystem.settings.defaultDeadzoneMin = leftStickDeadzoneValue;
    }

    private void Update()
    {
        // Null checks to prevent Input System errors before initialization
        if (moveAction != null && moveAction.enabled)
            MoveInput = moveAction.ReadValue<Vector2>();
        else
            MoveInput = Vector2.zero;
            
        if (lookAction != null && lookAction.enabled)
            LookInput = lookAction.ReadValue<Vector2>();
        else
            LookInput = Vector2.zero;

        activeControlScheme = playerInput.currentControlScheme;
    }


    //Turns the actions on
    private void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
        if (jumpAction != null) jumpAction.Enable();
        if (lookAction != null) lookAction.Enable();
        if (changeStanceAction != null) changeStanceAction.Enable();
        if (guardAction != null) guardAction.Enable();
        if (lightAttackAction != null) lightAttackAction.Enable();
        if (heavyAttackAction != null) heavyAttackAction.Enable();
        if (dashAction != null) dashAction.Enable();
        if (navigationMenuAction != null) navigationMenuAction.Enable();
        if (interactAction != null) interactAction.Enable();
        if (escapePuzzleAction != null) escapePuzzleAction.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (jumpAction != null) jumpAction.Disable();
        if (lookAction != null) lookAction.Disable();
        if (changeStanceAction != null) changeStanceAction.Disable();
        if (guardAction != null) guardAction.Disable();
        if (lightAttackAction != null) lightAttackAction.Disable();
        if (heavyAttackAction != null) heavyAttackAction.Disable();
        if (dashAction != null) dashAction.Disable();
        if (navigationMenuAction != null) navigationMenuAction.Disable();
        if (interactAction != null) interactAction.Disable();
        if (escapePuzzleAction != null) escapePuzzleAction.Disable();
    }

    /// <summary>
    /// Rebind this InputReader to a new PlayerInput instance (e.g., after scene restart or player respawn).
    /// Safely swaps action references and ensures the correct action map is active.
    /// </summary>
    /// <param name="newPlayerInput">The PlayerInput to bind to.</param>
    /// <param name="switchToGameplay">If true, switch the current action map to "Gameplay".</param>
    public void RebindTo(PlayerInput newPlayerInput, bool switchToGameplay = true)
    {
        if (newPlayerInput == null)
        {
            Debug.LogWarning("[InputReader] RebindTo called with null PlayerInput");
            return;
        }

        // Disable any old actions to avoid ghost reads
        if (moveAction != null) moveAction.Disable();
        if (jumpAction != null) jumpAction.Disable();
        if (lookAction != null) lookAction.Disable();
        if (changeStanceAction != null) changeStanceAction.Disable();
        if (guardAction != null) guardAction.Disable();
        if (lightAttackAction != null) lightAttackAction.Disable();
        if (heavyAttackAction != null) heavyAttackAction.Disable();
        if (dashAction != null) dashAction.Disable();
        if (navigationMenuAction != null) navigationMenuAction.Disable();
        if (interactAction != null) interactAction.Disable();
        if (escapePuzzleAction != null) escapePuzzleAction.Disable();

        _playerInput = newPlayerInput;
        playerInput = newPlayerInput;

        if (!playerInput.enabled)
            playerInput.enabled = true;

        // Optionally set the correct map first so action lookups succeed
        if (switchToGameplay)
        {
            try { playerInput.SwitchCurrentActionMap("Gameplay"); }
            catch (System.Exception e) { Debug.LogWarning($"[InputReader] Failed to switch to Gameplay map during rebind: {e.Message}"); }
        }

        try
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            lookAction = playerInput.actions["Look"];
            changeStanceAction = playerInput.actions["ChangeStance"];
            guardAction = playerInput.actions["Guard"];
            lightAttackAction = playerInput.actions["LightAttack"];
            heavyAttackAction = playerInput.actions["HeavyAttack"];
            dashAction = playerInput.actions["Dash"];
            interactAction = playerInput.actions["Interact"];
            escapePuzzleAction = playerInput.actions["EscapePuzzle"];

            try { navigationMenuAction = playerInput.actions["NavigationMenu"]; }
            catch { navigationMenuAction = null; }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InputReader] Failed to assign actions during rebind: {e.Message}");
        }

        // Re-enable actions if this component is active
        if (isActiveAndEnabled)
        {
            if (moveAction != null) moveAction.Enable();
            if (jumpAction != null) jumpAction.Enable();
            if (lookAction != null) lookAction.Enable();
            if (changeStanceAction != null) changeStanceAction.Enable();
            if (guardAction != null) guardAction.Enable();
            if (lightAttackAction != null) lightAttackAction.Enable();
            if (heavyAttackAction != null) heavyAttackAction.Enable();
            if (dashAction != null) dashAction.Enable();
            if (navigationMenuAction != null) navigationMenuAction.Enable();
            if (interactAction != null) interactAction.Enable();
            if (escapePuzzleAction != null) escapePuzzleAction.Enable();
        }

        Debug.Log("[InputReader] Rebound to new PlayerInput and actions re-enabled.");
    }
}
