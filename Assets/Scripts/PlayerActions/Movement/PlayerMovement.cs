/*
Written by Brandon Wahl

Handles player movement and saves/loads player position
*
* edited by Will T
* 
* Added dash functionality and modified jump to include double jump
* Also added animator integration
*/

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities.Combat.Attacks;

public class PlayerMovement : MonoBehaviour
{
    private static CharacterController characterController;
    public static bool isGrounded { get {return characterController.isGrounded; } }

    [Header("Player Animator")]
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerAttackManager attackManager;

    [Header("Input")]
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _dashAction;

    [Header("Input Filtering")]
    [SerializeField, Range(0f, 0.5f)]
    private float moveInputDeadZone = 0.08f;

    [Header("Player Movement Settings")]
    [SerializeField]
    private float walkSpeed = 2.25f;

    [SerializeField]
    private float jogSpeed = 3.75f;

    [SerializeField]
    private float sprintSpeed = 5.5f;

    [SerializeField, Range(0.5f, 10f)]
    private float jogDelaySeconds = 3.5f;

    [SerializeField, Range(0.2f, 1f)]
    private float joystickWalkThreshold = 0.8f;

    [SerializeField, Range(0.2f, 10f)]
    private float sprintDelaySeconds = 2f;

    [SerializeField, Range(0f, 20f)]
    private float friction = 6f;

    [SerializeField, Range(0f, 0.3f)]
    private float inputReleaseGrace = 0.08f;

    [Header("Landing Settings")]
    [SerializeField, Range(0f, 1f)]
    private float landingLockDuration = 0.35f;

    [Header("Keyboard Overrides")]
    [SerializeField, Range(0.1f, 0.5f)]
    private float keyboardDoubleTapWindow = 0.25f;

    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private bool shouldFaceMoveDirection = true;

    internal Vector3 currentMovement = Vector3.zero;

    [Header("Player Jump Settings")]
    [SerializeField]
    private float gravity = -9.81f;

    [Tooltip("How high the player will jump")]
    [SerializeField, Range(1f, 10f)]
    private float jumpForce;

    [SerializeField, Range(1f, 10f)]
    private float doubleJumpForce;

    [SerializeField, Range(0, 15)]
    private float airAttackHopForce = 5;

    [SerializeField, Range(1, 50)]
    private float terminalVelocity = 20;

    [SerializeField]
    private bool canDoubleJump;

    [Header("GroundCheck Variables")]
    [SerializeField]
    private Vector3 boxSize = new Vector3(.8f, .1f, .8f);

    [SerializeField]
    private float maxDistance;

    [Tooltip("Which layer the ground check detects for")]
    public LayerMask layerMask;

    [Header("Dash Settings")]
    [SerializeField]
    private float dashDistance = 6f;

    [SerializeField, Range(0.05f, 1f)]
    private float dashDuration = 0.25f;

    [SerializeField]
    private float dashCoolDown = 0.6f;

    [Header("Camera Settings")]
    [SerializeField] bool invertYAxis = false;

    [Header("High Fall Settings")]
    [SerializeField, Range(0.5f, 30f)]
    private float highFallHeightThreshold = 6f;

    [SerializeField, Range(1f, 100f)]
    private float highFallGroundProbeDistance = 25f;

    private bool canDash = true;
    private bool isDashing;
    private Vector3 dashVelocity = Vector3.zero;
    private bool wasGrounded;
    private float walkToJogTimer;
    private float jogToSprintTimer;
    private bool sprintChargeActive;
    private bool wasMoving;
    private float inputReleaseTimer;
    private Vector2 cachedMoveInput;
    private bool doubleJumpAvailable;
    private bool airborneAnimationLocked;
    private bool fallingAnimationPlaying;
    private float landingAnimationLockTimer;
    private float airborneStartHeight;
    private bool highFallActive;
    private bool airDashAvailable = true;
    private bool suspendGravityDuringDash;
    private bool airDashInProgress;
    private bool hasCombatIdleController;
    private PlayerCombatIdleController combatIdleController;

