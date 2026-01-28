using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Utilities.Combat;

namespace EnemyBehavior.Boss
{
    public enum RoombaForm { DuelistSummoner, CageBull }
    public enum StunType { None, Parry, PillarCollision }

    [System.Serializable]
    public sealed class BossAttackDescriptor
    {
        [Tooltip("Unique name for this attack (used for logs/analytics/parry id).")]
        public string Id;
        [Tooltip("If true, the attack can be parried any time the hitbox is active.")]
        public bool Parryable;
        [Tooltip("Minimum seconds between uses of this attack.")]
        public float Cooldown;
        [Tooltip("Min distance to the player at which this attack is eligible.")]
        public float RangeMin;
        [Tooltip("Max distance to the player at which this attack is eligible.")]
        public float RangeMax;
        
        [Header("Animation Timing")]
        [Tooltip("Reference clip name for windup phase (used to read actual length).")]
        public string WindupClipName;
        [Tooltip("Reference clip name for active phase (used to read actual length).")]
        public string ActiveClipName;
        [Tooltip("Reference clip name for recovery phase (used to read actual length).")]
        public string RecoveryClipName;
        [Tooltip("Speed multiplier for windup animation (1.0 = normal speed).")]
        public float WindupSpeedMultiplier = 1.0f;
        [Tooltip("Speed multiplier for active animation (1.0 = normal speed).")]
        public float ActiveSpeedMultiplier = 1.0f;
        [Tooltip("Speed multiplier for recovery animation (1.0 = normal speed).")]
        public float RecoverySpeedMultiplier = 1.0f;
        
        [Header("Animation Hooks")]
        [Tooltip("Fired when an attack starts winding up (telegraph).")]
        public string AnimatorTriggerOnWindup;
        [Tooltip("Fired at the start of the damage-active frames.")]
        public string AnimatorTriggerOnActive;
        [Tooltip("Fired when the damage window ends.")]
        public string AnimatorTriggerOnRecovery;
        [Tooltip("Set true if this attack requires arms to be deployed before windup.")]
        public bool RequiresArms;
    }

    [System.Serializable]
    public sealed class SidePanel
    {
        [Tooltip("The visual panel mesh that detaches and falls (child of zone collider object)")]
        public GameObject panelVisualMesh;
        public float maxHealth = 100f;
        [HideInInspector] public float currentHealth;
        [HideInInspector] public bool isDestroyed;
        [Tooltip("Damage multiplier when attacking the exposed zone after panel breaks")]
        public float vulnerabilityMultiplier = 2.0f;
        [Tooltip("Time in seconds before destroyed panel despawns (0 = never)")]
        public float destroyedPanelLifetime = 5f;
        [Tooltip("Force applied when panel breaks off")]
        public float breakOffForce = 400f;
        [Tooltip("Optional: Particle effect to spawn when panel breaks")]
        public GameObject breakVFXPrefab;
        [Tooltip("Optional: Simplified convex mesh for falling panel physics (uses MeshFilter mesh if null)")]
        public Mesh fallCollisionMesh;
    }

    public interface IParrySink { void OnParry(string attackId, GameObject player); }

    [RequireComponent(typeof(BossRoombaController))
    , RequireComponent(typeof(NavMeshAgent))]
    public sealed class BossRoombaBrain : MonoBehaviour, IParrySink
    {
        [Header("Component Help")]
        [SerializeField, TextArea(3, 6)] private string inspectorHelp =
            "BossRoombaBrain: high-level boss behavior and attack sequencing.\n" +
            "Attack timings are driven by animation clip lengths × speed multipliers.\n" +
            "Use Animation Events in clips for precise phase transitions.";

        public RoombaForm StartForm = RoombaForm.DuelistSummoner;

        [Header("Attacks")]
        // Base variants are unused - only Left/Right variants are selected
        [HideInInspector] public BossAttackDescriptor BasicSwipe;
        [HideInInspector] public BossAttackDescriptor ArmSweep;
        
        public BossAttackDescriptor BasicSwipeLeft;
        public BossAttackDescriptor BasicSwipeRight;
        public BossAttackDescriptor ArmSweepLeft;
        public BossAttackDescriptor ArmSweepRight;
        public BossAttackDescriptor DashLungeLeft;
        public BossAttackDescriptor DashLungeRight;
        public BossAttackDescriptor DashLungeNoArms;
        public BossAttackDescriptor ArmPoke;
        public BossAttackDescriptor KnockOffSpin;
        public BossAttackDescriptor VacuumSuction;
        public BossAttackDescriptor StaticCharge;
        public BossAttackDescriptor StaticChargeLeft;
        public BossAttackDescriptor StaticChargeRight;
        public BossAttackDescriptor TargetedCharge;

        [Header("Arms Deploy/ Retract")]
        public string ArmsDeployTrigger = "Arms_Deploy";
        public string ArmsRetractTrigger = "Arms_Retract";
        [Tooltip("Set by animation event on Arms_Deploy clip end, or fallback timeout.")]
        public float ArmsDeployTimeoutSeconds = 1.0f;
        [Tooltip("Duration to wait for arms retract animation to complete (increase if animation is getting cut off)")]
        public float ArmsRetractDuration = 0.6f;
        public float ArmsAutoRetractCooldown = 3.0f;

        [Header("Horns Deploy/Retract")]
        [Tooltip("Trigger for raising horns (faceplate lower)")]
        public string HornsRaiseTrigger = "Horns_Raise";
        [Tooltip("Trigger for lowering horns (faceplate raise)")]
        public string HornsLowerTrigger = "Horns_Lower";
        [Tooltip("Duration to wait for horns raise animation to complete")]
        public float HornsRaiseDuration = 0.8f;
        [Tooltip("Duration to wait for horns lower animation to complete")]
        public float HornsLowerDuration = 0.8f;

        [Header("Windows")]
        public float ParryStaggerSeconds = 3.0f;
        public float PillarStunSeconds = 2.0f;
        public float DefensesDownMultiplier = 1.2f;

        [Header("Top Zone/Spin")]
        public bool RequirePlayerOnTopForSpin = true;
        [Tooltip("Min/Max number of pokes before knock-off spin")]
        public Vector2Int PokeCountRange = new Vector2Int(3, 6);
        public float SpinAfterLastPokeDelay = 0.75f;
        [Tooltip("If the mounted state flickers off, continue treating as mounted for this many seconds.")]
        public float MountedGraceSeconds = 0.2f;
        [Tooltip("Force range applied to fling player off during knock-off spin")]
        public Vector2 FlingForceRange = new Vector2(800f, 1200f);

        [Header("Vacuum & Form Change")]
        [Tooltip("Min/Max attack count before vacuum triggers")]
        public Vector2Int AttackCountForVacuumRange = new Vector2Int(8, 12);
        [Tooltip("Position where roomba moves for vacuum attack")]
        public Transform VacuumPosition;
        [Tooltip("Center bounds for checking player is inside arena before raising walls")]
        public Collider ArenaCenterBounds;
        public BossArenaManager ArenaManager;
        [Tooltip("Manages player lifecycle (claims/releases from DontDestroyOnLoad)")]
        public BossScenePlayerManager PlayerManager;

        [Header("Cage Bull Charges")]
        [Tooltip("Speed multiplier during charges")]
        public float ChargeSpeedMultiplier = 2.5f;
        [Tooltip("Overshoot distance past target for targeted charge")]
        public float ChargeOvershootDistance = 5f;
        [Tooltip("Rest time between static charges and targeted charge")]
        public float ChargeRestDuration = 1.5f;
        [Tooltip("Number of static charge cycles before targeted charge")]
        public int StaticChargeCount = 3;

        [Header("Dash Settings")]
        [Tooltip("Speed for dash attacks")]
        public float DashSpeed = 15f;
        [Tooltip("Overshoot distance past player for dashes")]
        public float DashOvershootDistance = 3f;

        [Header("Side Panels")]
        public List<SidePanel> SidePanels = new List<SidePanel>();

        [Header("Animator Integration")]
        [SerializeField] private string ParamIdleIntensity = "IdleIntensity";
        [SerializeField] private string TriggerStunWindup = "Stun_Windup";
        [SerializeField] private string TriggerStunActive = "Stun_Active";
        [SerializeField] private string TriggerStunRecovery = "Stun_Recovery";
        [SerializeField] private string TriggerHornsRaise = "Horns_Raise";
        [SerializeField] private string TriggerHornsLower = "Horns_Lower";
        [SerializeField] private string TriggerDamagedV1 = "Damaged_v1";
        [SerializeField] private string TriggerDamagedV2 = "Damaged_v2";
        [SerializeField] private string TriggerDamagedV3 = "Damaged_v3";
        [Tooltip("Animator layers by name (must match Animator)")]
        [SerializeField] private string LayerNameHitReact = "HitReact";
        [SerializeField] private string LayerNameStun = "Stun";
        [SerializeField] private string LayerNameAttacks = "Attacks";
        [SerializeField] private string LayerNameIdleAdditive = "Idle";

