using UnityEngine;
using System.Collections;

/// <summary>
/// One-stop animation driver + animation-event sink.
/// Wire this to your player controller and call the public methods.
/// Requires: Animator on the same GameObject.
/// </summary>
public class AnimFacade : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator anim;
    [SerializeField] CharacterController characterController; // or your own mover (optional)

    [Header("Locomotion Feed")]
    [SerializeField, Range(0f, 0.3f)] float startMoveThreshold = 0.08f;  // when to fire StartMove
    [SerializeField, Range(0f, 0.3f)] float stopThreshold      = 0.01f;  // same as animator stop checks

    [Header("Combat")]
    [SerializeField] float inCombatHoldSeconds = 6.0f; // 5~7s
    float inCombatTimer = -1f;

    [Header("Dash")]
    [SerializeField] float dashCooldown = 0.6f;
    bool dashReady = true;

    [Header("Jump / Air Jumps")]
    [SerializeField] int   maxAirJumps = 1;
    [SerializeField] float preLandingJumpBuffer = 0.1f; // optional
    int   airJumps;
    bool  jumpBuffered;
    float jumpBufferTimer;

    [Header("Cancel & Specials Gates")]
    [SerializeField] bool canSpecial = true; // master gate (you can drive from gameplay too)
    [SerializeField, Tooltip("When movement is locked by attack events, freeze animator Speed to prevent locomotion transitions interrupting attacks")]
    bool freezeLocomotionWhenLocked = true;

    [Header("Debug")]
    public int comboStageDebug;

    // Animator hashes
    static readonly int StanceH       = Animator.StringToHash("Stance");
    static readonly int InCombatH     = Animator.StringToHash("InCombat");
    static readonly int SpeedH        = Animator.StringToHash("Speed");
    static readonly int IsGroundedH   = Animator.StringToHash("IsGrounded");
    static readonly int VertSpeedH    = Animator.StringToHash("VertSpeed");

    static readonly int InputXH       = Animator.StringToHash("InputX");
    static readonly int InputYH       = Animator.StringToHash("InputY");
    static readonly int BufferedXH    = Animator.StringToHash("BufferedX");
    static readonly int BufferedYH    = Animator.StringToHash("BufferedY");
    static readonly int CanChainH     = Animator.StringToHash("CanChain");      // or "Cancelable" if you used that
    static readonly int ComboStageH   = Animator.StringToHash("ComboStage");

    static readonly int StartMoveH    = Animator.StringToHash("StartMove");
    static readonly int ForceIdleH    = Animator.StringToHash("ForceIdle");

    static readonly int JumpTrigH     = Animator.StringToHash("Jump");
    static readonly int DJumpTrigH    = Animator.StringToHash("DJump");
    static readonly int DashTrigH     = Animator.StringToHash("Dash");
    static readonly int CanDashH      = Animator.StringToHash("CanDash");
    static readonly int AirJumpsH     = Animator.StringToHash("AirJumps");

    // runtime
    bool movementLocked;
    float lastSpeed;
    bool grounded;
    bool wasInAir; // Track if we were airborne (for automatic Jump_Land)

    void Reset()
    {
        anim = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        // Sanity check: warn if multiple Animators exist on the rig
        var anims = GetComponentsInChildren<Animator>(true);
        if (anims != null && anims.Length > 1)
        {
            Debug.LogWarning($"[AnimFacade] Multiple Animators found under {name}: {anims.Length}. Ensure only one controls the Avatar to avoid state fights.");
        }
        airJumps = maxAirJumps;
        // Sync air jumps to Animator parameter at start
        anim.SetInteger(AirJumpsH, airJumps);
        anim.SetBool(CanDashH, dashReady);
        
        // Initialize stance to 0 (Single Target - basic stance)
        anim.SetInteger(StanceH, 0);
    }

    void Update()
    {
        // --- Update buffered jump small timer (optional) ---
        if (jumpBuffered)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0f) jumpBuffered = false;
        }

        // --- In-combat decay timer ---
        if (inCombatTimer >= 0f)
        {
            inCombatTimer -= Time.deltaTime;
            if (inCombatTimer <= 0f)
            {
                anim.SetBool(InCombatH, false);
                inCombatTimer = -1f;
            }
        }

        // --- Movement lock simple stop (for CharacterController users) ---
        if (movementLocked && characterController)
        {
            // Prevent residual motion; your mover may have its own way to zero velocity.
            // Here we only ensure no controller.Move is performed externally.
        }

        // --- Clear consumed triggers to prevent them from firing repeatedly ---
        // This prevents animation restart bugs caused by triggers that aren't consumed
        if (Time.frameCount % 10 == 0) // Every 10 frames, clear old triggers
        {
            // Only reset triggers that should be one-shot events
            anim.ResetTrigger(StartMoveH);
            anim.ResetTrigger(ForceIdleH);
        }
    }

    // =========================================================================
    //  Public FEEDS (call these from your controller each frame / on events)
    // =========================================================================

    /// <summary>Feeds locomotion to animator. Fires StartMove when crossing the threshold.</summary>
    public void FeedMovement(float speed, bool isGrounded, float verticalSpeed)
    {
        // Prevent locomotion transitions from stealing control during attacks when movement is locked
        float animSpeed = (freezeLocomotionWhenLocked && movementLocked) ? 0f : speed;
        anim.SetFloat(SpeedH, animSpeed);
        anim.SetBool(IsGroundedH, isGrounded);
        anim.SetFloat(VertSpeedH, verticalSpeed);

        // Debug if Speed is being forced to zero
        if (movementLocked && speed > 0.1f)
        {
            Debug.Log($"[AnimFacade] FeedMovement: Actual speed={speed:F2}, forced to 0 (locked)");
        }

        // StartMove trigger on "begin moving"
        if (lastSpeed <= startMoveThreshold && speed > startMoveThreshold)
            anim.SetTrigger(StartMoveH);

        // === AUTOMATIC JUMP ANIMATION HANDLING (Code-Driven) ===
        
        // Player just became airborne (jump or fall off ledge) → play Jump_AirLoop
        if (grounded && !isGrounded)
        {
            wasInAir = true;
            // Only auto-play AirLoop if we're NOT already in a jump state
            var currentState = anim.GetCurrentAnimatorStateInfo(0);
            bool inJumpState = currentState.IsName("Jump_Start") || currentState.IsName("AirJump_Start") || currentState.IsName("Jump_AirLoop");
            
            if (!inJumpState)
            {
                // Player fell off ledge (didn't jump) - play Jump_AirLoop directly
                anim.CrossFade("Jump_AirLoop", 0.1f, 0);
                Debug.Log("[AnimFacade] Became airborne (fall) - playing Jump_AirLoop");
            }
        }
        
        // Player just landed → play Jump_Land
        if (!grounded && isGrounded && wasInAir)
        {
            wasInAir = false;
            anim.CrossFade("Jump_Land", 0.05f, 0);
            Debug.Log("[AnimFacade] Landed - playing Jump_Land");
            OnLanded(); // Reset air jumps, etc.
        }

        lastSpeed = speed;
        grounded  = isGrounded;
    }

    /// <summary>Set stance: 0=Single, 1=AOE, 2=Guard.</summary>
    public void SetStance(int stance) => anim.SetInteger(StanceH, stance);

    // =========================================================================
    //  INPUT API (hook your input system here)
    // =========================================================================

    public void PressLight()  => HandleAttackInput(isHeavy:false);
    public void PressHeavy()  => HandleAttackInput(isHeavy:true);

    void HandleAttackInput(bool isHeavy)
    {
        // If chain window is open, buffer; otherwise fire opener triggers.
        if (anim.GetBool(CanChainH))
        {
            if (isHeavy) anim.SetBool(BufferedYH, true);
            else         anim.SetBool(BufferedXH, true);
        }
        else
        {
            if (isHeavy) anim.SetTrigger(InputYH);
            else         anim.SetTrigger(InputXH);
        }

        MarkInCombat(); // refresh timer
    }

    public void RequestJump()
    {
        if (!canSpecial) return;

        if (grounded)
        {
            // Ground jump - play Jump_Start directly (instant, no transitions)
            anim.CrossFade("Jump_Start", 0.0f, 0);
            Debug.Log("[AnimFacade] Ground jump - playing Jump_Start directly");
            
            // Auto-transition to Jump_AirLoop after Jump_Start clip finishes
            StartCoroutine(TransitionToAirLoopAfterClip("Jump_Start"));
        }
        else
        {
            // Air jump available?
            if (airJumps > 0)
            {
                airJumps--;
                // Sync the Animator parameter
                anim.SetInteger(AirJumpsH, airJumps);
                // Play AirJump_Start directly (instant, no transitions)
                anim.CrossFade("AirJump_Start", 0.0f, 0);
                Debug.Log($"[AnimFacade] Air jump - playing AirJump_Start directly, remaining: {airJumps}");
                
                // Auto-transition to Jump_AirLoop after AirJump_Start clip finishes
                StartCoroutine(TransitionToAirLoopAfterClip("AirJump_Start"));
            }
            else
            {
                // optional pre-landing buffer
                jumpBuffered = true;
                jumpBufferTimer = preLandingJumpBuffer;
                Debug.Log("[AnimFacade] No air jumps left, buffering jump for landing");
            }
        }
    }

    IEnumerator TransitionToAirLoopAfterClip(string clipName)
    {
        // Wait for the jump start clip to finish
        yield return null; // Wait one frame for state to register
        
        var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.IsName(clipName) && stateInfo.normalizedTime < 0.95f)
        {
            yield return null;
            stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        }
        
        // Transition to Jump_AirLoop
        anim.CrossFade("Jump_AirLoop", 0.1f, 0);
        Debug.Log($"[AnimFacade] {clipName} finished - transitioning to Jump_AirLoop");
    }

    public void RequestDash()
    {
        if (!canSpecial) return;
        
        // CRITICAL: Set both Dash trigger AND CanDash bool immediately
        // This ensures AnyState transition fires instantly without waiting
        anim.SetBool(CanDashH, true);
        anim.SetTrigger(DashTrigH);
        
        Debug.Log("[AnimFacade] Dash requested - setting Dash trigger and CanDash=true");
        
        // Start cooldown (CanDash will be false during cooldown to prevent spam)
        if (dashReady)
        {
            StartCoroutine(DashCooldown());
        }
    }

    IEnumerator DashCooldown()
    {
        dashReady = false;
        anim.SetBool(CanDashH, false);
        yield return new WaitForSeconds(dashCooldown);
        dashReady = true;
        anim.SetBool(CanDashH, true);
    }

    // =========================================================================
    //  GAMEPLAY HELPERS
    // =========================================================================

    /// <summary>Call this when ground contact is (re)gained or from Jump_Land event.</summary>
    public void OnLanded()
    {
        airJumps = maxAirJumps;
        // Sync air jumps to Animator when reset on landing
        anim.SetInteger(AirJumpsH, airJumps);
        Debug.Log($"[AnimFacade] Landed - air jumps reset to {airJumps}");

        if (jumpBuffered)
        {
            jumpBuffered = false;
            RequestJump(); // auto-consume buffered jump
        }
    }

    /// <summary>Check if air jumps are available (for external systems).</summary>
    public bool HasAirJumps() => airJumps > 0;

    /// <summary>Refreshes the InCombat bool and timer.</summary>
    public void MarkInCombat()
    {
        anim.SetBool(InCombatH, true);
        inCombatTimer = inCombatHoldSeconds;
    }

    // =========================================================================
    //  ANIMATION EVENT SINKS  (add these by name to clips)
    // =========================================================================

    // --- Combo bookkeeping ---
    public void SetComboStage(int stage)
    {
        comboStageDebug = stage;
        anim.SetInteger(ComboStageH, stage);
        // Clear buffered inputs when combo stage advances (attack is actually starting)
        ClearBufferedInputs();
    }

    // --- Chain window / cancelability ---
    public void OpenChainWindow()
    {
        anim.SetBool(CanChainH, true);
        // Clear any stale buffers when opening a new chain window
        ClearBufferedInputs();
    }
    public void EnableCancel()      { anim.SetBool(CanChainH, true); }  // if you used Cancelable instead, swap param name
    public void CloseChainWindow()
    {
        anim.SetBool(CanChainH, false);
        anim.SetBool(BufferedXH, false);
        anim.SetBool(BufferedYH, false);
        // Unlock movement when chain window closes (attack recovery complete)
        LockMovementOff();
    }
    public void DisableCancel()     { anim.SetBool(CanChainH, false); }

    // --- Forced settle (optional on finishers) ---
    public void ReturnToIdle()
    {
        anim.ResetTrigger(InputXH);
        anim.ResetTrigger(InputYH);
        anim.SetTrigger(ForceIdleH);
        anim.SetInteger(ComboStageH, 1);
        anim.SetBool(BufferedXH, false);
        anim.SetBool(BufferedYH, false);
    }

    // --- Movement lock (called by openers/dash start, released in rewind or near end) ---
    public void LockMovementOn()
    {
        movementLocked = true;
        Debug.Log("[AnimFacade] Movement LOCKED");
    }
    
    public void LockMovementOff()
    {
        movementLocked = false;
        Debug.Log("[AnimFacade] Movement UNLOCKED");
    }

    // --- Specials gating (optional, call from clips during non-cancelable windows) ---
    public void AllowSpecials()   { canSpecial = true; }
    public void ForbidSpecials()  { canSpecial = false; }

    // --- Input buffers (callable by gameplay even while inputBusy) ---
    public void BufferLight() { anim.SetBool(BufferedXH, true); }
    public void BufferHeavy() { anim.SetBool(BufferedYH, true); }
    public void ClearBufferedInputs()
    {
        anim.SetBool(BufferedXH, false);
        anim.SetBool(BufferedYH, false);
    }
}
