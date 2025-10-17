/*
 * Written by Brandon Wahl
 * 
 * Assigns events to their action in the player's action map
*/

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Singletons;
using UnityEditor.ShaderGraph.Serialization;

public class InputReader : Singleton<InputReader>
{
    [SerializeField] private InputActionAsset _playerControls;
    [SerializeField] internal PlayerInput _playerInput;

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

    public static bool inputBusy = false;

    [Header("DeadzoneValues")]
    [SerializeField] private float leftStickDeadzoneValue;

    // Gets the input and sets the variable
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool DashTrigger { get; private set; } = false;

    // Reset methods for triggers that need manual resetting
    public void ResetDashTrigger()
    {
        DashTrigger = false;
    }

    override protected void Awake()
    {
        if (_playerInput == null)
        {
            Debug.LogError("Player Input component not found. Input won't work.");
        }
        else
            playerInput = _playerInput;


        if (_playerControls == null)
        {
            Debug.LogError("Player Controls Input Action component not found. Input won't work.");
        }
        else
            playerControls = _playerControls;


        base.Awake();

        // Assigns the input action variables to the action in the action map
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        lookAction = playerInput.actions["Look"];
        changeStanceAction = playerInput.actions["ChangeStance"];
        guardAction = playerInput.actions["Guard"];
        lightAttackAction = playerInput.actions["LightAttack"];
        heavyAttackAction = playerInput.actions["HeavyAttack"];
        dashAction = playerInput.actions["Dash"];
        

        //RegisterInputAction();
            
        //Sets gamepad deadzone
        InputSystem.settings.defaultDeadzoneMin = leftStickDeadzoneValue;
    }

    private void Update()
    {
        MoveInput = moveAction.ReadValue<Vector2>();
        LookInput = lookAction.ReadValue<Vector2>();
    }


    //Turns the actions on
    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        lookAction.Enable();
        changeStanceAction.Enable();
        guardAction.Enable();
        lightAttackAction.Enable();
        heavyAttackAction.Enable();
        dashAction.Enable();
            
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        lookAction.Disable();
        changeStanceAction.Disable();
        guardAction.Disable();
        lightAttackAction.Disable();
        heavyAttackAction.Disable();
        dashAction.Disable();
    }
}
