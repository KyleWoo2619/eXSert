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
    [SerializeField]
    private InputActionAsset _playerControls;
    public static InputActionAsset playerControls { get; private set; }

    private static PlayerInput _playerInput;

    // Static property to access PlayerInput instance
    public static PlayerInput playerInput
    {
        get
        {
            // Lazy load PlayerInput if not already assigned
            if (_playerInput == null && awakeComplete == true)
            {
                Debug.LogError("InputReader: PlayerInput not assigned, returning null value");
            }

            return _playerInput;
        } 

        private set
        {
            _playerInput = value;
        }
    }
    

    internal float mouseSens;

    private static InputAction moveAction => playerInput == null ? null : playerInput.actions["Move"];
    private static InputAction jumpAction => playerInput == null ? null : playerInput.actions["Jump"];
    private static InputAction lookAction => playerInput == null ? null : playerInput.actions["Look"];
    private static InputAction changeStanceAction => playerInput == null ? null : playerInput.actions["ChangeStance"];
    private static InputAction guardAction => playerInput == null ? null : playerInput.actions["Guard"];
    private static InputAction lightAttackAction => playerInput == null ? null : playerInput.actions["LightAttack"];
    private static InputAction heavyAttackAction => playerInput == null ? null : playerInput.actions["HeavyAttack"];
    private static InputAction dashAction => playerInput == null ? null : playerInput.actions["Dash"];
    private static InputAction navigationMenuAction => playerInput == null ? null : playerInput.actions["NavigationMenu"];

    // array to hold all actions for easy enabling/disabling if needed
    private InputAction[] actions;

    public static bool inputBusy = false;

    // Gets the input and sets the variable
    public static Vector2 MoveInput
    { 
        get
        {
            if (moveAction == null)
                return Vector2.zero;

            return moveAction.ReadValue<Vector2>();
        }
    }

    public static Vector2 LookInput
    { 
        get
        {
            if (lookAction == null)
                return Vector2.zero;

            return lookAction.ReadValue<Vector2>();
        }
    }

    static bool awakeComplete = false;
    override protected void Awake()
    {
        base.Awake(); // Call singleton Awake first

        // tries to assign playerControls if it isn't already assigned
        if (_playerControls == null)
            playerControls = new eXsert.PlayerControls().asset; // Initialize the InputActionAsset

        else
            playerControls = _playerControls;

        if (playerInput == null)
        {
            Debug.LogWarning("InputReader: PlayerInput not assigned in inspector, attempting to find in scene...");
            playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput == null)
            {
                Debug.LogError("InputReader: No PlayerInput found in scene during Awake!");
            }
            else
            {
                Debug.Log("InputReader: Successfully found PlayerInput in scene during Awake!");
            }
        }

        //Sets gamepad deadzone
        InputSystem.settings.defaultDeadzoneMin = 0;

        // Move action array initialization here so playerInput is guaranteed to be assigned
        actions = new InputAction[] {moveAction, jumpAction, lookAction, changeStanceAction, guardAction, lightAttackAction, heavyAttackAction, dashAction, navigationMenuAction};

        awakeComplete = true;
    }

    //Turns the actions on
    private void OnEnable()
    {
        EnableAllActions();
    }

    private void OnDisable()
    {
        DisableAllActions();
    }

    public static void AssignPlayerInput(PlayerInput newPlayerInput)
    {
        Debug.Log("InputReader: Assigning new PlayerInput instance.");

        if (newPlayerInput == null)
        {
            Debug.LogError("InputReader: Cannot assign null PlayerInput!");
            return;
        }
        playerInput = newPlayerInput;
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
        DisableAllActions();

        playerInput = newPlayerInput;

        if (!playerInput.enabled)
            playerInput.enabled = true;

        // Optionally set the correct map first so action lookups succeed
        if (switchToGameplay)
        {
            try { playerInput.SwitchCurrentActionMap("Gameplay"); }
            catch (System.Exception e) { Debug.LogWarning($"[InputReader] Failed to switch to Gameplay map during rebind: {e.Message}"); }
        }

        // Re-enable actions if this component is active
        if (isActiveAndEnabled)
        {
            EnableAllActions();
        }

        Debug.Log("[InputReader] Rebound to new PlayerInput and actions re-enabled.");
    }

    private void EnableAllActions()
    {
        foreach (var action in actions)
        {
            if (action != null)
                action.Enable();
            else
                Debug.LogWarning($"InputReader: Tried to enable action {action.name} but it was null!");
        }
    }

    private void DisableAllActions()
    {
        foreach (var action in actions)
        {
            if (action != null)
                action.Disable();
            else
                Debug.LogWarning($"InputReader: Tried to disable action {action.name} but it was null!");
        }
    }
}
