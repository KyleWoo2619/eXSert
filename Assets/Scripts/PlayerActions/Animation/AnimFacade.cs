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
    
    [Header("Movement Sounds")]
    [SerializeField, Tooltip("ScriptableObject containing footstep, jump, and landing sound arrays")]
    private PlayerMovementSounds movementSounds;

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

    // Guard system hashes
    static readonly int GuardingH     = Animator.StringToHash("Guarding");
    static readonly int ParryH        = Animator.StringToHash("Parry");
    static readonly int ParrySuccessH = Animator.StringToHash("ParrySuccess");

    // State hashes for code-driven playback
    static readonly int LocomotionStateH = Animator.StringToHash("BT_Locomotion_Normal");
    static readonly int IdleStateH       = Animator.StringToHash("ST_Idle_WC"); // Or "AOE_Idle_WC" depending on stance

    // runtime
    bool movementLocked;
    float lastSpeed;
    bool grounded;
    bool wasInAir; // Track if we were airborne (for automatic Jump_Land)
    bool isInLocomotion; // Track if we manually started locomotion
    int prevStance = 0; // Store stance before entering guard
    bool parryWindowOpen = false; // Track if parry i-frames are active

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

        // === CODE-DRIVEN LOCOMOTION (Skip Walk_Windup) ===
        var currentState = anim.GetCurrentAnimatorStateInfo(0);
        
        // Start locomotion when: grounded + speed > 0.01 + not locked
        bool shouldBeInLocomotion = isGrounded && speed > 0.01f && !movementLocked;
        
        // Check what state we're currently in
        bool inJumpState = currentState.IsName("Jump_Start") || currentState.IsName("AirJump_Start") 
                        || currentState.IsName("Jump_AirLoop") || currentState.IsName("Jump_Land");
        bool inDashState = currentState.IsName("Dash_Forward");
        bool inAttackState = currentState.IsName("SX1") || currentState.IsName("SX2") || currentState.IsName("SX3") || currentState.IsName("SX4")
                          || currentState.IsName("AX1") || currentState.IsName("AX2") || currentState.IsName("AX3") || currentState.IsName("AX4")
                          || currentState.IsName("SY1") || currentState.IsName("SY2") || currentState.IsName("SY3") || currentState.IsName("SY4")
                          || currentState.IsName("AY1") || currentState.IsName("AY2") || currentState.IsName("AY3") || currentState.IsName("AY4");
        bool inIdleState = currentState.IsName("ST_Idle_WC") || currentState.IsName("AOE_Idle_WC") 
                        || currentState.IsName("ST_Idle_Combat") || currentState.IsName("AOE_Idle_Combat");
        
        // SNAP into locomotion when moving (interrupt idle/land, but respect jumps/dash/attacks)
        if (shouldBeInLocomotion && !isInLocomotion)
        {
            // Cancel idle and landing animations immediately when moving
            if (inIdleState || currentState.IsName("Jump_Land"))
            {
                anim.CrossFade("BT_Locomotion_Normal", 0.05f, 0); // Very fast transition (snap)
                isInLocomotion = true;
                Debug.Log("[AnimFacade] SNAP to locomotion - interrupting idle/landing");
            }
            // Start locomotion after jump/dash/attack finishes naturally
            else if (!inJumpState && !inDashState && !inAttackState)
            {
                anim.CrossFade("BT_Locomotion_Normal", 0.15f, 0);
                isInLocomotion = true;
                Debug.Log("[AnimFacade] Started locomotion (code-driven) - playing BT_Locomotion_Normal");
            }
        }
        // Stop locomotion when player stops moving or enters other priority states
        else if (isInLocomotion && (!shouldBeInLocomotion || inJumpState || inDashState || inAttackState))
        {
            isInLocomotion = false;
            // Let animator naturally transition to idle when Speed drops to 0
            Debug.Log("[AnimFacade] Stopped locomotion (returning to idle/other state)");
        }

        // === AUTOMATIC JUMP ANIMATION HANDLING (Code-Driven) ===
        
        // Player just became airborne (jump or fall off ledge) → play Jump_AirLoop
        if (grounded && !isGrounded)
        {
            wasInAir = true;
            // Only auto-play AirLoop if we're NOT already in a jump state
            bool inAnyJumpState = currentState.IsName("Jump_Start") || currentState.IsName("AirJump_Start") 
                               || currentState.IsName("Jump_AirLoop") || currentState.IsName("Jump_Land");
            
            if (!inAnyJumpState)
            {
                // Player fell off ledge (didn't jump) - play Jump_AirLoop directly
                anim.CrossFade("Jump_AirLoop", 0.1f, 0);
                Debug.Log("[AnimFacade] Became airborne (fall) - playing Jump_AirLoop");
            }
        }
        
        // Player just landed → play Jump_Land (but only if we're airborne and NOT already landing)
        if (!grounded && isGrounded && wasInAir)
        {
            // Only play Jump_Land if not already in it (prevents loop)
            bool alreadyLanding = currentState.IsName("Jump_Land");
            if (!alreadyLanding)
            {
                wasInAir = false;
                anim.CrossFade("Jump_Land", 0.05f, 0);
                Debug.Log("[AnimFacade] Landed - playing Jump_Land");
                OnLanded(); // Reset air jumps, etc.
            }
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
            
            // Play jump sound (gasp/grunt)
            PlayJumpSound();
            
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
                
                // Play jump sound for air jump too
                PlayJumpSound();
                
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
        
        // Play dash sound
        PlayDashSound();
        
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
    //  GUARD SYSTEM
    // =========================================================================

    /// <summary>Called when guard button is pressed (hold). Enters Guard state.</summary>
    public void StartGuard()
    {
        // Remember current stance so we can return to it
        prevStance = anim.GetInteger(StanceH);
        
        // Debug: Check current animator state on layer 0 (Base Layer)
        AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
        string stateName = "";
        foreach (AnimatorClipInfo clipInfo in anim.GetCurrentAnimatorClipInfo(0))
        {
            stateName = clipInfo.clip.name;
            break;
        }
        
        Debug.Log($"[AnimFacade] StartGuard called - Current clip: '{stateName}', State hash: {currentState.shortNameHash}, InCombat: {anim.GetBool(InCombatH)}, Stance: {prevStance}");
        
        // Freeze locomotion while guard is up (player can't move while guarding)
        LockMovementOn();
        
        // Set Guarding bool (for Guard_Idle loop and exit condition)
        anim.SetBool(GuardingH, true);
        
        // Trigger Parry (for Any State → Guard_Up transition with parry window)
        anim.SetTrigger(ParryH);
        
        // Debug: Verify parameters were set
        bool guardingValue = anim.GetBool(GuardingH);
        Debug.Log($"[AnimFacade] Guard started - Guarding={guardingValue}, Parry trigger set, locked movement");
    }

    /// <summary>Called when guard button is released. Exits Guard state.</summary>
    public void StopGuard()
    {
        // Leave Guard sub-state machine
        anim.SetBool(GuardingH, false);
        
        // Restore stance we came from (ST or AOE)
        anim.SetInteger(StanceH, prevStance);
        
        // Force transition to idle of the correct stance
        // This ensures smooth return regardless of where we came from
        string targetIdle = prevStance == 0 ? "ST_Idle_WC" : "AOE_Idle_WC";
        anim.CrossFade(targetIdle, 0.15f, 0);
        
        // Unlock movement
        LockMovementOff();
        
        // Close parry window if it was left open
        if (parryWindowOpen)
        {
            CloseParryWindow();
        }
        
        Debug.Log($"[AnimFacade] Guard stopped - restored stance {prevStance}, transitioning to {targetIdle}, unlocked movement");
    }

    /// <summary>Called by gameplay when a perfect parry happens. Triggers special exit.</summary>
    public void OnParrySuccess()
    {
        // Close parry window (brief invincibility ends)
        CloseParryWindow();
        
        // Trigger special parry success transition (Guard → Combat)
        anim.SetTrigger(ParrySuccessH);
        
        // Exit guard state
        anim.SetBool(GuardingH, false);
        
        // Restore stance
        anim.SetInteger(StanceH, prevStance);
        
        // Mark in-combat to keep aggressive camera/FX (THIS triggers combat mode)
        MarkInCombat();
        
        // Force transition to idle (with combat active now)
        string targetIdle = prevStance == 0 ? "ST_Idle_WC" : "AOE_Idle_WC";
        anim.CrossFade(targetIdle, 0.1f, 0);
        
        // Unlock movement
        LockMovementOff();
        
        Debug.Log($"[AnimFacade] Parry success! Restored stance {prevStance}, marked InCombat, transitioning to {targetIdle}");
    }

    // =========================================================================
    //  ANIMATION EVENTS (Guard System)
    // =========================================================================

    /// <summary>Animation Event: Place on Guard_Up clip where parry window starts.</summary>
    public void OpenParryWindow()
    {
        parryWindowOpen = true;
        // If you have a combat manager, toggle i-frames here:
        // CombatManager.SetInvincible(true);
        Debug.Log("[AnimFacade] Parry window OPENED - i-frames active");
    }

    /// <summary>Animation Event: Place on Guard_Up clip where parry window ends.</summary>
    public void CloseParryWindow()
    {
        parryWindowOpen = false;
        // CombatManager.SetInvincible(false);
        Debug.Log("[AnimFacade] Parry window CLOSED - i-frames inactive");
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

        // Play landing sound
        PlayLandingSound();

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

    // =========================================================================
    //  ANIMATION EVENT SINKS - MOVEMENT SOUNDS
    // =========================================================================
    
    /// <summary>
    /// Animation Event: Place on walk/run animation frames where foot contacts ground.
    /// Plays a random footstep sound from the movement sounds bank.
    /// </summary>
    public void PlayFootstepSound()
    {
        if (movementSounds != null)
        {
            movementSounds.PlayRandomFootstep();
        }
    }

    /// <summary>
    /// Animation Event: Place on jump animation start frame.
    /// Plays a random jump sound (gasp/grunt) from the movement sounds bank.
    /// </summary>
    public void PlayJumpSound()
    {
        if (movementSounds != null)
        {
            movementSounds.PlayRandomJump();
        }
    }

    /// <summary>
    /// Called when player lands (automatically from OnLanded or animation event).
    /// Plays a random landing sound from the movement sounds bank.
    /// </summary>
    public void PlayLandingSound()
    {
        if (movementSounds != null)
        {
            movementSounds.PlayRandomLanding();
        }
    }

    /// <summary>
    /// Animation Event: Place on dash animation start frame.
    /// Plays a random dash sound from the movement sounds bank.
    /// </summary>
    public void PlayDashSound()
    {
        if (movementSounds != null)
        {
            movementSounds.PlayRandomDash();
        }
    }
}
