/*
Written by Brandon Wahl

Handles player movement and saves/loads player position
*
* edited by Will T
* 
* Added dash functionality and modified jump to include double jump
* Also added animator integration
*
* Enhanced by Kyle Woo
* 
* CHANGES:
* - Replaced direct Animator access with AnimFacade integration
* - Added AerialComboManager integration for air dash reset
* - Changed event subscription from PlayerAttackManager to EnhancedPlayerAttackManager
* - Added FeedMovement() calls to AnimFacade for proper animation syncing
* - Cleaned up animator parameter setting (now handled by AnimFacade)
*/

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnhancedPlayerMovement : MonoBehaviour
{
    private CharacterController characterController; // Changed from static to prevent conflicts
    
    // Ground check property - uses both CharacterController AND Physics check for reliability
    public bool isGrounded 
    { 
        get 
        {
            if (characterController == null) return false;
            
            // Primary check: CharacterController.isGrounded
            if (characterController.isGrounded) return true;
            
            // Secondary check: Physics BoxCast for more reliable detection
            // This catches cases where CharacterController fails (slopes, edges, spawn)
            Vector3 boxCenter = transform.position + (Vector3.down * (maxDistance > 0 ? maxDistance : 0.1f));
            bool physicsCheck = Physics.BoxCast(
                transform.position,
                boxSize * 0.5f,
                Vector3.down,
                Quaternion.identity,
                maxDistance > 0 ? maxDistance : 0.2f,
                layerMask
            );
            
            return physicsCheck;
        }
    }

    [Header("Animation Integration")]
    [SerializeField] private AnimFacade animFacade;
    [SerializeField] private AerialComboManager aerialComboManager;
    [Tooltip("Fallback: Only used if AnimFacade is null (not recommended)")]
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField] private InputActionReference _jumpAction;
    [SerializeField] private InputActionReference _dashAction;

    [Header("Player Movement Settings")]
    [Tooltip("Speed of player"), SerializeField] internal float speed;
    [SerializeField] private float maxNormalSpeed = 5f;
    [SerializeField] private float maxguardSpeed = 2.5f;
    [SerializeField, Range(0f, 20f)] private float friction = 3f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool shouldFaceMoveDirection = true;

    internal Vector3 currentMovement = Vector3.zero;

    [Header("Player Jump Settings")]
    [SerializeField] private float gravity = -9.81f;
    [Tooltip("How high the player will jump")][SerializeField][Range(1, 10)] private float jumpForce;
    [SerializeField, Range(1, 10)] private float doubleJumpForce;
    [SerializeField, Range(0, 15)] private float airAttackHopForce = 5;

    [SerializeField, Range(1, 50)] private float terminalVelocity = 20;
    [SerializeField] private bool canDoubleJump;

    [Header("GroundCheck Variables")]
    [SerializeField] private Vector3 boxSize = new Vector3(.8f, .1f, .8f);
    [SerializeField] private float maxDistance = 0.3f;
    [Tooltip("Which layer the ground check detects for (set to 'Default' or 'Ground' layer)")]
    public LayerMask layerMask = ~0; // Default to everything

    [Header("Dash Settings")]
    [SerializeField] [Range(1, 5)] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCoolDown;

    [Header("Camera Settings")]
    [SerializeField] bool invertYAxis = false;

    private float maxRunningSpeed => CombatManager.isGuarding ? maxguardSpeed : maxNormalSpeed;

    private bool canDash = true;
    private bool wasGroundedLastFrame = false;

    private Vector3 forward => new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z);
    private Vector3 right => new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z);

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Auto-find AnimFacade if not assigned
        if (animFacade == null)
            animFacade = GetComponent<AnimFacade>();
        if (animFacade == null)
            Debug.LogError("EnhancedPlayerMovement: No animation facade found! (AnimFacade or AnimatorCoderFacade)");

        // Auto-find AerialComboManager if not assigned
        if (aerialComboManager == null)
        {
            aerialComboManager = GetComponent<AerialComboManager>();
            if (aerialComboManager == null)
                Debug.LogWarning("EnhancedPlayerMovement: AerialComboManager not found! Air dash reset won't work.");
        }

        // Fallback animator warning
        if (animFacade == null && animator == null)
        {
            Debug.LogError("EnhancedPlayerMovement: Both AnimFacade and Animator are null! Player animations will not work.");
        }
    }

    private void OnEnable()
    {
        // Subscribe to attack event from the enhanced manager (used for aerial hop)
        EnhancedPlayerAttackManager.onAttack += AerialAttackHop;
    }

    private void OnDisable()
    {
        // Unsubscribe
        EnhancedPlayerAttackManager.onAttack -= AerialAttackHop;
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

        // Check for landing (for aerial combo manager)
        if (!wasGroundedLastFrame && isGrounded && aerialComboManager != null)
        {
            aerialComboManager.OnLanded();
        }
        wasGroundedLastFrame = isGrounded;
        
        Move();

        if (_jumpAction != null && _jumpAction.action != null && _jumpAction.action.triggered)
            OnJump();

        if (_dashAction != null && _dashAction.action != null && _dashAction.action.triggered)
            OnDash();

        ApplyMovement();

        // Feed movement data to AnimFacade (replaces direct animator calls)
        UpdateAnimFacade();
    }

    private void Move()
    {
        // Use InputReader instead of Input System callbacks
        Vector2 inputMove = InputReader.MoveInput;

        // player input detected
        if (inputMove != Vector2.zero && !InputReader.inputBusy)
        {
            Vector3 moveDirection = forward * inputMove.y + right * inputMove.x;
            
            // Calculate input magnitude for dynamic speed (0.0 to 1.0)
            // This allows analog stick to control walk/run speed
            float inputMagnitude = Mathf.Clamp01(inputMove.magnitude);
            
            // Don't normalize if we want to preserve analog input magnitude
            // If you want full speed always, uncomment the line below:
            // moveDirection.Normalize();
            
            // If not normalizing, need to clamp moveDirection magnitude
            if (moveDirection.magnitude > 1f)
                moveDirection = moveDirection.normalized;

            // Dynamic speed based on input magnitude
            // Allows walking (slight tilt) vs running (full tilt)
            float dynamicSpeed = speed * inputMagnitude;
            
            // Apply guard speed modifier if guarding
            float maxSpeed = CombatManager.isGuarding ? maxguardSpeed : maxNormalSpeed;
            dynamicSpeed = Mathf.Min(dynamicSpeed, maxSpeed);
            
            // Move horizontally with dynamic speed
            Vector3 horizontalMovement = moveDirection * dynamicSpeed;

            if (horizontalMovement.magnitude > 0.001f)
            {
                // Set horizontal movement (don't accumulate, or it will explode!)
                currentMovement.x = horizontalMovement.x;
                currentMovement.z = horizontalMovement.z;
            }

            // Rotate player to face movement direction if enabled
            if (shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
            }
        }

        // no player input detected
        else
        {
            // Apply friction when no input is detected
            currentMovement.x = Mathf.Lerp(currentMovement.x, 0, friction * Time.deltaTime);
            currentMovement.z = Mathf.Lerp(currentMovement.z, 0, friction * Time.deltaTime);
        }
    }

    private void OnJump()
    {
        // checks to see if the player can jump or double jump
        if (characterController.isGrounded)
        {
            Debug.Log("Grounded Jumped");
            currentMovement.y = jumpForce;

            canDoubleJump = false;
            StartCoroutine(CanDoubleJump());

            // Trigger jump via whichever animation system is present
            if (animFacade != null) animFacade.RequestJump();
            else if (animator != null) { animator.SetBool("jumpTrigger", true); animator.SetBool("isGrounded", false); }
        }
        else if (canDoubleJump)
        {
            Debug.Log("Double Jumped");
            currentMovement.y += doubleJumpForce;

            if (animFacade != null) animFacade.RequestJump();
        }
    }

    private void OnDash()
    {
       if (canDash && !InputReader.inputBusy)
        {
            canDash = false;
            // Don't set inputBusy here - let the dash coroutine handle movement directly
            // InputReader.inputBusy = true; // REMOVED - this blocks normal movement

            // Notify aerial combo manager if in air
            if (!isGrounded && aerialComboManager != null)
            {
                bool dashAllowed = aerialComboManager.TryAirDash();
                if (!dashAllowed)
                {
                    Debug.Log("Air dash already used - cannot dash again");
                    // Still allow dash, but aerial attacks won't reset
                }
            }

            // Use the current horizontal movement for dash direction
            Vector3 dashDirection = (forward * InputReader.MoveInput.y) + (right * InputReader.MoveInput.x);
            dashDirection.Normalize();

            if (dashDirection.magnitude < 0.1f) // If not moving, dash forward
            {
                dashDirection = transform.forward;
            }

            // Drive dash state via animation system
            if (animFacade != null)
            {
                animFacade.RequestDash();
                // Lock movement so attacks can't interrupt dash
                animFacade.LockMovementOn();
            }
            else if (animator != null)
            {
                animator.SetBool("dashTrigger", true);
            }

            StartCoroutine(DashCoroutine(dashDirection));
        }
    }

    private IEnumerator CanDoubleJump()
    {
        if (!canDoubleJump)
        {
            yield return new WaitForSeconds(.1f);
            canDoubleJump = true;
        }
        else
        {
            yield return new WaitForSeconds(.01f);
            canDoubleJump = false;
        }
    }

    private IEnumerator DashCoroutine(Vector3 direction)
    {
        float starttime = Time.time;

        yield return null;

        // AnimFacade handles dash trigger, no need to reset manually
        if (animator != null && animFacade == null)
        {
            // Fallback: only reset if using direct animator
            animator.SetBool("dashTrigger", false);
        }

        Debug.Log($"[Dash] Starting dash in direction: {direction}, duration: {dashTime}s, speed: {dashSpeed}");

        while (Time.time < starttime + dashTime)
        {
            // Move using CharacterController directly (bypasses normal movement lock)
            characterController.Move(direction * dashSpeed * Time.deltaTime);

            yield return null;
        }

        Debug.Log("[Dash] Dash complete, unlocking movement");

        // Unlock movement at the end of dash
        if (animFacade != null)
        {
            animFacade.LockMovementOff();
        }

        yield return new WaitForSeconds(dashCoolDown);
        canDash = true;
    }

    private void AerialAttackHop()
    {
        if (characterController.isGrounded) return;

        currentMovement.y = airAttackHopForce;
    }

    private void ApplyMovement()
    {
        // apply gravity (add gravity, not subtract, since gravity is already negative)
        currentMovement.y += gravity * Time.deltaTime;
        currentMovement.y = Mathf.Clamp(currentMovement.y, -terminalVelocity, terminalVelocity);

        // apply max running speed
        Vector3 horizontalMovement = new Vector3(currentMovement.x, 0, currentMovement.z);
        if (horizontalMovement.magnitude > maxRunningSpeed)
        {
            horizontalMovement = horizontalMovement.normalized * maxRunningSpeed;
            currentMovement.x = horizontalMovement.x;
            currentMovement.z = horizontalMovement.z;
        }

        // move the character controller
        characterController.Move(currentMovement * Time.deltaTime);

        // reset vertical movement when grounded
        if (characterController.isGrounded && currentMovement.y < 0)
        {
            // Use very small negative value to keep player attached to ground
            // Prevents animator from thinking player is "falling" (-1f was too much)
            currentMovement.y = -0.1f;
        }
    }

    /// <summary>
    /// Feed movement data to AnimFacade for proper animation syncing.
    /// This replaces all direct animator.SetBool() calls.
    /// </summary>
    [Header("Animation Debug")]
    [SerializeField] private bool showAnimationDebug = false;
    
    private void UpdateAnimFacade()
    {
        if (animFacade != null)
        {
            // Calculate current speed (horizontal magnitude)
            Vector3 horizontalMovement = new Vector3(currentMovement.x, 0, currentMovement.z);
            float currentSpeed = horizontalMovement.magnitude;

            // Debug logging to verify animation values
            if (showAnimationDebug)
            {
                Debug.Log($"[AnimFacade] Speed={currentSpeed:F2}, Grounded={isGrounded}, VertSpeed={currentMovement.y:F2}, InputBusy={InputReader.inputBusy}");
            }

            // Feed all movement data to AnimFacade
            // AnimFacade handles: moving, isGrounded, verticalSpeed, etc.
            animFacade.FeedMovement(currentSpeed, isGrounded, currentMovement.y);
        }
        else
        {
            if (showAnimationDebug)
            {
                Debug.LogWarning("[EnhancedPlayerMovement] AnimFacade reference is NULL! Animations won't play.");
            }
            
            if (animator != null)
            {
                // Fallback: Direct animator access (not recommended, but prevents breaking old setup)
                Vector2 inputMove = InputReader.MoveInput;
                animator.SetBool("moving", inputMove != Vector2.zero && !InputReader.inputBusy);
                animator.SetBool("isGrounded", isGrounded);
                
                if (showAnimationDebug)
                {
                    Debug.Log($"[Direct Animator] Moving={inputMove != Vector2.zero}, Grounded={isGrounded}");
                }
            }
            else if (showAnimationDebug)
            {
                Debug.LogError("[EnhancedPlayerMovement] No Animator component found! Can't play animations.");
            }
        }
    }

    // Debug visualization for ground check
    private void OnDrawGizmosSelected()
    {
        if (characterController == null) return;

        // Draw ground check box
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 boxCenter = transform.position + (Vector3.down * (maxDistance > 0 ? maxDistance : 0.1f));
        Gizmos.DrawWireCube(boxCenter, boxSize);
        
        // Draw a line showing the check distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, boxCenter);
    }
}
