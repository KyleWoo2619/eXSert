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
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities.Combat.Attacks;

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
    [SerializeField, CriticalReference] private AnimFacade animFacade;
    [SerializeField] private AerialComboManager aerialComboManager;
    [Tooltip("Fallback: Only used if AnimFacade is null (not recommended)")]
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField, CriticalReference] private InputActionReference _jumpAction;
    [SerializeField, CriticalReference] private InputActionReference _dashAction;

    [Header("Player Movement Settings")]
    [Tooltip("DEPRECATED: Use walk/run speeds instead. This value is no longer used.")]
    [SerializeField] internal float speed; // Kept for backwards compatibility, not used
    [Tooltip("Walk speed (locked value for animation matching)")]
    [SerializeField] private float walkSpeed = 2.5f;
    [Tooltip("Run speed (locked value for animation matching)")]
    [SerializeField] private float runSpeed = 5.0f;
    [Tooltip("Input threshold to switch from walk to run (0-1)")]
    [SerializeField, Range(0.3f, 0.8f)] private float walkToRunThreshold = 0.5f;
    [Tooltip("Maximum speed when guarding")]
    [SerializeField] private float maxguardSpeed = 2.5f;
    [SerializeField, Range(0f, 20f)] private float friction = 3f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool shouldFaceMoveDirection = true;
    
    [Header("Speed Transitions")]
    [Tooltip("How fast speed snaps between walk/run (higher = snappier)")]
    [SerializeField, Range(5f, 30f)] private float speedTransitionSpeed = 15f;
    [Tooltip("How fast animation blend changes (0.1 walk to 1.0 run). Lower = smoother. 0.05-0.1 recommended.")]
    [SerializeField, Range(0.01f, 0.5f)] private float animationBlendSpeed = 0.1f;

    internal Vector3 currentMovement = Vector3.zero;
    private float currentSpeed = 0f; // Actual smoothed speed value used for movement
    private bool isRunning = false; // Track if currently in run mode
    private float currentAnimationSpeed = 0f; // Smoothed animation blend value (0.1 to 1.0)

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

    [Header("Plunge Attack Settings")]
    [Tooltip("Brief pause/hover before plunging (for dramatic effect)")]
    [SerializeField, Range(0f, 0.3f)] private float plungeHoverTime = 0.08f;
    [Tooltip("Strong downward velocity applied after hover")]
    [SerializeField, Range(10f, 50f)] private float plungeDownSpeed = 26f;
    [Tooltip("Horizontal movement damping during plunge (0=stop, 1=full control)")]
    [SerializeField, Range(0f, 1f)] private float plungeHorizontalDampen = 0.35f;
    [Tooltip("Minimal input lock window during plunge start")]
    [SerializeField, Range(0f, 0.5f)] private float plungeLockSeconds = 0.20f;

    [Header("Camera Settings")]
    [SerializeField] bool invertYAxis = false;

    private float maxRunningSpeed => CombatManager.isGuarding ? maxguardSpeed : runSpeed;

    private bool canDash = true;
    private bool wasGroundedLastFrame = false;
    
    // Plunge attack state
    private bool isPlunging = false;
    private float plungeTimer = 0f;
    private float plungeInputUnlockTimer = 0f;

    public bool IsPlunging => isPlunging;

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
        EnhancedPlayerAttackManager.OnAttack += AerialAttackHop;
    }

    private void OnDisable()
    {
        // Unsubscribe
        EnhancedPlayerAttackManager.OnAttack -= AerialAttackHop;
    }

    // Update is called once per frame
    
    public void Update()
    {
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
            float inputMagnitude = Mathf.Clamp01(inputMove.magnitude);
            
            // If not normalizing, need to clamp moveDirection magnitude
            if (moveDirection.magnitude > 1f)
                moveDirection = moveDirection.normalized;

            // TWO-SPEED SYSTEM: Walk OR Run (no blend)
            // Threshold determines which speed to use
            float maxSpeed = CombatManager.isGuarding ? maxguardSpeed : runSpeed;
            
            // Determine target speed based on input threshold
            float targetSpeed;
            if (inputMagnitude >= walkToRunThreshold)
            {
                // Run mode
                targetSpeed = maxSpeed;
                isRunning = true;
            }
            else
            {
                // Walk mode
                targetSpeed = walkSpeed;
                isRunning = false;
            }
            
            // Quickly snap to target speed (locked speeds for animation matching)
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedTransitionSpeed * Time.deltaTime);
            
            // Move horizontally with locked speed
            Vector3 horizontalMovement = moveDirection * currentSpeed;

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
            // Quickly decelerate to stop
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, speedTransitionSpeed * Time.deltaTime);
            isRunning = false;
            
            // Apply friction when no input is detected
            currentMovement.x = Mathf.Lerp(currentMovement.x, 0, friction * Time.deltaTime);
            currentMovement.z = Mathf.Lerp(currentMovement.z, 0, friction * Time.deltaTime);
        }
    }

    private void OnJump()
    {
        // Let AnimFacade handle all jump logic (air jump counter, triggers, etc.)
        // This just applies the physics force based on grounded state
        
        if (characterController.isGrounded)
        {
            Debug.Log("Grounded Jumped");
            currentMovement.y = jumpForce;

            // Trigger jump via AnimFacade (handles ground jump trigger)
            if (animFacade != null) animFacade.RequestJump();
            else if (animator != null) { animator.SetBool("jumpTrigger", true); animator.SetBool("isGrounded", false); }
        }
        else
        {
            // Air jump - let AnimFacade check if allowed (it has the air jump counter)
            if (animFacade != null)
            {
                // Check if air jump is available via AnimFacade
                // AnimFacade will handle the trigger and counter internally
                bool hadAirJumps = animFacade.HasAirJumps();
                
                animFacade.RequestJump(); // This decrements counter if available
                
                // Only apply vertical force if air jump was actually allowed
                if (hadAirJumps)
                {
                    Debug.Log("Double Jumped - physics applied");
                    currentMovement.y = doubleJumpForce; // Replace vertical velocity, don't add
                }
                else
                {
                    Debug.Log("Double Jump blocked - no air jumps left");
                }
            }
            else if (animator != null && canDoubleJump)
            {
                // Fallback for direct animator (legacy)
                Debug.Log("Double Jumped");
                currentMovement.y += doubleJumpForce;
            }
        }
    }

    private void OnDash()
    {
       if (canDash && !InputReader.inputBusy)
        {
            canDash = false;

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

            // INSTANT: Play dash animation directly, bypassing transitions
            if (animator != null)
            {
                // CrossFade with 0.0 blend = instant switch to Dash_Forward state
                animator.CrossFade("Dash_Forward", 0.0f, 0);
                Debug.Log("[Dash] Playing Dash_Forward animation directly (instant)");
            }
            else if (animFacade != null)
            {
                // Fallback: use AnimFacade if no direct animator reference
                animFacade.RequestDash();
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

        // Wait one frame for animator to register the Dash trigger
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
            // Move using CharacterController directly (bypasses normal movement)
            characterController.Move(direction * dashSpeed * Time.deltaTime);

            yield return null;
        }

        Debug.Log("[Dash] Dash complete");

        // Cooldown before allowing next dash
        yield return new WaitForSeconds(dashCoolDown);
        canDash = true;
    }

    private void AerialAttackHop(PlayerAttack attack)
    {
        if (characterController.isGrounded) return;

        // Don't apply hop if plunging (plunge attack should drop, not hop)
        if (isPlunging)
        {
            Debug.Log("[EnhancedPlayerMovement] Plunge active - skipping aerial hop");
            return;
        }

        // Normal aerial attack hop (X button attacks)
        currentMovement.y = airAttackHopForce;
        Debug.Log($"[EnhancedPlayerMovement] Aerial hop applied: {airAttackHopForce}");
    }

    /// <summary>
    /// Initiates plunge attack - brief hover, then strong downward drop
    /// </summary>
    public void StartPlunge()
    {
        if (isPlunging) return; // Already plunging

        isPlunging = true;
        plungeTimer = plungeHoverTime;
        plungeInputUnlockTimer = plungeLockSeconds;
        
        // Kill upward velocity and damp horizontal movement
        currentMovement.y = Mathf.Min(0f, currentMovement.y);
        currentMovement.x *= plungeHorizontalDampen;
        currentMovement.z *= plungeHorizontalDampen;
        
        Debug.Log($"[EnhancedPlayerMovement] Plunge initiated - hover for {plungeHoverTime}s");
    }

    /// <summary>
    /// Stops plunge attack (called on landing)
    /// </summary>
    private void StopPlunge()
    {
        if (!isPlunging) return;

        isPlunging = false;
        plungeTimer = 0f;
        plungeInputUnlockTimer = 0f;
        currentMovement.y = 0f;
        
        Debug.Log("[EnhancedPlayerMovement] Plunge ended - landed");
    }

    private void ApplyMovement()
    {
        // Handle plunge attack physics
        if (isPlunging)
        {
            // Update plunge timer
            if (plungeTimer > 0f)
            {
                // Hover phase - freeze vertical movement
                plungeTimer -= Time.deltaTime;
                currentMovement.y = 0f;
            }
            else
            {
                // Drop phase - apply strong downward force
                currentMovement.y = -plungeDownSpeed;
            }

            // Count down input lock
            if (plungeInputUnlockTimer > 0f)
                plungeInputUnlockTimer -= Time.deltaTime;
        }
        else
        {
            // Normal gravity
            currentMovement.y += gravity * Time.deltaTime;
        }

        // Clamp to terminal velocity
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
            // Stop plunge on landing and notify aerial system
            if (isPlunging)
            {
                StopPlunge();
                
                // Reset aerial combo for next airtime
                if (aerialComboManager != null)
                    aerialComboManager.OnLanded();
            }

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
            float currentSpeedMagnitude = horizontalMovement.magnitude;

            // TWO-SPEED SYSTEM: Calculate target animation speed
            // 0.1 = walk (triggers locomotion), 1.0 = run
            // Must be > 0.01 to trigger locomotion in AnimFacade
            float targetAnimationSpeed;
            
            if (currentSpeedMagnitude > 0.1f) // Moving
            {
                if (isRunning)
                {
                    targetAnimationSpeed = 1.0f; // Run animation (threshold 1.0)
                }
                else
                {
                    targetAnimationSpeed = 0.1f; // Walk animation (threshold 0.0, but > 0.01 to trigger locomotion)
                }
            }
            else // Stopped or nearly stopped
            {
                targetAnimationSpeed = 0f; // Below threshold, stops locomotion
            }

            // Smoothly lerp animation speed for gradual transition (0.1 to 1.0 over time)
            // Using time-based lerp for consistent transition regardless of framerate
            float lerpSpeed = 1f / animationBlendSpeed; // Convert time to lerp speed
            currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, targetAnimationSpeed, lerpSpeed * Time.deltaTime);

            // Debug logging to verify animation values
            if (showAnimationDebug)
            {
                float inputMag = InputReader.MoveInput.magnitude;
                Debug.Log($"[AnimFacade] Input={inputMag:F2}, Speed={currentSpeedMagnitude:F2}, AnimSpeed={currentAnimationSpeed:F2} (Target={targetAnimationSpeed:F2}) ({(isRunning ? "RUN" : "WALK")}), Grounded={isGrounded}");
            }

            // Feed smoothed animation speed to AnimFacade for gradual blend tree transition
            // Smoothly transitions between 0.1 (walk) and 1.0 (run) over animationBlendSpeed seconds
            animFacade.FeedMovement(currentAnimationSpeed, isGrounded, currentMovement.y);
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