        [Header("Idle Overlay Intensity")]
        public float IdleIntensityMin = 0.9f;
        public float IdleIntensityMax = 1.5f;
        public float MaxIdleSpeedForIntensity = 10f;

        private BossRoombaController ctrl;
        private NavMeshAgent agent;
        private Transform player;
        private RoombaForm form;
        private Coroutine loop;
        private bool alarmDestroyed;
        private Animator animator;
        private BossAnimationEventMediator animMediator;

        private bool playerOnTop;

        private bool armsDeployed;
        private bool armsDeployInProgress;
        private bool armsRetractInProgress;
        private Coroutine armsRetractRoutine;
        private bool cancelArmsRetract; // Flag to cancel retraction in progress

        private bool hornsRaised; // Track if horns are currently raised
        private bool hornsRaiseInProgress;
        private bool hornsLowerInProgress;

        private BossAttackDescriptor currentAttack;
        private Coroutine currentAttackRoutine;

        private readonly Dictionary<string, float> nextReadyTime = new Dictionary<string, float>(16);
        private readonly Queue<string> lastActions = new Queue<string>(8);

        private int topPokeCount;
        private int requiredPokesForSpin;
        private float lastMountedTime;
        private bool hasEverMounted;

        private int attackCounter;
        private int attackThresholdForVacuum;

        private bool isStunned;
        private StunType currentStunType = StunType.None;

        private int hitReactLayer = -1;
        private int stunLayer = -1;
        private int attacksLayer = -1;
        private int idleAdditiveLayer = -1;

        private bool IsPlayerMounted()
        {
            if (player == null) return false;
            return player.IsChildOf(transform);
        }

        private bool IsMountedWithGrace()
        {
            bool mounted = IsPlayerMounted() || playerOnTop;
            if (mounted)
            {
                lastMountedTime = Time.time;
                hasEverMounted = true;
                return true;
            }
            if (!hasEverMounted) return false;
            return (Time.time - lastMountedTime) <= MountedGraceSeconds;
        }

        void Awake()
        {
            ctrl = GetComponent<BossRoombaController>();
            agent = GetComponent<NavMeshAgent>();
            // GetComponentInChildren searches this GameObject and all children
            animator = GetComponentInChildren<Animator>();
            // Mediator must be on same GameObject as Animator (or child of it) for Animation Events
            animMediator = GetComponentInChildren<BossAnimationEventMediator>(true);
            player = GameObject.FindWithTag("Player")?.transform;
            lastMountedTime = -999f;
            hasEverMounted = false;

            CacheAnimatorLayerIndices();

            attackThresholdForVacuum = Random.Range(AttackCountForVacuumRange.x, AttackCountForVacuumRange.y + 1);

            foreach (var panel in SidePanels)
            {
                panel.currentHealth = panel.maxHealth;
                panel.isDestroyed = false;
            }

            InitializeAttackDescriptors();
        }

        private void CacheAnimatorLayerIndices()
        {
            if (animator == null) return;
            
            // CRITICAL FIX: Base Layer should ALWAYS be layer 0, but your setup has it at layer 3
            // Force correct layer configuration
            int baseLayerIndex = animator.GetLayerIndex("Base Layer");
            if (baseLayerIndex != 0)
            {
                Debug.LogError($"[BossRoombaBrain] CRITICAL: Base Layer is at index {baseLayerIndex} instead of 0! Animator Controller layer order is wrong. Applying workaround...");
                
                // Emergency workaround: Disable the misplaced Base Layer and ensure layer 0 is active
                if (baseLayerIndex > 0)
                {
                    animator.SetLayerWeight(baseLayerIndex, 0f); // Disable misplaced base layer
                    Debug.LogWarning($"[BossRoombaBrain] Disabled Base Layer at index {baseLayerIndex}");
                }
                
                // Ensure layer 0 (whatever it's called) is enabled
                animator.SetLayerWeight(0, 1f);
                Debug.Log($"[BossRoombaBrain] Enabled layer 0: {animator.GetLayerName(0)}");
            }
            
            hitReactLayer = !string.IsNullOrEmpty(LayerNameHitReact) ? animator.GetLayerIndex(LayerNameHitReact) : -1;
            stunLayer = !string.IsNullOrEmpty(LayerNameStun) ? animator.GetLayerIndex(LayerNameStun) : -1;
            attacksLayer = !string.IsNullOrEmpty(LayerNameAttacks) ? animator.GetLayerIndex(LayerNameAttacks) : -1;
            idleAdditiveLayer = !string.IsNullOrEmpty(LayerNameIdleAdditive) ? animator.GetLayerIndex(LayerNameIdleAdditive) : -1;
            
            // Log final layer configuration
            Debug.Log($"[BossRoombaBrain] Layer indices - HitReact: {hitReactLayer}, Stun: {stunLayer}, Attacks: {attacksLayer}, Idle: {idleAdditiveLayer}");
        }

