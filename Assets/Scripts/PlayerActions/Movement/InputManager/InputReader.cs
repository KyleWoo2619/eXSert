/*
Written by Brandon Wahl

Assigns events to their action in the player's action map

*/

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Singletons;
using UnityEditor.ShaderGraph.Serialization;

public class InputReader : Singletons.Singleton<InputReader>
    {
        [SerializeField] private InputActionAsset playerControls;

        //Gathers action map name and assigns it to a variable
        [SerializeField] private string actionMapName = "Gameplay";
        
        //Allows editor to change name of var if the name in the acton map changes
        [Header("Input Names")]
        [SerializeField] private string move = "Move";
        [SerializeField] private string jump = "Jump";
        [SerializeField] private string look = "Look";
        [SerializeField] private string changeStance = "ChangeStance";
        [SerializeField] private string guard = "Guard";
        [SerializeField] private string lightAttack = "LightAttack";
        [SerializeField] private string heavyAttack = "HeavyAttack";
        [SerializeField] private string dash = "Dash";

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

        [Header("DeadzoneValues")]
        [SerializeField] private float leftStickDeadzoneValue;

       // Gets the input and sets the variable
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpTrigger { get; internal set; }
        public bool ChangeStanceTrigger { get; private set; }
        public bool GuardTrigger { get; private set; }
        public bool LightAttackTrigger { get; internal set; }
        public bool HeavyAttackTrigger {  get; internal set; }
        public bool DashTrigger { get; internal set; }

        public static new InputReader Instance { get; private set; }

        // Reset methods for triggers that need manual resetting
        public void ResetDashTrigger()
        {
            DashTrigger = false;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

           // Assigns the input action variables to the action in the action map
            moveAction = playerControls.FindActionMap(actionMapName).FindAction(move);
            jumpAction = playerControls.FindActionMap(actionMapName).FindAction(jump);
            lookAction = playerControls.FindActionMap(actionMapName).FindAction(look);
            changeStanceAction = playerControls.FindActionMap(actionMapName).FindAction(changeStance);
            guardAction = playerControls.FindActionMap(actionMapName).FindAction(guard);
            lightAttackAction = playerControls.FindActionMap(actionMapName).FindAction(lightAttack);
            heavyAttackAction = playerControls.FindActionMap(actionMapName).FindAction(heavyAttack);
            dashAction = playerControls.FindActionMap(actionMapName).FindAction(dash);

        RegisterInputAction();
            
            //Sets gamepad deadzone
            InputSystem.settings.defaultDeadzoneMin = leftStickDeadzoneValue;
        }

        //Passes the input action to get/set variable when the action is performed or canceled
        void RegisterInputAction()
        {
            moveAction.performed += context => MoveInput = context.ReadValue<Vector2>();
            moveAction.canceled += context => MoveInput = context.ReadValue<Vector2>();

            lookAction.performed += context => LookInput = context.ReadValue<Vector2>();
            lookAction.canceled += context => LookInput = context.ReadValue<Vector2>();

            jumpAction.performed += context => JumpTrigger = true;
            jumpAction.canceled += context => JumpTrigger = false;

            changeStanceAction.performed += context => ChangeStanceTrigger = true;
            changeStanceAction.canceled += context => ChangeStanceTrigger = false;

            guardAction.performed += context => GuardTrigger = true;
            guardAction.canceled += context => GuardTrigger = false;

            lightAttackAction.performed += context => LightAttackTrigger = true;
            lightAttackAction.canceled += context => LightAttackTrigger = false;

            heavyAttackAction.performed += context => HeavyAttackTrigger = true;
            heavyAttackAction.canceled += context => HeavyAttackTrigger = false;

            dashAction.performed += context => DashTrigger = true;
            dashAction.canceled += context => DashTrigger = false;

    }

        

        //If the button is held down it returns true and vice versa
        public bool GetGuard()
        {
            return GuardTrigger;
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
