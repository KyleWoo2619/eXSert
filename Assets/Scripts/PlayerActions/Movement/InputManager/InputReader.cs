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
using UnityEngine.SceneManagement;
using Singletons;
using eXsert;

public class InputReader : Singleton<InputReader>
{
    [SerializeField] private InputActionAsset _playerControls;
    [SerializeField] internal PlayerInput _playerInput;

    [SerializeField] internal string activeControlScheme;

    private static InputActionAsset playerControls;
    private PlayerControls runtimeGeneratedControls;
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
    [SerializeField, Range(0f, 0.5f)] private float lockOnDashSuppressionWindow = 0.18f;
    private float lastDashPerformedTime = float.NegativeInfinity;

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

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (_playerControls == null)
        {
            runtimeGeneratedControls = new PlayerControls();
            _playerControls = runtimeGeneratedControls.asset;
            playerControls = _playerControls;
            Debug.LogWarning("Player Controls Input Action asset not assigned on InputReader; creating a runtime copy so gameplay actions remain available.");
        }
        else
        {
            playerControls = _playerControls;
        }

        if (_playerInput != null)
        {
            RebindTo(_playerInput, switchToGameplay: true);
        }
        else if (!TryAutoBindFromLoadedScenes())
        {
            Debug.Log("[InputReader] No PlayerInput assigned; waiting for a Player scene to bind automatically.");
            StartCoroutine(WaitForPlayerInputRoutine());
        }

        InputSystem.settings.defaultDeadzoneMin = Mathf.Min(leftStickDeadzoneValue, rightStickDeadzoneValue);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnregisterActionCallbacks();
        if (runtimeGeneratedControls != null)
        {
            runtimeGeneratedControls.Dispose();
            runtimeGeneratedControls = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (playerInput != null)
            return;

        if (scene.isLoaded && TryBindFromScene(scene))
            return;

        // If the specific scene didn't contain a player, try a broader search (e.g., additive load order differences)
        TryAutoBindFromLoadedScenes();
    }

    private System.Collections.IEnumerator WaitForPlayerInputRoutine()
    {
        const float timeout = 10f;
        float elapsed = 0f;
        while (playerInput == null && elapsed < timeout)
        {
            if (TryAutoBindFromLoadedScenes())
                yield break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private bool TryAutoBindFromLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || scene.name == "DontDestroyOnLoad")
                continue;

            if (TryBindFromScene(scene))
                return true;
        }

        return false;
    }

    private bool TryBindFromScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        GameObject[] roots = scene.GetRootGameObjects();
        PlayerInput fallback = null;

        for (int i = 0; i < roots.Length; i++)
        {
            var candidate = roots[i].GetComponentInChildren<PlayerInput>(true);
            if (candidate == null)
                continue;

            if (candidate.GetComponent<InputReader>() != null)
                continue;

            bool likelyPlayer = candidate.gameObject.CompareTag("Player")
                || candidate.GetComponentInParent<PlayerPersistence>() != null;

            if (likelyPlayer)
            {
                Debug.Log($"[InputReader] Auto-binding to PlayerInput '{candidate.name}' in scene '{scene.name}'.");
                RebindTo(candidate, switchToGameplay: true);
                return true;
            }

            if (fallback == null)
                fallback = candidate;
        }

        if (fallback != null)
        {
            Debug.Log($"[InputReader] Fallback binding to PlayerInput '{fallback.name}' in scene '{scene.name}'.");
            RebindTo(fallback, switchToGameplay: true);
            return true;
        }

        return false;
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

        if (playerInput != null)
        {
            try
            {
                activeControlScheme = playerInput.currentControlScheme;
            }
            catch
            {
                activeControlScheme = string.Empty;
            }
        }
        else
        {
            activeControlScheme = string.Empty;
        }
    }

    #endregion


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

        if (playerInput.actions == null)
        {
            if (playerControls != null)
            {
                playerInput.actions = Instantiate(playerControls);
            }
            else
            {
                Debug.LogError("[InputReader] PlayerInput has no action asset and no fallback is available.");
                return;
            }
        }

        if (!playerInput.enabled)
            playerInput.enabled = true;

        playerInput.neverAutoSwitchControlSchemes = false;

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

        try
        {
            activeControlScheme = playerInput.currentControlScheme;
        }
        catch
        {
            activeControlScheme = string.Empty;
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
        if (dashAction != null)
            dashAction.performed += HandleDashPerformed;

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
        if (dashAction != null)
            dashAction.performed -= HandleDashPerformed;

        callbacksRegistered = false;
    }

    private void HandleLockOnPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (Time.time - lastDashPerformedTime <= lockOnDashSuppressionWindow)
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

    private void HandleDashPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        lastDashPerformedTime = Time.time;
    }

    private static Vector2 ApplyDeadzone(Vector2 value, float deadzone)
    {
        if (deadzone <= 0f)
            return value;

        return value.magnitude < deadzone ? Vector2.zero : value;
    }
}