        private void InitializeAttackDescriptors()
        {
            // Initialize all attack descriptors with animation data.
            // RequiresArms can be overridden in Inspector after initialization.
            
            BasicSwipe = new BossAttackDescriptor {
                Id = "BasicSwipe", Parryable = false, Cooldown = 1.2f,
                RangeMin = 0.0f, RangeMax = 3.0f,
                WindupClipName = "Roomba_Swipe_Windup", ActiveClipName = "Roomba_Swipe_Active", RecoveryClipName = "Roomba_Swipe_Recovery",
                AnimatorTriggerOnWindup = "Swipe_Windup", AnimatorTriggerOnActive = "Swipe_Active",
                AnimatorTriggerOnRecovery = "Swipe_Recovery", RequiresArms = true
            };

            BasicSwipeLeft = new BossAttackDescriptor {
                Id = "BasicSwipeLeft", Parryable = false, Cooldown = 1.2f,
                RangeMin = 0.0f, RangeMax = 3.0f,
                WindupClipName = "Roomba_Swipe_L_Windup", ActiveClipName = "Roomba_Swipe_L_Active", RecoveryClipName = "Roomba_Swipe_L_Recovery",
                AnimatorTriggerOnWindup = "Swipe_L_Windup", AnimatorTriggerOnActive = "Swipe_L_Active",
                AnimatorTriggerOnRecovery = "Swipe_L_Recovery", RequiresArms = true
            };

            BasicSwipeRight = new BossAttackDescriptor {
                Id = "BasicSwipeRight", Parryable = false, Cooldown = 1.2f,
                RangeMin = 0.0f, RangeMax = 3.0f,
                WindupClipName = "Roomba_Swipe_R_Windup", ActiveClipName = "Roomba_Swipe_R_Active", RecoveryClipName = "Roomba_Swipe_R_Recovery",
                AnimatorTriggerOnWindup = "Swipe_R_Windup", AnimatorTriggerOnActive = "Swipe_R_Active",
                AnimatorTriggerOnRecovery = "Swipe_R_Recovery", RequiresArms = true
            };

            ArmSweep = new BossAttackDescriptor {
                Id = "ArmSweep", Parryable = true, Cooldown = 2.0f,
                RangeMin = 0.5f, RangeMax = 3.5f,
                WindupClipName = "Roomba_Sweep_Windup", ActiveClipName = "Roomba_Sweep_Active", RecoveryClipName = "Roomba_Sweep_Recovery",
                AnimatorTriggerOnWindup = "Sweep_Windup", AnimatorTriggerOnActive = "Sweep_Active",
                AnimatorTriggerOnRecovery = "Sweep_Recovery", RequiresArms = true
            };

            ArmSweepLeft = new BossAttackDescriptor {
                Id = "ArmSweepLeft", Parryable = true, Cooldown = 2.0f,
                RangeMin = 0.5f, RangeMax = 3.5f,
                WindupClipName = "Roomba_Sweep_L_Windup", ActiveClipName = "Roomba_Sweep_L_Active", RecoveryClipName = "Roomba_Sweep_L_Recovery",
                AnimatorTriggerOnWindup = "Sweep_L_Windup", AnimatorTriggerOnActive = "Sweep_L_Active",
                AnimatorTriggerOnRecovery = "Sweep_L_Recovery", RequiresArms = true
            };

            ArmSweepRight = new BossAttackDescriptor {
                Id = "ArmSweepRight", Parryable = true, Cooldown = 2.0f,
                RangeMin = 0.5f, RangeMax = 3.5f,
                WindupClipName = "Roomba_Sweep_R_Windup", ActiveClipName = "Roomba_Sweep_R_Active", RecoveryClipName = "Roomba_Sweep_R_Recovery",
                AnimatorTriggerOnWindup = "Sweep_R_Windup", AnimatorTriggerOnActive = "Sweep_R_Active",
                AnimatorTriggerOnRecovery = "Sweep_R_Recovery", RequiresArms = true
            };

            // ALL DASH ATTACKS: Set RequiresArms = false (no arms needed)
            DashLungeLeft = new BossAttackDescriptor {
                Id = "DashLungeLeft", Parryable = true, Cooldown = 3.0f,
                RangeMin = 6.0f, RangeMax = 25.0f,
                WindupClipName = "Roomba_Dash_L_Windup", ActiveClipName = "Roomba_Dash_L_Active", RecoveryClipName = "Roomba_Dash_L_Recovery",
                AnimatorTriggerOnWindup = "Dash_L_Windup", AnimatorTriggerOnActive = "Dash_L_Active",
                AnimatorTriggerOnRecovery = "Dash_L_Recovery", RequiresArms = false // NO ARMS FOR DASH
            };

            DashLungeRight = new BossAttackDescriptor {
                Id = "DashLungeRight", Parryable = true, Cooldown = 3.0f,
                RangeMin = 6.0f, RangeMax = 25.0f,
                WindupClipName = "Roomba_Dash_R_Windup", ActiveClipName = "Roomba_Dash_R_Active", RecoveryClipName = "Roomba_Dash_R_Recovery",
                AnimatorTriggerOnWindup = "Dash_R_Windup", AnimatorTriggerOnActive = "Dash_R_Active",
                AnimatorTriggerOnRecovery = "Dash_R_Recovery", RequiresArms = false // NO ARMS FOR DASH
            };

            DashLungeNoArms = new BossAttackDescriptor {
                Id = "DashLungeNoArms", Parryable = false, Cooldown = 2.5f,
                RangeMin = 6.0f, RangeMax = 25.0f,
                WindupClipName = "Roomba_Dash_N_Windup", ActiveClipName = "Roomba_Dash_N_Active", RecoveryClipName = "Roomba_Dash_N_Recovery",
                AnimatorTriggerOnWindup = "Dash_N_Windup", AnimatorTriggerOnActive = "Dash_N_Active",
                AnimatorTriggerOnRecovery = "Dash_N_Recovery", RequiresArms = false // NO ARMS FOR DASH
            };

            ArmPoke = new BossAttackDescriptor {
                Id = "ArmPoke", Parryable = true, Cooldown = 0.8f,
                RangeMin = 0.0f, RangeMax = 999f,
                WindupClipName = "Roomba_Poke_Windup", ActiveClipName = "Roomba_Poke_Active", RecoveryClipName = "Roomba_Poke_Recovery",
                AnimatorTriggerOnWindup = "Poke_Windup", AnimatorTriggerOnActive = "Poke_Active",
                AnimatorTriggerOnRecovery = "Poke_Recovery", RequiresArms = true
            };

            KnockOffSpin = new BossAttackDescriptor {
                Id = "KnockOffSpin", Parryable = false, Cooldown = 12.0f,
                RangeMin = 0.0f, RangeMax = 2.0f,
                WindupClipName = "Roomba_Knockoff_Windup", ActiveClipName = "Roomba_Knockoff_Active", RecoveryClipName = "Roomba_Knockoff_Recovery",
                AnimatorTriggerOnWindup = "Knockoff_Windup", AnimatorTriggerOnActive = "Knockoff_Active",
                AnimatorTriggerOnRecovery = "Knockoff_Recovery", RequiresArms = true
            };

            VacuumSuction = new BossAttackDescriptor {
                Id = "VacuumSuction", Parryable = false, Cooldown = 12.0f,
                RangeMin = 3.0f, RangeMax = 10.0f,
                WindupClipName = "Roomba_Vacuum_Windup", ActiveClipName = "Roomba_Vacuum_Active", RecoveryClipName = "Roomba_Vacuum_Recovery",
                AnimatorTriggerOnWindup = "Vacuum_Windup", AnimatorTriggerOnActive = "Vacuum_Active",
                AnimatorTriggerOnRecovery = "Vacuum_Recovery", RequiresArms = false
            };

            StaticCharge = new BossAttackDescriptor {
                Id = "StaticCharge", Parryable = false, Cooldown = 1.5f,
                RangeMin = 0.0f, RangeMax = 999f,
                WindupClipName = "Roomba_Charge_N_Windup", ActiveClipName = "Roomba_Charge_N_Active", RecoveryClipName = "Roomba_Charge_N_Recovery",
                AnimatorTriggerOnWindup = "Charge_N_Windup", AnimatorTriggerOnActive = "Charge_N_Active",
                AnimatorTriggerOnRecovery = "Charge_N_Recovery", RequiresArms = false
            };

            StaticChargeLeft = new BossAttackDescriptor {
                Id = "StaticChargeLeft", Parryable = false, Cooldown = 1.5f,
                RangeMin = 0.0f, RangeMax = 999f,
                WindupClipName = "Roomba_Charge_L_Windup", ActiveClipName = "Roomba_Charge_L_Active", RecoveryClipName = "Roomba_Charge_L_Recovery",
                AnimatorTriggerOnWindup = "Charge_L_Windup", AnimatorTriggerOnActive = "Charge_L_Active",
                AnimatorTriggerOnRecovery = "Charge_L_Recovery", RequiresArms = false
            };

            StaticChargeRight = new BossAttackDescriptor {
                Id = "StaticChargeRight", Parryable = false, Cooldown = 1.5f,
                RangeMin = 0.0f, RangeMax = 999f,
                WindupClipName = "Roomba_Charge_R_Windup", ActiveClipName = "Roomba_Charge_R_Active", RecoveryClipName = "Roomba_Charge_R_Recovery",
                AnimatorTriggerOnWindup = "Charge_R_Windup", AnimatorTriggerOnActive = "Charge_R_Active",
                AnimatorTriggerOnRecovery = "Charge_R_Recovery", RequiresArms = false
            };

            TargetedCharge = new BossAttackDescriptor {
                Id = "TargetedCharge", Parryable = false, Cooldown = 3.0f,
                RangeMin = 0.0f, RangeMax = 999f,
                WindupClipName = "Roomba_Charge_N_Windup", ActiveClipName = "Roomba_Charge_N_Active", RecoveryClipName = "Roomba_Charge_N_Recovery",
                AnimatorTriggerOnWindup = "Charge_N_Windup", AnimatorTriggerOnActive = "Charge_N_Active",
                AnimatorTriggerOnRecovery = "Charge_N_Recovery", RequiresArms = false
            };
        }

        void OnEnable()
        {
            form = StartForm;
            if (loop != null) StopCoroutine(loop);
            loop = StartCoroutine(FormLoop());
            ctrl.StartFollowingPlayer(0.1f);
        }

        void OnDisable()
        {
            if (loop != null) { StopCoroutine(loop); loop = null; }
            if (currentAttackRoutine != null) { StopCoroutine(currentAttackRoutine); currentAttackRoutine = null; }
            if (armsRetractRoutine != null) { StopCoroutine(armsRetractRoutine); armsRetractRoutine = null; }
            
            // Clear in-progress flags
            armsDeployInProgress = false;
            armsRetractInProgress = false;
            hornsRaiseInProgress = false;
            hornsLowerInProgress = false;
        }

        void Update()
        {
            if (animator != null && agent != null && !string.IsNullOrEmpty(ParamIdleIntensity))
            {
                float speed = agent.velocity.magnitude;
                float t = Mathf.InverseLerp(0f, Mathf.Max(0.01f, MaxIdleSpeedForIntensity), speed);
                float intensity = Mathf.Lerp(IdleIntensityMin, IdleIntensityMax, t);
                animator.SetFloat(ParamIdleIntensity, intensity);
            }
        }

        private void PushAction(string s)
        {
            if (lastActions.Count == 8) lastActions.Dequeue();
            lastActions.Enqueue(s);
            Debug.Log($"[Boss] {s}");
        }

        public IEnumerable<string> GetRecentActions() => lastActions;
        public BossAttackDescriptor GetCurrentAttack() => currentAttack;

        private bool IsOffCooldown(BossAttackDescriptor a)
        {
            if (a == null) return false;
            float t;
            return !nextReadyTime.TryGetValue(a.Id, out t) || Time.time >= t;
        }

        private void MarkCooldown(BossAttackDescriptor a)
        {
            if (a == null) return;
            nextReadyTime[a.Id] = Time.time + Mathf.Max(0f, a.Cooldown);
        }

        private void IncrementAttackCounter()
        {
            attackCounter++;
            PushAction($"Attack counter: {attackCounter}/{attackThresholdForVacuum}");
        }

        private BossAttackDescriptor SelectCloseRangeAttack(float dist)
        {
            var options = new List<BossAttackDescriptor>();
            
            // Account for stopping distance to prevent getting too close
            // Increased from 2.0f to 3.5f to ensure arm swings hit at the sweet spot
            float effectiveDist = dist - Mathf.Max(agent.stoppingDistance, 3.5f);

            if (effectiveDist >= BasicSwipe.RangeMin && effectiveDist <= BasicSwipe.RangeMax)
            {
                if (IsOffCooldown(BasicSwipeLeft)) options.Add(BasicSwipeLeft);
                if (IsOffCooldown(BasicSwipeRight)) options.Add(BasicSwipeRight);
            }

            if (effectiveDist >= ArmSweep.RangeMin && effectiveDist <= ArmSweep.RangeMax)
            {
                if (IsOffCooldown(ArmSweepLeft)) options.Add(ArmSweepLeft);
                if (IsOffCooldown(ArmSweepRight)) options.Add(ArmSweepRight);
            }

            if (options.Count == 0) return null;
            return options[Random.Range(0, options.Count)];
        }