    private Vector3 forward => new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z);
    private Vector3 right => new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z);

    private enum GroundMoveState
    {
        Walk,
        Jog,
        Sprint
    }

    private bool keyboardJogOverride;
    private float tapUpTime = float.NegativeInfinity;
    private float tapDownTime = float.NegativeInfinity;
    private float tapLeftTime = float.NegativeInfinity;
    private float tapRightTime = float.NegativeInfinity;
    private Vector2 previousKeyboardInput = Vector2.zero;

    private GroundMoveState moveState = GroundMoveState.Walk;
    private float CurrentSpeed => moveState switch
    {
        GroundMoveState.Walk => walkSpeed,
        GroundMoveState.Jog => jogSpeed,
        GroundMoveState.Sprint => sprintSpeed,
        _ => walkSpeed
    };

    private void Awake()
    {
        attackManager = attackManager
            ?? GetComponent<PlayerAttackManager>()
            ?? GetComponentInChildren<PlayerAttackManager>()
            ?? GetComponentInParent<PlayerAttackManager>();

        combatIdleController = GetComponent<PlayerCombatIdleController>()
            ?? GetComponentInChildren<PlayerCombatIdleController>()
            ?? GetComponentInParent<PlayerCombatIdleController>()
            ?? FindFirstCombatIdleController();
        hasCombatIdleController = combatIdleController != null;
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animationController = animationController != null ? animationController : GetComponent<PlayerAnimationController>();

        // Assign PlayerInput to InputReader
        InputReader.AssignPlayerInput(GetComponent<PlayerInput>());

        doubleJumpAvailable = canDoubleJump;
        airborneStartHeight = transform.position.y;
        airDashAvailable = true;
        suspendGravityDuringDash = false;
    }
    private void OnEnable()
    {
        PlayerAttackManager.OnAttack += AerialAttackHop;
    }

    private void OnDisable()
    {
        PlayerAttackManager.OnAttack -= AerialAttackHop;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        // Debug checks
        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform is NULL! Assign your Cinemachine camera to Camera Transform field.");
            return;
        }

        if (characterController == null)
        {
            Debug.LogError("Character Controller is NULL!");
            return;
        }

        landingAnimationLockTimer = Mathf.Max(0f, landingAnimationLockTimer - Time.deltaTime);

        Move();

        if (_jumpAction != null && _jumpAction.action != null && _jumpAction.action.triggered)
            OnJump();

        if (_dashAction != null && _dashAction.action != null && _dashAction.action.triggered)
            OnDash();

        HandleAirborneAnimations();

        ApplyMovement();
    }

    private void Move()
    {
        if (InputReader.inputBusy || isDashing)
        {
            currentMovement.x = 0f;
            currentMovement.z = 0f;
            return;
        }

        Vector2 inputMove = ApplyMoveDeadZone(InputReader.MoveInput);
        float inputMagnitude = inputMove.magnitude;
        bool hasMovementInput = inputMagnitude > moveInputDeadZone;

        Vector2 keyboardInput = ReadKeyboardDirection();
        bool keyboardMovementActive = ProcessKeyboardDoubleTap(keyboardInput);
        bool usingAnalogThresholds = !keyboardMovementActive && IsAnalogControlSchemeActive();

        if (!hasMovementInput)
        {
            inputReleaseTimer += Time.deltaTime;
            if (inputReleaseTimer < inputReleaseGrace && cachedMoveInput.sqrMagnitude > 0.0001f)
            {
                inputMove = cachedMoveInput;
                inputMagnitude = inputMove.magnitude;
                hasMovementInput = true;
            }
        }
        else
        {
            cachedMoveInput = inputMove;
            inputReleaseTimer = 0f;
        }
        bool previouslyMoving = wasMoving;

        if (hasMovementInput)
        {
            bool stateChanged = UpdateMoveState(usingAnalogThresholds, inputMagnitude);

            Vector3 moveDirection = (forward * inputMove.y + right * inputMove.x).normalized;
            Vector3 desiredVelocity = moveDirection * CurrentSpeed;
            currentMovement.x = desiredVelocity.x;
            currentMovement.z = desiredVelocity.z;

            if (shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
            }

            if (!previouslyMoving && !stateChanged)
            {
                PlayMovementAnimation();
            }

            landingAnimationLockTimer = 0f;
            wasMoving = true;
        }
        else
        {
            wasMoving = false;
            inputReleaseTimer = 0f;
            cachedMoveInput = Vector2.zero;
            ResetMoveState();
            currentMovement.x = Mathf.MoveTowards(currentMovement.x, 0f, friction * Time.deltaTime);
            currentMovement.z = Mathf.MoveTowards(currentMovement.z, 0f, friction * Time.deltaTime);

            if (characterController.isGrounded && !airborneAnimationLocked && landingAnimationLockTimer <= 0f &&
                (previouslyMoving || (Mathf.Abs(currentMovement.x) < 0.01f && Mathf.Abs(currentMovement.z) < 0.01f)))
            {
                EnsureCombatIdleControllerReference();
                if (!hasCombatIdleController)
                    animationController?.PlayIdle();
            }
        }
    }

    private void EnsureCombatIdleControllerReference()
    {
        if (hasCombatIdleController && combatIdleController != null)
            return;

        combatIdleController = combatIdleController
            ?? GetComponent<PlayerCombatIdleController>()
            ?? GetComponentInChildren<PlayerCombatIdleController>()
            ?? GetComponentInParent<PlayerCombatIdleController>()
            ?? FindFirstCombatIdleController();

        hasCombatIdleController = combatIdleController != null;
    }

    private PlayerCombatIdleController FindFirstCombatIdleController()
    {
#if UNITY_2022_3_OR_NEWER
        return FindFirstObjectByType<PlayerCombatIdleController>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<PlayerCombatIdleController>(true);
#endif
    }

    private void OnJump()
    {
        // checks to see if the player can jump or double jump
        if (characterController.isGrounded)
        {
            currentMovement.y = jumpForce;
            doubleJumpAvailable = canDoubleJump;
            airborneAnimationLocked = true;
            fallingAnimationPlaying = false;
            highFallActive = false;

            animationController?.PlayJump();
        }
        else if (canDoubleJump && doubleJumpAvailable)
        {
            currentMovement.y += doubleJumpForce;
            doubleJumpAvailable = false;
            airborneAnimationLocked = true;
            fallingAnimationPlaying = false;
            highFallActive = true;
            animationController?.PlayAirJumpStart();
        }
    }

    private void OnDash()
    {
        if (!canDash)
            return;

        bool grounded = characterController.isGrounded;
        bool dashAllowed = grounded || airDashAvailable;
        if (!dashAllowed)
            return;

        if (InputReader.inputBusy)
            attackManager?.ForceCancelCurrentAttack();

        canDash = false;
        InputReader.inputBusy = true;

        Vector3 dashDirection = (forward * InputReader.MoveInput.y) + (right * InputReader.MoveInput.x);
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
        }
        dashDirection.Normalize();

        if (!grounded)
            airDashAvailable = false;

        if (dashDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(dashDirection, Vector3.up);
        }

        StartCoroutine(DashCoroutine(dashDirection, !grounded));
    }

    private IEnumerator DashCoroutine(Vector3 direction, bool isAirDash)
    {
        isDashing = true;
        airDashInProgress = isAirDash;
        dashVelocity = direction * (dashDistance / dashDuration);
        currentMovement.x = 0f;
        currentMovement.z = 0f;

        if (isAirDash)
        {
            suspendGravityDuringDash = true;
            currentMovement.y = 0f;
        }

        if (characterController.isGrounded)
            animationController?.PlayDash();
        else
            animationController?.PlayAirDash();

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        dashVelocity = Vector3.zero;
        isDashing = false;
        airDashInProgress = false;
        suspendGravityDuringDash = false;
        InputReader.inputBusy = false;

        if (InputReader.MoveInput.sqrMagnitude > 0.1f)
        {
            TrySetMoveState(GroundMoveState.Sprint, force: true);
        }
        else
        {
            ResetMoveState();
        }

        yield return new WaitForSeconds(dashCoolDown);
        canDash = true;
    }

    private void AerialAttackHop(PlayerAttack _)
    {
        if (characterController.isGrounded) return;

        currentMovement.y = airAttackHopForce;
        airborneAnimationLocked = true;
        fallingAnimationPlaying = false;
    }

    private void ApplyMovement()
    {
        // apply gravity over time unless temporarily suspended for air dash
        if (!suspendGravityDuringDash)
        {
            currentMovement.y += gravity * Time.deltaTime;
        }
        currentMovement.y = Mathf.Clamp(currentMovement.y, -terminalVelocity, terminalVelocity);

        Vector3 horizontalMovement = isDashing ? dashVelocity : new Vector3(currentMovement.x, 0, currentMovement.z);
        Vector3 finalVelocity = horizontalMovement + Vector3.up * currentMovement.y;

        // move the character controller
        characterController.Move(finalVelocity * Time.deltaTime);

        // reset vertical movement when grounded
        if (characterController.isGrounded && currentMovement.y < 0)
        {
            currentMovement.y = -1f; // small negative value to keep the player grounded
        }
    }

    private void HandleAirborneAnimations()
    {
        bool grounded = characterController.isGrounded;

        if (!wasGrounded && grounded)
        {
            animationController?.PlayLand();
            ResetMoveState();
            doubleJumpAvailable = canDoubleJump;
            airborneAnimationLocked = false;
            fallingAnimationPlaying = false;
            highFallActive = false;
            airDashAvailable = true;
            suspendGravityDuringDash = false;

            bool movementBuffered = InputReader.MoveInput.sqrMagnitude > moveInputDeadZone * moveInputDeadZone;
            landingAnimationLockTimer = movementBuffered ? 0f : landingLockDuration;
        }
        else if (!grounded)
        {
            if (wasGrounded)
            {
                airborneAnimationLocked = true;
                fallingAnimationPlaying = false;
                airborneStartHeight = transform.position.y;
                highFallActive = false;
                airDashAvailable = true;
            }

            if (currentMovement.y <= 0f && !fallingAnimationPlaying && !airDashInProgress)
            {
                if (ShouldUseHighFallAnimation())
                    animationController?.PlayFallingHigh();
                else
                    animationController?.PlayFalling();

                fallingAnimationPlaying = true;
            }
        }

        wasGrounded = grounded;
    }

    private void ResetMoveState()
    {
        walkToJogTimer = 0f;
        jogToSprintTimer = 0f;
        sprintChargeActive = false;
        moveState = GroundMoveState.Walk;
        keyboardJogOverride = false;
    }

    private bool TrySetMoveState(GroundMoveState state, bool force = false)
    {
        if (!force && moveState == state)
            return false;

        moveState = state;

        switch (state)
        {
            case GroundMoveState.Walk:
                walkToJogTimer = 0f;
                jogToSprintTimer = 0f;
                sprintChargeActive = false;
                break;
            case GroundMoveState.Jog:
                jogToSprintTimer = 0f;
                sprintChargeActive = true;
                break;
            case GroundMoveState.Sprint:
                sprintChargeActive = false;
                break;
        }

        PlayMovementAnimation();
        return true;
    }

    private bool IsAnalogControlSchemeActive()
    {
        if (InputReader.Instance == null)
            return false;

        string scheme = InputReader.Instance.activeControlScheme;
        if (string.IsNullOrEmpty(scheme))
            return false;

        scheme = scheme.ToLowerInvariant();
        return scheme.Contains("gamepad") || scheme.Contains("controller");
    }

    private bool UpdateMoveState(bool useAnalogThresholds, float inputMagnitude)
    {
        bool stateChanged = false;

        if (useAnalogThresholds)
        {
            if (inputMagnitude < joystickWalkThreshold)
            {
                stateChanged |= TrySetMoveState(GroundMoveState.Walk);
            }
            else if (moveState == GroundMoveState.Walk)
            {
                stateChanged |= TrySetMoveState(GroundMoveState.Jog);
            }
        }
        else if (moveState == GroundMoveState.Walk && !keyboardJogOverride)
        {
            walkToJogTimer += Time.deltaTime;
            if (walkToJogTimer >= jogDelaySeconds)
            {
                stateChanged |= TrySetMoveState(GroundMoveState.Jog);
            }
        }

        if (moveState == GroundMoveState.Jog && sprintChargeActive)
        {
            jogToSprintTimer += Time.deltaTime;
            if (jogToSprintTimer >= sprintDelaySeconds && inputMagnitude > moveInputDeadZone)
            {
                stateChanged |= TrySetMoveState(GroundMoveState.Sprint);
            }
        }

        return stateChanged;
    }

    private Vector2 ApplyMoveDeadZone(Vector2 rawInput)
    {
        if (Mathf.Abs(rawInput.x) < moveInputDeadZone)
            rawInput.x = 0f;

        if (Mathf.Abs(rawInput.y) < moveInputDeadZone)
            rawInput.y = 0f;

        return rawInput;
    }

    private Vector2 ReadKeyboardDirection()
    {
        if (Keyboard.current == null)
            return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            x -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            x += 1f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            y += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            y -= 1f;

        return new Vector2(x, y);
    }

    private bool ProcessKeyboardDoubleTap(Vector2 keyboardInput)
    {
        bool keyboardActive = keyboardInput.sqrMagnitude > 0f;

        if (!keyboardActive)
        {
            keyboardJogOverride = false;
            previousKeyboardInput = Vector2.zero;
            return false;
        }

        float now = Time.time;

        bool upPressed = previousKeyboardInput.y <= 0f && keyboardInput.y > 0f;
        bool downPressed = previousKeyboardInput.y >= 0f && keyboardInput.y < 0f;
        bool rightPressed = previousKeyboardInput.x <= 0f && keyboardInput.x > 0f;
        bool leftPressed = previousKeyboardInput.x >= 0f && keyboardInput.x < 0f;

        EvaluateKeyboardTap(ref tapUpTime, upPressed, now);
        EvaluateKeyboardTap(ref tapDownTime, downPressed, now);
        EvaluateKeyboardTap(ref tapRightTime, rightPressed, now);
        EvaluateKeyboardTap(ref tapLeftTime, leftPressed, now);

        previousKeyboardInput = keyboardInput;
        return true;
    }

    private void EvaluateKeyboardTap(ref float lastTapTime, bool pressedThisFrame, float now)
    {
        if (!pressedThisFrame)
            return;

        if (now - lastTapTime <= keyboardDoubleTapWindow)
        {
            ActivateKeyboardJogOverride();
        }

        lastTapTime = now;
    }

    private void ActivateKeyboardJogOverride()
    {
        keyboardJogOverride = true;
        TrySetMoveState(GroundMoveState.Jog, force: true);
    }

    private bool ShouldUseHighFallAnimation()
    {
        if (highFallActive)
            return true;

        float fallDistance = airborneStartHeight - transform.position.y;
        if (fallDistance >= highFallHeightThreshold)
        {
            highFallActive = true;
            return true;
        }

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                highFallGroundProbeDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance >= highFallHeightThreshold)
            {
                highFallActive = true;
                return true;
            }
        }

        return false;
    }

    private void PlayMovementAnimation()
    {
        if (animationController == null)
            return;

        if (characterController != null && (!characterController.isGrounded || airborneAnimationLocked))
            return;

        if (landingAnimationLockTimer > 0f && InputReader.MoveInput.sqrMagnitude < moveInputDeadZone * moveInputDeadZone)
            return;

        switch (moveState)
        {
            case GroundMoveState.Walk:
                animationController.PlayWalk();
                break;
            case GroundMoveState.Jog:
                animationController.PlayJog();
                break;
            case GroundMoveState.Sprint:
                animationController.PlaySprint();
                break;
        }
    }
}
