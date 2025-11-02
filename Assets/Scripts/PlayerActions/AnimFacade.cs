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
    static readonly int DashTrigH     = Animator.StringToHash("Dash");
    static readonly int CanDashH      = Animator.StringToHash("CanDash");

    // runtime
    bool movementLocked;
    float lastSpeed;
    bool grounded;

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
        anim.SetFloat(SpeedH, speed);
        anim.SetBool(IsGroundedH, isGrounded);
        anim.SetFloat(VertSpeedH, verticalSpeed);

        // StartMove trigger on "begin moving"
        if (lastSpeed <= startMoveThreshold && speed > startMoveThreshold)
            anim.SetTrigger(StartMoveH);

        // Landing detection for air-jump reset & jump buffer
        if (!grounded && isGrounded) OnLanded();

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
            anim.ResetTrigger(JumpTrigH);
            anim.SetTrigger(JumpTrigH); // AnyState -> Jump_Start (ground rule)
        }
        else
        {
            // Air jump available?
            if (airJumps > 0)
            {
                airJumps--;
                anim.ResetTrigger(JumpTrigH);
                anim.SetTrigger(JumpTrigH); // AnyState -> AirJump_Start (air rule)
            }
            else
            {
                // optional pre-landing buffer
                jumpBuffered = true;
                jumpBufferTimer = preLandingJumpBuffer;
            }
        }
    }

    public void RequestDash()
    {
        if (!canSpecial || !dashReady) return;
        anim.SetBool(CanDashH, true);
        anim.SetTrigger(DashTrigH);
        StartCoroutine(DashCooldown());
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

        if (jumpBuffered)
        {
            jumpBuffered = false;
            RequestJump(); // auto-consume buffered jump
        }
    }

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
    }

    // --- Chain window / cancelability ---
    public void OpenChainWindow()   { anim.SetBool(CanChainH, true); }
    public void EnableCancel()      { anim.SetBool(CanChainH, true); }  // if you used Cancelable instead, swap param name
    public void CloseChainWindow()
    {
        anim.SetBool(CanChainH, false);
        anim.SetBool(BufferedXH, false);
        anim.SetBool(BufferedYH, false);
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
    public void LockMovementOn()  { movementLocked = true; }
    public void LockMovementOff() { movementLocked = false; }

    // --- Specials gating (optional, call from clips during non-cancelable windows) ---
    public void AllowSpecials()   { canSpecial = true; }
    public void ForbidSpecials()  { canSpecial = false; }
}