        private void ResetTopSequence()
        {
            topPokeCount = 0;
            requiredPokesForSpin = Random.Range(PokeCountRange.x, PokeCountRange.y + 1);
        }

        private IEnumerator FormLoop()
        {
            while (true)
            {
                switch (form)
                {
                    case RoombaForm.DuelistSummoner:
                        yield return DuelistSummonerLoop();
                        break;
                    case RoombaForm.CageBull:
                        yield return CageBullLoop();
                        break;
                }
                yield return null;
            }
        }

        private IEnumerator DuelistSummonerLoop()
        {
            ctrl.ActivateAlarm();

            while (form == RoombaForm.DuelistSummoner)
            {
                if (isStunned)
                {
                    yield return null;
                    continue;
                }

                if (player == null) yield break;

                // Check vacuum threshold ONLY when not in special states
                if (attackCounter >= attackThresholdForVacuum && !IsMountedWithGrace())
                {
                    yield return ExecuteVacuumSequence();
                    continue;
                }

                if (RequirePlayerOnTopForSpin && IsMountedWithGrace())
                {
                    yield return ExecuteArmPokeSequenceThenSpin();
                    continue;
                }

                float dist = Vector3.Distance(transform.position, player.position);

                var close = SelectCloseRangeAttack(dist);
                if (close != null)
                {
                    yield return ExecuteAttackChain(close);
                }
                else if (dist <= Mathf.Max(BasicSwipe.RangeMax, ArmSweep.RangeMax))
                {
                    yield return MoveTowardPlayer(0.25f);
                }
                else
                {
                    if (dist >= DashLungeLeft.RangeMin && Random.value < 0.25f)
                        yield return ExecuteRandomLunge();
                    else
                        yield return MoveTowardPlayer(0.35f);
                }
            }
        }

        private IEnumerator ExecuteVacuumSequence()
        {
            PushAction("Vacuum sequence START");

            if (VacuumPosition != null)
            {
                agent.SetDestination(VacuumPosition.position);
                while (Vector3.Distance(transform.position, VacuumPosition.position) > 1f)
                {
                    yield return null;
                }
            }

            // Execute vacuum attack WITHOUT incrementing the attack counter
            // (it's a special transition attack, not part of the regular attack cycle)
            yield return ExecuteVacuumAttackWithoutCounter();

            bool playerInCenter = false;
            if (player != null && ArenaCenterBounds != null)
            {
                playerInCenter = ArenaCenterBounds.bounds.Contains(player.position);
            }

            if (playerInCenter)
            {
                if (ArenaManager != null)
                {
                    ArenaManager.RaiseWalls(true);
                    PushAction("Walls RAISED");
                }

                form = RoombaForm.CageBull;
                PushAction("Form changed to CAGE BULL");

                // Duelist → CageBull: Raise horns (if not already raised from vacuum)
                // This is safe because RaiseHornsIfNeeded() has guards
                StartCoroutine(RaiseHornsIfNeeded());

                if (!alarmDestroyed)
                {
                    ctrl.DeactivateAlarm();
                }
            }
            else
            {
                PushAction("Player not in center - vacuum failed");
                attackCounter = 0;
                attackThresholdForVacuum = Random.Range(AttackCountForVacuumRange.x, AttackCountForVacuumRange.y + 1);
            }
        }

        private IEnumerator ExecuteVacuumAttackWithoutCounter()
        {
            var a = VacuumSuction;
            currentAttack = a;
            PushAction($"Attack: {a.Id}");

            // NO IncrementAttackCounter() here - vacuum is a transition attack

            // CRITICAL: Cancel pending retract routine if running
            if (armsRetractRoutine != null)
            {
                cancelArmsRetract = true;
                StopCoroutine(armsRetractRoutine);
                armsRetractRoutine = null;
                PushAction("Arms retraction routine STOPPED by vacuum attack");
            }

            // Vacuum doesn't use arms - retract if currently deployed
            if (armsDeployed)
            {
                PushAction("Vacuum attack - retracting arms...");
                yield return RetractArmsIfNeeded();
                PushAction("Arms retract complete, continuing vacuum setup");
            }

            // PROPERLY HANDLED: Raise horns BEFORE vacuum attack (just like arms deploy/retract)
            yield return RaiseHornsIfNeeded();

            Debug.Log($"[Boss] Starting vacuum attack animations - Windup trigger: {a.AnimatorTriggerOnWindup}");
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));

            // PROPERLY HANDLED: Lower horns AFTER vacuum attack (just like arms deploy/retract)
            yield return LowerHornsIfNeeded();

