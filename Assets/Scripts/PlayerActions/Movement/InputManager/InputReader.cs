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
    private InputAction lockOnAction;
    private InputAction leftTargetAction;
    private InputAction rightTargetAction;

    private bool callbacksRegistered = false;

    public static event Action LockOnPressed;
    public static event Action LeftTargetPressed;
    public static event Action RightTargetPressed;

    public static bool inputBusy = false;

    [Header("DeadzoneValues")]
    [SerializeField, Range(0f, 0.5f)] internal float leftStickDeadzoneValue = 0.15f;
    [SerializeField, Range(0f, 0.5f)] internal float rightStickDeadzoneValue = 0.15f;

    // Gets the input and sets the variable
    public static Vector2 MoveInput { get; private set; }
    public static Vector2 LookInput { get; private set; }

    // Centralized action accessors so gameplay scripts never touch InputActions directly
    public static bool JumpTriggered =>
        Instance != null
        && Instance.jumpAction != null
        && Instance.jumpAction.triggered;

    public static bool DashTriggered =>
        Instance != null
        && Instance.dashAction != null
        && Instance.dashAction.triggered;

    public static bool JumpHeld =>
        Instance != null
        && Instance.jumpAction != null
        && Instance.jumpAction.IsPressed();

    public static bool DashHeld =>
        Instance != null
        && Instance.dashAction != null
        && Instance.dashAction.IsPressed();

    public static bool GuardHeld =>
        Instance != null
        && Instance.guardAction != null
        && Instance.guardAction.IsPressed();

    public static bool LightAttackTriggered =>
        Instance != null
        && Instance.lightAttackAction != null
        && Instance.lightAttackAction.triggered;

    public static bool HeavyAttackTriggered =>
        Instance != null
        && Instance.heavyAttackAction != null
        && Instance.heavyAttackAction.triggered;

    public static bool ChangeStanceTriggered =>
        Instance != null
        && Instance.changeStanceAction != null
        && Instance.changeStanceAction.triggered;

    public static bool InteractTriggered =>
        Instance != null
        && Instance.interactAction != null
        && Instance.interactAction.triggered;

    public static bool EscapePuzzleTriggered =>
        Instance != null
        && Instance.escapePuzzleAction != null
        && Instance.escapePuzzleAction.triggered;

    public static bool NavigationMenuTriggered =>
        Instance != null
        && Instance.navigationMenuAction != null
        && Instance.navigationMenuAction.triggered;

    protected override void Awake()
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
            lockOnAction = playerInput.actions["LockOn"];
            leftTargetAction = playerInput.actions["LeftTarget"];
            rightTargetAction = playerInput.actions["RightTarget"];
            
            // Try to get NavigationMenu, but don't fail if it doesn't exist
            try
            {
                navigationMenuAction = playerInput.actions["NavigationMenu"];
            }
            catch
            {
                Debug.LogWarning("NavigationMenu action not found - continuing without it");
            }

            RegisterActionCallbacks();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to assign input actions: {e.Message}");
            return;
        }

        //RegisterInputAction();

        // Sets a conservative global deadzone so even blended inputs respect drift filtering
        InputSystem.settings.defaultDeadzoneMin = Mathf.Min(leftStickDeadzoneValue, rightStickDeadzoneValue);
    }

    private void Update()
    {
        // Null checks to prevent Input System errors before initialization
        if (moveAction != null && moveAction.enabled)
            MoveInput = ApplyDeadzone(moveAction.ReadValue<Vector2>(), leftStickDeadzoneValue);
        else
            MoveInput = Vector2.zero;
            
        if (lookAction != null && lookAction.enabled)
            LookInput = ApplyDeadzone(lookAction.ReadValue<Vector2>(), rightStickDeadzoneValue);
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
        if (lockOnAction != null) lockOnAction.Enable();
        if (leftTargetAction != null) leftTargetAction.Enable();
        if (rightTargetAction != null) rightTargetAction.Enable();
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
        if (lockOnAction != null) lockOnAction.Disable();
        if (leftTargetAction != null) leftTargetAction.Disable();
        if (rightTargetAction != null) rightTargetAction.Disable();
    }

    private void OnDestroy()
    {
        UnregisterActionCallbacks();
    }

    /// <summary>
    /// Static method to assign a new PlayerInput instance.
    /// For compatibility with existing code that calls InputReader.AssignPlayerInput().
    /// </summary>
    /// <param name="newPlayerInput">The PlayerInput to assign.</param>
    public static void AssignPlayerInput(PlayerInput newPlayerInput)
    {
        if (Instance == null)
        {
            Debug.LogError("InputReader: Instance not available, cannot assign PlayerInput!");
            return;
        }

        if (newPlayerInput == null)
        {
            Debug.LogError("InputReader: Cannot assign null PlayerInput!");
            return;
        }

        Debug.Log("InputReader: Assigning new PlayerInput instance via AssignPlayerInput().");
        Instance.RebindTo(newPlayerInput, switchToGameplay: true);
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
        if (lockOnAction != null) lockOnAction.Disable();
        if (leftTargetAction != null) leftTargetAction.Disable();
        if (rightTargetAction != null) rightTargetAction.Disable();

        UnregisterActionCallbacks();

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
            lockOnAction = playerInput.actions["LockOn"];
            leftTargetAction = playerInput.actions["LeftTarget"];
            rightTargetAction = playerInput.actions["RightTarget"];

            try { navigationMenuAction = playerInput.actions["NavigationMenu"]; }
            catch { navigationMenuAction = null; }

            RegisterActionCallbacks();
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
            if (lockOnAction != null) lockOnAction.Enable();
            if (leftTargetAction != null) leftTargetAction.Enable();
            if (rightTargetAction != null) rightTargetAction.Enable();
        }

        Debug.Log("[InputReader] Rebound to new PlayerInput and actions re-enabled.");
    }

    private void RegisterActionCallbacks()
    {
        if (callbacksRegistered)
            return;

        if (lockOnAction != null)
            lockOnAction.performed += HandleLockOnPerformed;
        if (leftTargetAction != null)
            leftTargetAction.performed += HandleLeftTargetPerformed;
        if (rightTargetAction != null)
            rightTargetAction.performed += HandleRightTargetPerformed;

        callbacksRegistered = true;
    }

    private void UnregisterActionCallbacks()
    {
        if (!callbacksRegistered)
            return;

        if (lockOnAction != null)
            lockOnAction.performed -= HandleLockOnPerformed;
        if (leftTargetAction != null)
            leftTargetAction.performed -= HandleLeftTargetPerformed;
        if (rightTargetAction != null)
            rightTargetAction.performed -= HandleRightTargetPerformed;

        callbacksRegistered = false;
    }

    private void HandleLockOnPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        LockOnPressed?.Invoke();
    }

    private void HandleLeftTargetPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        LeftTargetPressed?.Invoke();
    }

    private void HandleRightTargetPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        RightTargetPressed?.Invoke();
    }

    private static Vector2 ApplyDeadzone(Vector2 value, float deadzone)
    {
        if (deadzone <= 0f)
            return value;

        return value.magnitude < deadzone ? Vector2.zero : value;
    }
}