            MarkCooldown(a);
        }

        private IEnumerator CageBullLoop()
        {
            PushAction("Cage Bull loop START");

            if (RequirePlayerOnTopForSpin && IsMountedWithGrace())
            {
                yield return ExecuteArmPokeSequenceThenSpin();
                yield break;
            }

            for (int i = 0; i < StaticChargeCount; i++)
            {
                if (isStunned) break;
                yield return ExecuteStaticCharge(i);
            }

            if (!isStunned)
            {
                yield return WaitForSecondsCache.Get(ChargeRestDuration);
                yield return ExecuteTargetedCharge();
            }

            yield return null;
        }

        private IEnumerator ExecuteStaticCharge(int cycleIndex)
        {
            if (ArenaManager == null || ArenaManager.LaneStarts.Count == 0)
            {
                PushAction("No arena lanes configured");
                yield break;
            }

            int laneCount = Mathf.Min(3, ArenaManager.LaneStarts.Count);
            var (start, end) = ArenaManager.GetLane(cycleIndex % laneCount);

            PushAction($"Static charge cycle {cycleIndex} - Lane {cycleIndex % laneCount}");

            agent.SetDestination(start);
            while (Vector3.Distance(transform.position, start) > 0.5f)
            {
                yield return null;
            }

            yield return ExecuteChargeBetweenPoints(start, end);
            yield return WaitForSecondsCache.Get(0.3f);
        }

        private IEnumerator ExecuteTargetedCharge()
        {
            if (player == null) yield break;

            PushAction("Targeted charge START");
            currentAttack = TargetedCharge;

            if (animator != null && !string.IsNullOrEmpty(TargetedCharge.AnimatorTriggerOnWindup))
                animator.SetTrigger(TargetedCharge.AnimatorTriggerOnWindup);

            yield return new WaitForSeconds(TargetedCharge.WindupSpeedMultiplier * GetClipLength(animator, TargetedCharge.WindupClipName));

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            Vector3 overshootTarget = player.position + dirToPlayer * ChargeOvershootDistance;

            float baseSpeed = agent.speed;
            agent.speed = baseSpeed * ChargeSpeedMultiplier;

            if (animator != null && !string.IsNullOrEmpty(TargetedCharge.AnimatorTriggerOnActive))
                animator.SetTrigger(TargetedCharge.AnimatorTriggerOnActive);

            agent.SetDestination(overshootTarget);

            float chargeTime = 0f;
            while (chargeTime < (TargetedCharge.ActiveSpeedMultiplier * GetClipLength(animator, TargetedCharge.ActiveClipName)) && !isStunned)
            {
                chargeTime += Time.deltaTime;
                yield return null;
            }

            agent.speed = baseSpeed;

            if (animator != null && !string.IsNullOrEmpty(TargetedCharge.AnimatorTriggerOnRecovery))
                animator.SetTrigger(TargetedCharge.AnimatorTriggerOnRecovery);

            yield return new WaitForSeconds(TargetedCharge.RecoverySpeedMultiplier * GetClipLength(animator, TargetedCharge.RecoveryClipName));
            MarkCooldown(TargetedCharge);
        }

        private IEnumerator ExecuteChargeBetweenPoints(Vector3 start, Vector3 end)
        {
            float baseSpeed = agent.speed;
            agent.speed = baseSpeed * ChargeSpeedMultiplier;

            if (animator != null && !string.IsNullOrEmpty(StaticCharge.AnimatorTriggerOnActive))
                animator.SetTrigger(StaticCharge.AnimatorTriggerOnActive);

            agent.SetDestination(end);

            while (Vector3.Distance(transform.position, end) > 1f)
            {
                yield return null;
            }

            agent.speed = baseSpeed;
        }

        public void OnPillarCollision(int pillarIndex)
        {
            if (form != RoombaForm.CageBull) return;

            PushAction($"Pillar {pillarIndex} collision - STUNNED");

            if (ArenaManager != null)
            {
                ArenaManager.OnPillarCollision(pillarIndex);
            }

            StartCoroutine(ApplyStun(StunType.PillarCollision, PillarStunSeconds));

            if (ArenaManager != null)
            {
                ArenaManager.RaiseWalls(false);
                PushAction("Walls LOWERED");
            }

            form = RoombaForm.DuelistSummoner;
            attackCounter = 0;
            attackThresholdForVacuum = Random.Range(AttackCountForVacuumRange.x, AttackCountForVacuumRange.y + 1);
            PushAction("Form changed to DUELIST/SUMMONER");

            // CageBull → Duelist: Lower horns
            StartCoroutine(LowerHornsIfNeeded());

            ctrl.ActivateAlarm();
        }

        private IEnumerator ExecuteArmPokeSequenceThenSpin()
        {
            if (topPokeCount == 0)
            {
                requiredPokesForSpin = requiredPokesForSpin <= 0 ?
                    Random.Range(PokeCountRange.x, PokeCountRange.y + 1) : requiredPokesForSpin;
            }

            bool waitedAfterLastPoke = false;

            while (IsMountedWithGrace())
            {
                if (topPokeCount < requiredPokesForSpin)
                {
                    if (IsOffCooldown(ArmPoke))
                    {
                        yield return ExecuteAttackChain(ArmPoke);
                        topPokeCount++;
                    }
                    else
                    {
                        yield return null;
                    }
                }
                else
                {
                    if (!waitedAfterLastPoke)
                    {
                        waitedAfterLastPoke = true;
                        yield return WaitForSecondsCache.Get(SpinAfterLastPokeDelay);
                        if (!IsMountedWithGrace()) { ResetTopSequence(); yield break; }
                    }

                    if (IsOffCooldown(KnockOffSpin))
                    {
                        yield return ExecuteKnockOffSpin();
                        ResetTopSequence();
                        yield break;
                    }
                    else
                    {
                        if (IsOffCooldown(ArmPoke))
                        {
                            yield return ExecuteAttackChain(ArmPoke);
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                }
            }

            ResetTopSequence();
        }

        private bool IsTopExclusive(BossAttackDescriptor a)
        {
            return a == ArmPoke || a == KnockOffSpin;
        }

        private IEnumerator ExecuteAttackChain(BossAttackDescriptor a)
        {
            if (player == null || a == null) yield break;
            if (IsMountedWithGrace() && !IsTopExclusive(a)) yield break;
            if (isStunned) yield break;

            currentAttack = a;
            PushAction($"Attack: {a.Id}");

            if (a != ArmPoke && form == RoombaForm.DuelistSummoner)
            {
                IncrementAttackCounter();
            }

            // ALWAYS cancel pending retraction when ANY attack starts
            if (armsRetractRoutine != null)
            {
                cancelArmsRetract = true;
                StopCoroutine(armsRetractRoutine);
                armsRetractRoutine = null;
                PushAction("Arms retraction routine STOPPED by new attack");
            }

            // Handle arms state based on attack requirements
            if (a.RequiresArms)
            {
                // Attack needs arms deployed - deploy if not already
                PushAction("Waiting for arms deploy animation...");
                yield return DeployArmsIfNeeded();
                PushAction("Arms deploy complete, starting attack");
            }
            else
            {
                // Attack doesn't use arms - retract if currently deployed
                if (armsDeployed)
                {
                    PushAction("Waiting for arms retract animation...");
                    yield return RetractArmsIfNeeded();
                    PushAction("Arms retract complete, starting attack");
                }
            }

            // Horns deployment/retraction handled within attack timings
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));

            ApplyBossDamageIfPlayerPresent(1.0f);

            MarkCooldown(a);

            // Only schedule auto-retract if arms are currently deployed
            if (armsDeployed)
                ScheduleArmsAutoRetract();
        }

        private IEnumerator ExecuteRandomLunge()
        {
            float r = Random.value;
            BossAttackDescriptor lunge;
            if (r < 0.33f) lunge = DashLungeLeft;
            else if (r < 0.66f) lunge = DashLungeRight;
            else lunge = DashLungeNoArms;
            yield return ExecuteDashLunge(lunge);
        }

        private IEnumerator ExecuteDashLunge(BossAttackDescriptor a)
        {
            if (IsMountedWithGrace() && !IsTopExclusive(a)) yield break;
            if (isStunned || a == null) yield break;
            if (player == null) yield break;

            currentAttack = a;
            PushAction($"Attack: {a.Id}");

            IncrementAttackCounter();

            // ALWAYS cancel pending retraction when ANY attack starts
            if (armsRetractRoutine != null)
            {
                cancelArmsRetract = true;
                StopCoroutine(armsRetractRoutine);
                armsRetractRoutine = null;
                PushAction("Arms retraction routine STOPPED by dash attack");
            }

            // Handle arms state based on attack requirements
            if (a.RequiresArms)
            {
                // Dash needs arms deployed
                yield return DeployArmsIfNeeded();
            }
            else
            {
                // Dash doesn't use arms - retract if currently deployed
                if (armsDeployed)
                {
                    PushAction("Dash attack - retracting arms...");
                    yield return RetractArmsIfNeeded();
                }
            }

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            Vector3 overshootTarget = player.position + dirToPlayer * DashOvershootDistance;

            float baseSpeed = agent.speed;
            agent.speed = DashSpeed;

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);

            agent.SetDestination(overshootTarget);

            yield return new WaitForSeconds(a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName));

            agent.speed = baseSpeed;

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));

            MarkCooldown(a);

            // Only schedule auto-retract if arms are currently deployed
            if (armsDeployed)
                ScheduleArmsAutoRetract();
        }

        private IEnumerator ExecuteKnockOffSpin()
        {
            currentAttack = KnockOffSpin;
            PushAction("Attack: KnockOffSpin");

            // ALWAYS cancel pending retraction
            if (armsRetractRoutine != null)
            {
                cancelArmsRetract = true;
                StopCoroutine(armsRetractRoutine);
                armsRetractRoutine = null;
                PushAction("Arms retraction routine STOPPED by knock-off spin");
            }

            if (KnockOffSpin.RequiresArms)
            {
                yield return DeployArmsIfNeeded();
            }
            
            var a = KnockOffSpin;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);

            if (player != null && IsPlayerMounted())
            {
                FlingPlayer();
            }

            yield return new WaitForSeconds(a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));
            MarkCooldown(a);
            if (armsDeployed)
                ScheduleArmsAutoRetract();
        }

        private void FlingPlayer()
        {
            if (player == null) return;

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                Vector3 flingDir = new Vector3(randomDir.x, 1f, randomDir.y).normalized;

                float force = Random.Range(FlingForceRange.x, FlingForceRange.y);
                rb.AddForce(flingDir * force, ForceMode.Impulse);

                PushAction($"Player flung with force {force}");
            }
        }

        private IEnumerator ApplyStun(StunType stunType, float duration)
        {
            isStunned = true;
            currentStunType = stunType;

            // Animator: trigger stun windup
            if (animator != null && !string.IsNullOrEmpty(TriggerStunWindup))
            {
                animator.SetTrigger(TriggerStunWindup);
                PushAction($"Stun animation: Windup triggered");
            }

            // Immediately disable all hitboxes (safety)
            if (animMediator == null) animMediator = GetComponentInChildren<BossAnimationEventMediator>(true);
            animMediator?.DisableAllHitboxes();

            // Optional: temporarily zero attacks layer; keep HitReact visible
            SetLayerWeightSafe(attacksLayer, 0f);
            // Idle additive off for stillness beneath stun
            SetLayerWeightSafe(idleAdditiveLayer, 0f);

            agent.isStopped = true;
            PushAction($"Stunned ({stunType}) for {duration}s");

            // Wait a bit for windup animation, then trigger active phase
            float windupDuration = duration * 0.2f; // 20% of stun time for windup
            float activeDuration = duration * 0.6f; // 60% of stun time for active (main stun)
            float recoveryDuration = duration * 0.2f; // 20% of stun time for recovery

            yield return new WaitForSeconds(windupDuration);
            
            // Trigger stun active phase
            if (animator != null && !string.IsNullOrEmpty(TriggerStunActive))
            {
                animator.SetTrigger(TriggerStunActive);
                PushAction($"Stun animation: Active triggered");
            }

            yield return new WaitForSeconds(activeDuration);
            
            // Trigger stun recovery phase
            if (animator != null && !string.IsNullOrEmpty(TriggerStunRecovery))
            {
                animator.SetTrigger(TriggerStunRecovery);
                PushAction($"Stun animation: Recovery triggered");
            }

            yield return new WaitForSeconds(recoveryDuration);

            agent.isStopped = false;
            isStunned = false;
            currentStunType = StunType.None;
            PushAction("Stun ended");

            // Restore layer weights
            SetLayerWeightSafe(attacksLayer, 1f);
            SetLayerWeightSafe(idleAdditiveLayer, 1f);

            if (armsDeployed)
                ScheduleArmsAutoRetract();
        }

        private void SetLayerWeightSafe(int layerIndex, float weight)
        {
            if (animator == null) return;
            if (layerIndex >= 0 && layerIndex < animator.layerCount)
                animator.SetLayerWeight(layerIndex, Mathf.Clamp01(weight));
        }

        /// <summary>
        /// Called by BossSidePanelCollider when player attacks a panel.
        /// </summary>
        public void DamageSidePanel(int panelIndex, float damage)
        {
            if (panelIndex < 0 || panelIndex >= SidePanels.Count) return;

            var panel = SidePanels[panelIndex];
            if (panel.isDestroyed) return;

            panel.currentHealth -= damage;
            PushAction($"Panel {panelIndex} took {damage} damage ({panel.currentHealth}/{panel.maxHealth})");

            TriggerRandomHitReact();

            if (panel.currentHealth <= 0)
            {
                DestroyPanel(panelIndex);
            }
        }

        /// <summary>
        /// Called by BossSidePanelCollider when player attacks an exposed vulnerable zone.
        /// Applies amplified damage to the boss's main health pool.
        /// </summary>
        public void DamageVulnerableZone(int panelIndex, float damage)
        {
            if (panelIndex < 0 || panelIndex >= SidePanels.Count) return;

            var panel = SidePanels[panelIndex];
            if (!panel.isDestroyed) return; // Only take damage if panel is already destroyed

            float amplifiedDamage = damage * panel.vulnerabilityMultiplier;
            PushAction($"Vulnerable zone {panelIndex} hit for {amplifiedDamage} damage (x{panel.vulnerabilityMultiplier})");

            // Apply amplified damage to boss health
            ApplyDamageToBoss(amplifiedDamage);
            
            TriggerRandomHitReact();
        }

        /// <summary>
        /// Centralized method to apply damage to the boss's health pool.
        /// Used by vulnerable zones and can be called by other systems.
        /// </summary>
        public void ApplyDamageToBoss(float damage)
        {
            var health = GetComponent<BossHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[BossRoombaBrain] No BossHealth component found! Cannot apply {damage} damage.");
            }
        }





        private void DestroyPanel(int panelIndex)
        {
            var panel = SidePanels[panelIndex];
            panel.isDestroyed = true;
            panel.currentHealth = 0;
            PushAction($"Side panel {panelIndex} DESTROYED! Zone now vulnerable (x{panel.vulnerabilityMultiplier} damage)");

            if (panel.panelVisualMesh == null)
            {
                Debug.LogWarning($"[BossRoombaBrain] Panel {panelIndex} has no visual mesh assigned!");
                return;
            }

            // Spawn break VFX at panel position
            Vector3 panelPos = panel.panelVisualMesh.transform.position;
            Quaternion panelRot = panel.panelVisualMesh.transform.rotation;
            
            if (panel.breakVFXPrefab != null)
            {
                Instantiate(panel.breakVFXPrefab, panelPos, panelRot);
            }

            // Check if this is a Skinned Mesh Renderer (animated mesh)
            var skinnedRenderer = panel.panelVisualMesh.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer != null)
            {
                // SKINNED MESH: Bake to static mesh, create new falling object, hide original
                CreateFallingPanelFromSkinnedMesh(panel, skinnedRenderer, panelPos, panelRot);
            }
            else
            {
                // REGULAR MESH: Detach and apply physics directly
                CreateFallingPanelFromStaticMesh(panel, panelPos);
            }
        }

        /// <summary>
        /// Handles panel break-off for Skinned Mesh Renderers.
        /// Bakes the current pose to a static mesh, creates a falling copy, hides original.
        /// </summary>
        private void CreateFallingPanelFromSkinnedMesh(SidePanel panel, SkinnedMeshRenderer skinnedRenderer, Vector3 position, Quaternion rotation)
        {
            // Bake the current skinned mesh pose to a static mesh
            Mesh bakedMesh = new Mesh();
            skinnedRenderer.BakeMesh(bakedMesh);
            
            // Create a new GameObject for the falling panel
            GameObject fallingPanel = new GameObject($"FallingPanel_{panel.panelVisualMesh.name}");
            fallingPanel.transform.position = position;
            fallingPanel.transform.rotation = rotation;
            fallingPanel.transform.localScale = panel.panelVisualMesh.transform.lossyScale;
            
            // Add mesh components
            var meshFilter = fallingPanel.AddComponent<MeshFilter>();
            meshFilter.mesh = bakedMesh;
            
            var meshRenderer = fallingPanel.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = skinnedRenderer.sharedMaterials;
            
            // Add physics
            var rb = fallingPanel.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Add collider
            var meshCol = fallingPanel.AddComponent<MeshCollider>();
            meshCol.convex = true;
            meshCol.sharedMesh = panel.fallCollisionMesh != null ? panel.fallCollisionMesh : bakedMesh;
            
            // Apply break-off force
            Vector3 breakDirection = (position - transform.position).normalized;
            breakDirection.y = 0.3f;
            rb.AddForce(breakDirection.normalized * panel.breakOffForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * panel.breakOffForce * 0.5f, ForceMode.Impulse);
            
            // Hide the original skinned mesh (can't detach from skeleton)
            panel.panelVisualMesh.SetActive(false);
            
            // Schedule cleanup of the falling panel
            if (panel.destroyedPanelLifetime > 0)
            {
                Destroy(fallingPanel, panel.destroyedPanelLifetime);
            }
            
            PushAction($"Panel {panel.panelVisualMesh.name} baked and detached (skinned mesh)");
        }

        /// <summary>
        /// Handles panel break-off for regular static meshes.
        /// Detaches and applies physics directly.
        /// </summary>
        private void CreateFallingPanelFromStaticMesh(SidePanel panel, Vector3 panelPos)
        {
            // Detach from boss hierarchy
            panel.panelVisualMesh.transform.SetParent(null);
            
            // Add physics components
            var rb = panel.panelVisualMesh.GetComponent<Rigidbody>();
            if (rb == null) rb = panel.panelVisualMesh.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // Add collider for falling physics (convex required for rigidbody)
            var meshCol = panel.panelVisualMesh.GetComponent<MeshCollider>();
            if (meshCol == null) meshCol = panel.panelVisualMesh.AddComponent<MeshCollider>();
            meshCol.convex = true;
            
            // Use custom collision mesh if provided, otherwise try to get from MeshFilter
            if (panel.fallCollisionMesh != null)
            {
                meshCol.sharedMesh = panel.fallCollisionMesh;
            }
            else
            {
                var meshFilter = panel.panelVisualMesh.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshCol.sharedMesh = meshFilter.sharedMesh;
                }
            }
            
            // Apply outward force from boss center
            Vector3 breakDirection = (panelPos - transform.position).normalized;
            breakDirection.y = 0.3f;
            rb.AddForce(breakDirection.normalized * panel.breakOffForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * panel.breakOffForce * 0.5f, ForceMode.Impulse);

            // Schedule cleanup
            if (panel.destroyedPanelLifetime > 0)
            {
                Destroy(panel.panelVisualMesh, panel.destroyedPanelLifetime);
            }
            
            PushAction($"Panel {panel.panelVisualMesh.name} detached (static mesh)");
        }

        /// <summary>
        /// Check if a specific panel has been destroyed (for external queries).
        /// </summary>
        public bool IsPanelDestroyed(int panelIndex)
        {
            if (panelIndex < 0 || panelIndex >= SidePanels.Count) return false;
            return SidePanels[panelIndex].isDestroyed;
        }

        private void TriggerRandomHitReact()
        {
            if (animator == null) return;
            int roll = Random.Range(0, 3);
            switch (roll)
            {
                case 0:
                    if (!string.IsNullOrEmpty(TriggerDamagedV1)) animator.SetTrigger(TriggerDamagedV1);
                    break;
                case 1:
                    if (!string.IsNullOrEmpty(TriggerDamagedV2)) animator.SetTrigger(TriggerDamagedV2);
                    break;
                default:
                    if (!string.IsNullOrEmpty(TriggerDamagedV3)) animator.SetTrigger(TriggerDamagedV3);
                    break;
            }
        }

        public float GetDamageMultiplierForPanel(int panelIndex)
        {
            if (panelIndex < 0 || panelIndex >= SidePanels.Count) return 1f;
            return SidePanels[panelIndex].isDestroyed ? SidePanels[panelIndex].vulnerabilityMultiplier : 1f;
        }

        [ContextMenu("Debug: Trigger Vacuum Sequence")]
        public void DebugTriggerVacuumSequence()
        {
            if (form == RoombaForm.DuelistSummoner)
            {
                attackCounter = attackThresholdForVacuum;
                PushAction("DEBUG: Forced vacuum sequence");
            }
        }

        [ContextMenu("Debug: Return to Duelist Form")]
        public void DebugReturnToDuelistForm()
        {
            if (form == RoombaForm.CageBull)
            {
                if (ArenaManager != null)
                {
                    ArenaManager.RaiseWalls(false);
                }
                form = RoombaForm.DuelistSummoner;
                attackCounter = 0;
                attackThresholdForVacuum = Random.Range(AttackCountForVacuumRange.x, AttackCountForVacuumRange.y + 1);
                ctrl.ActivateAlarm();
                StartCoroutine(LowerHornsIfNeeded());
                PushAction("DEBUG: Returned to Duelist form");
            }
        }

        [ContextMenu("Debug: Apply Parry Stun")]
        public void DebugApplyParryStun()
        {
            StartCoroutine(ApplyStun(StunType.Parry, ParryStaggerSeconds));
        }

        [ContextMenu("Debug: Apply Pillar Stun")]
        public void DebugApplyPillarStun()
        {
            StartCoroutine(ApplyStun(StunType.PillarCollision, PillarStunSeconds));
        }

        [ContextMenu("Debug: Take 50 Damage")]
        public void DebugTake50Damage()
        {
            var health = GetComponent<BossHealth>();
            if (health != null)
            {
                health.TakeDamage(50f);
                TriggerRandomHitReact(); // Trigger hit reaction animation
                PushAction("DEBUG: Took 50 damage");
            }
            else
            {
                Debug.LogError("[BossRoombaBrain] BossHealth component not found! Cannot test damage.");
            }
        }

        [ContextMenu("Debug: Take 100 Damage")]
        public void DebugTake100Damage()
        {
            var health = GetComponent<BossHealth>();
            if (health != null)
            {
                health.TakeDamage(100f);
                TriggerRandomHitReact(); // Trigger hit reaction animation
                PushAction("DEBUG: Took 100 damage");
            }
            else
            {
                Debug.LogError("[BossRoombaBrain] BossHealth component not found! Cannot test damage.");
            }
        }

        [ContextMenu("Debug: Take 250 Damage (Heavy)")]
        public void DebugTake250Damage()
        {
            var health = GetComponent<BossHealth>();
            if (health != null)
            {
                health.TakeDamage(250f);
                TriggerRandomHitReact(); // Trigger hit reaction animation
                PushAction("DEBUG: Took 250 heavy damage");
            }
            else
            {
                Debug.LogError("[BossRoombaBrain] BossHealth component not found! Cannot test damage.");
            }
        }

        #region Debug Panel Break Methods
        
        /// <summary>
        /// Debug method to force-break a specific panel by index.
        /// Works in Edit mode and Play mode.
        /// </summary>
        public void DebugBreakPanel(int panelIndex)
        {
            if (panelIndex < 0 || panelIndex >= SidePanels.Count)
            {
                Debug.LogError($"[BossRoombaBrain] Invalid panel index {panelIndex}. Valid range: 0-{SidePanels.Count - 1}");
                return;
            }
            
            var panel = SidePanels[panelIndex];
            if (panel.isDestroyed)
            {
                Debug.LogWarning($"[BossRoombaBrain] Panel {panelIndex} is already destroyed!");
                return;
            }
            
            if (panel.panelVisualMesh == null)
            {
                Debug.LogError($"[BossRoombaBrain] Panel {panelIndex} has no visual mesh assigned!");
                return;
            }
            
            Debug.Log($"[BossRoombaBrain] DEBUG: Breaking panel {panelIndex} ({panel.panelVisualMesh.name})");
            DestroyPanel(panelIndex);
        }
        
        /// <summary>
        /// Debug method to reset all panels to their original state.
        /// Only works if the panel GameObjects still exist.
        /// </summary>
        [ContextMenu("Debug: Reset All Panels")]
        public void DebugResetAllPanels()
        {
            foreach (var panel in SidePanels)
            {
                panel.currentHealth = panel.maxHealth;
                panel.isDestroyed = false;
            }
            Debug.Log($"[BossRoombaBrain] DEBUG: Reset all {SidePanels.Count} panels to full health. Note: Detached panels cannot be re-attached at runtime.");
        }
        
        [ContextMenu("Debug: Break Panel 0")]
        public void DebugBreakPanel0() => DebugBreakPanel(0);
        
        [ContextMenu("Debug: Break Panel 1")]
        public void DebugBreakPanel1() => DebugBreakPanel(1);
        
        [ContextMenu("Debug: Break Panel 2")]
        public void DebugBreakPanel2() => DebugBreakPanel(2);
        
        [ContextMenu("Debug: Break Panel 3")]
        public void DebugBreakPanel3() => DebugBreakPanel(3);
        
        [ContextMenu("Debug: Break Panel 4")]
        public void DebugBreakPanel4() => DebugBreakPanel(4);
        
        [ContextMenu("Debug: Break Panel 5")]
        public void DebugBreakPanel5() => DebugBreakPanel(5);
        
        [ContextMenu("Debug: Break Panel 6")]
        public void DebugBreakPanel6() => DebugBreakPanel(6);
        
        [ContextMenu("Debug: Break Panel 7")]
        public void DebugBreakPanel7() => DebugBreakPanel(7);
        
        [ContextMenu("Debug: Break ALL Panels")]
        public void DebugBreakAllPanels()
        {
            for (int i = 0; i < SidePanels.Count; i++)
            {
                if (!SidePanels[i].isDestroyed)
                {
                    DebugBreakPanel(i);
                }
            }
        }
        
        #endregion

        private IEnumerator DeployArmsIfNeeded()
        {
            // Guard: Already deployed
            if (armsDeployed)
            {
                Debug.Log($"[Boss] DeployArmsIfNeeded() - arms already deployed, skipping");
                yield break;
            }

            // Guard: Deploy already in progress
            if (armsDeployInProgress)
            {
                Debug.Log($"[Boss] DeployArmsIfNeeded() - deploy already in progress, waiting...");
                yield return new WaitUntil(() => !armsDeployInProgress);
                yield break;
            }

            // Guard: Wait for any retract in progress to finish first
            if (armsRetractInProgress)
            {
                Debug.Log($"[Boss] DeployArmsIfNeeded() - retract in progress, waiting for it to finish...");
                yield return new WaitUntil(() => !armsRetractInProgress);
            }

            armsDeployInProgress = true;
            armsDeployed = true;
#if UNITY_EDITOR
            Debug.Log($"[Boss] DeployArms() started - setting armsDeployed=true, triggering animator: {ArmsDeployTrigger}");
#endif
            
            if (animator != null && !string.IsNullOrEmpty(ArmsDeployTrigger)) 
            {
                animator.SetTrigger(ArmsDeployTrigger);
#if UNITY_EDITOR
                Debug.Log($"[Boss] Animator trigger '{ArmsDeployTrigger}' SET");
#endif
            }

            // Wait for deploy animation to complete
            yield return WaitForSecondsCache.Get(ArmsDeployTimeoutSeconds);
            
            armsDeployInProgress = false;
#if UNITY_EDITOR
            Debug.Log($"[Boss] DeployArms() complete");
#endif
        }

        private IEnumerator RetractArmsIfNeeded()
        {
            // Guard: Already retracted
            if (!armsDeployed)
            {
                Debug.Log($"[Boss] RetractArmsIfNeeded() - arms already retracted, skipping");
                yield break;
            }

            // Guard: Retract already in progress
            if (armsRetractInProgress)
            {
                Debug.Log($"[Boss] RetractArmsIfNeeded() - retract already in progress, waiting...");
                yield return new WaitUntil(() => !armsRetractInProgress);
                yield break;
            }

            // Guard: Wait for any deploy in progress to finish first
            if (armsDeployInProgress)
            {
                Debug.Log($"[Boss] RetractArmsIfNeeded() - deploy in progress, waiting for it to finish...");
                yield return new WaitUntil(() => !armsDeployInProgress);
            }

            armsRetractInProgress = true;
            armsDeployed = false;
#if UNITY_EDITOR
            Debug.Log($"[Boss] RetractArms() started - setting armsDeployed=false, triggering animator: {ArmsRetractTrigger}");
#endif
            
            if (animator != null && !string.IsNullOrEmpty(ArmsRetractTrigger)) 
            {
                animator.SetTrigger(ArmsRetractTrigger);
#if UNITY_EDITOR
                Debug.Log($"[Boss] Animator trigger '{ArmsRetractTrigger}' SET");
#endif
            }

            // CRITICAL: Wait for the FULL retract animation to complete
            // This ensures arms are fully retracted before starting unarmed attacks
            float waitTime = ArmsRetractDuration;
#if UNITY_EDITOR
            Debug.Log($"[Boss] RetractArms() - waiting {waitTime}s for animation to complete");
#endif
            yield return WaitForSecondsCache.Get(waitTime);
            
            armsRetractInProgress = false;
#if UNITY_EDITOR
            Debug.Log($"[Boss] RetractArms() complete");
#endif
        }

        private IEnumerator RaiseHornsIfNeeded()
        {
            // Guard: Already raised
            if (hornsRaised)
            {
                Debug.Log($"[Boss] RaiseHornsIfNeeded() - horns already raised, skipping");
                yield break;
            }

            // Guard: Raise already in progress
            if (hornsRaiseInProgress)
            {
                Debug.Log($"[Boss] RaiseHornsIfNeeded() - raise already in progress, waiting...");
                yield return new WaitUntil(() => !hornsRaiseInProgress);
                yield break;
            }

            // Guard: Wait for any lower in progress to finish first
            if (hornsLowerInProgress)
            {
                Debug.Log($"[Boss] RaiseHornsIfNeeded() - lower in progress, waiting for it to finish...");
                yield return new WaitUntil(() => !hornsLowerInProgress);
            }

            hornsRaiseInProgress = true;
            hornsRaised = true;
            PushAction("Raising horns (lowering faceplate)...");
#if UNITY_EDITOR
            Debug.Log($"[Boss] RaiseHorns() started - setting hornsRaised=true, triggering animator: {HornsRaiseTrigger}");
#endif
            
            if (animator != null && !string.IsNullOrEmpty(HornsRaiseTrigger)) 
            {
                animator.SetTrigger(HornsRaiseTrigger);
#if UNITY_EDITOR
                Debug.Log($"[Boss] Animator trigger '{HornsRaiseTrigger}' SET");
#endif
            }

            // Wait for horn raise animation to complete
            float waitTime = HornsRaiseDuration;
#if UNITY_EDITOR
            Debug.Log($"[Boss] RaiseHorns() - waiting {waitTime}s for animation to complete");
#endif
            yield return WaitForSecondsCache.Get(waitTime);
            
            hornsRaiseInProgress = false;
            PushAction("Horns raised (faceplate lowered)");
#if UNITY_EDITOR
            Debug.Log($"[Boss] RaiseHorns() complete");
#endif
        }

        private IEnumerator LowerHornsIfNeeded()
        {
            // Guard: Already lowered
            if (!hornsRaised)
            {
                Debug.Log($"[Boss] LowerHornsIfNeeded() - horns already lowered, skipping");
                yield break;
            }

            // Guard: Lower already in progress
            if (hornsLowerInProgress)
            {
                Debug.Log($"[Boss] LowerHornsIfNeeded() - lower already in progress, waiting...");
                yield return new WaitUntil(() => !hornsLowerInProgress);
                yield break;
            }

            // Guard: Wait for any raise in progress to finish first
            if (hornsRaiseInProgress)
            {
                Debug.Log($"[Boss] LowerHornsIfNeeded() - raise in progress, waiting for it to finish...");
                yield return new WaitUntil(() => !hornsRaiseInProgress);
            }

            hornsLowerInProgress = true;
            hornsRaised = false;
            PushAction("Lowering horns (raising faceplate)...");
#if UNITY_EDITOR
            Debug.Log($"[Boss] LowerHorns() started - setting hornsRaised=false, triggering animator: {HornsLowerTrigger}");
#endif
            
            if (animator != null && !string.IsNullOrEmpty(HornsLowerTrigger)) 
            {
                animator.SetTrigger(HornsLowerTrigger);
#if UNITY_EDITOR
                Debug.Log($"[Boss] Animator trigger '{HornsLowerTrigger}' SET");
#endif
            }

            // Wait for horn lower animation to complete
            float waitTime = HornsLowerDuration;
#if UNITY_EDITOR
            Debug.Log($"[Boss] LowerHorns() - waiting {waitTime}s for animation to complete");
#endif
            yield return WaitForSecondsCache.Get(waitTime);
            
            hornsLowerInProgress = false;
            PushAction("Horns lowered (faceplate raised)");
#if UNITY_EDITOR
            Debug.Log($"[Boss] LowerHorns() complete");
#endif
        }

        private void DeployArms()
        {
            if (armsDeployInProgress) return;
            armsDeployInProgress = true;
            StartCoroutine(DeployArmsIfNeeded());
        }

        private void ScheduleArmsAutoRetract()
        {
            if (armsRetractRoutine != null) StopCoroutine(armsRetractRoutine);
            armsRetractRoutine = StartCoroutine(ArmsRetractAfterCooldown());
        }

        private IEnumerator ArmsRetractAfterCooldown()
        {
            Debug.Log($"[Boss] ArmsRetractAfterCooldown() started - waiting {ArmsAutoRetractCooldown}s");
            cancelArmsRetract = false; // Reset cancel flag
            float t = 0f;
            while (t < ArmsAutoRetractCooldown)
            {
                // Check if retraction was canceled (new attack started)
                if (cancelArmsRetract)
                {
                    PushAction("Arms retraction CANCELED - new attack starting");
                    armsRetractRoutine = null;
                    yield break;
                }
                
                t += Time.deltaTime;
                yield return null;
            }
            
            // Final check before actually retracting
            if (cancelArmsRetract)
            {
                Debug.Log($"[Boss] ArmsRetractAfterCooldown() aborted - cancelArmsRetract={cancelArmsRetract}");
                armsRetractRoutine = null;
                yield break;
            }

            // Don't retract if already retracting or deploying
            if (armsRetractInProgress || armsDeployInProgress)
            {
                Debug.Log($"[Boss] ArmsRetractAfterCooldown() aborted - animation already in progress (retract={armsRetractInProgress}, deploy={armsDeployInProgress})");
                armsRetractRoutine = null;
                yield break;
            }

            // Use the guarded retract helper
            PushAction("Arms auto-retracting after cooldown...");
            yield return RetractArmsIfNeeded();
            PushAction("Arms auto-retraction complete");
            
            armsRetractRoutine = null;
        }

        public void SetPlayerOnTop(bool value)
        {
            Debug.Log($"[BossRoombaBrain] SetPlayerOnTop called: {value}, playerOnTop was: {playerOnTop}");
            playerOnTop = value;
            if (value)
            {
                lastMountedTime = Time.time;
                hasEverMounted = true;
                Debug.Log("[BossRoombaBrain] Player mounted! Starting top wander.");
                ctrl.StartTopWander();
            }
            else
            {
                if (hasEverMounted) lastMountedTime = Time.time;
                Debug.Log("[BossRoombaBrain] Player dismounted! Stopping top wander.");
                ctrl.StopTopWander();
            }
        }

        private IEnumerator MoveTowardPlayer(float seconds)
        {
            float t = 0f;
            while (t < seconds && player != null)
            {
                agent.SetDestination(player.position);
                t += Time.deltaTime;
                yield return null;
            }
        }

        public void OnParry(string attackId, GameObject parryingPlayer)
        {
            if (currentAttack != null && currentAttack.Id == attackId && currentAttack.Parryable)
            {
                // Stop current attack execution
                if (currentAttackRoutine != null) { StopCoroutine(currentAttackRoutine); currentAttackRoutine = null; }
                // Apply parry stun (which triggers Stun_Windup animator trigger)
                StartCoroutine(ApplyStun(StunType.Parry, ParryStaggerSeconds));
                PushAction($"Parried: {attackId}");
            }
        }

        public void OnAlarmDestroyed() { alarmDestroyed = true; }

        /// <summary>
        /// Called when the boss is defeated. Handles cleanup and player release.
        /// </summary>
        public void OnBossDefeated()
        {
            PushAction("BOSS DEFEATED!");
            
            // Stop all coroutines
            if (loop != null) { StopCoroutine(loop); loop = null; }
            if (currentAttackRoutine != null) { StopCoroutine(currentAttackRoutine); currentAttackRoutine = null; }
            if (armsRetractRoutine != null) { StopCoroutine(armsRetractRoutine); armsRetractRoutine = null; }
            
            // Stop movement
            if (agent != null) agent.isStopped = true;
            
            // Release player back to DontDestroyOnLoad
            if (PlayerManager != null)
            {
                PlayerManager.OnBossDefeated();
            }
            else
            {
                Debug.LogWarning("[BossRoombaBrain] PlayerManager not assigned! Player will not be released to DontDestroyOnLoad.");
            }
            
            // Optional: Play death animation, spawn loot, etc.
            // You can add that logic here or trigger it via events
        }

        public void OnArmsDeployComplete()
        {
            armsDeployInProgress = false;
            PushAction("Arms deployment complete (via animation event)");
            Debug.Log($"[Boss] OnArmsDeployComplete() - armsDeployInProgress cleared");
        }

        public void OnArmsRetractComplete()
        {
            armsRetractInProgress = false;
            PushAction("Arms retraction complete (via animation event)");
            Debug.Log($"[Boss] OnArmsRetractComplete() - armsRetractInProgress cleared");
        }

        public void OnHornsRaiseComplete()
        {
            hornsRaiseInProgress = false;
            PushAction("Horns raise complete (via animation event)");
            Debug.Log($"[Boss] OnHornsRaiseComplete() - hornsRaiseInProgress cleared");
        }

        public void OnHornsLowerComplete()
        {
            hornsLowerInProgress = false;
            PushAction("Horns lower complete (via animation event)");
            Debug.Log($"[Boss] OnHornsLowerComplete() - hornsLowerInProgress cleared");
        }

        private void ApplyBossDamageIfPlayerPresent(float damage)
        {
            if (player == null) return;
            if (CombatManager.isParrying && currentAttack != null && currentAttack.Parryable)
            {
                CombatManager.ParrySuccessful();
                Debug.Log($"Boss attack {currentAttack?.Id} parried.");
                return;
            }
            var hs = player.GetComponent<IHealthSystem>();
            if (hs == null || damage <= 0f) return;
            if (CombatManager.isGuarding)
            {
                float reduced = damage * 0.5f;
                hs.LoseHP(reduced);
                Debug.Log($"Boss attack {currentAttack?.Id} guarded. Damage reduced to {reduced}.");
            }
            else
            {
                hs.LoseHP(damage);
            }
        }

        private float GetClipLength(Animator anim, string clipName)
        {
            if (anim == null) return 0f;
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName) return clip.length;
            }
            return 0f;
        }
    }
}
