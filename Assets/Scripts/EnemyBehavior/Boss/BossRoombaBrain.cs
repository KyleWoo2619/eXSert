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
    public sealed class BossRoombaBrain : MonoBehaviour, IParrySink, IQueuedAttacker
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
        
        [Header("Vacuum Suction Settings")]
        [Tooltip("Reference to the VacuumSuctionEffect component (creates one if null)")]
        public VacuumSuctionEffect VacuumSuctionController;
        [Tooltip("Duration of the vacuum suction pull effect")]
        public float VacuumSuctionDuration = 4.0f;
        [Tooltip("Base pull strength for vacuum suction")]
        public float VacuumPullStrength = 10f;
        [Tooltip("Maximum pull strength when player is close")]
        public float VacuumMaxPullStrength = 18f;
        [Tooltip("Effective radius of the vacuum suction")]
        public float VacuumEffectiveRadius = 30f;

        [Tooltip("Speed multiplier when moving to vacuum position (higher = faster approach)")]
        public float VacuumApproachSpeedMultiplier = 3f;

        [Tooltip("How close the boss must get to the vacuum position before starting the attack")]
        public float VacuumPositionThreshold = 2f;

        [Header("Cage Bull Charges")]
        [Tooltip("Speed multiplier when moving to charge start positions (higher = faster approach)")]
        [Range(1f, 10f)] public float ChargeApproachSpeedMultiplier = 2.5f;
        
        [Tooltip("Speed multiplier during static charge dashes (should be VERY fast!)")]
        [Range(2f, 20f)] public float StaticChargeSpeedMultiplier = 4f;
        
        [Tooltip("Speed multiplier during targeted charge at player")]
        [Range(2f, 15f)] public float TargetedChargeSpeedMultiplier = 3f;
        
        [Tooltip("Angular speed multiplier for STATIC charges (high = can turn during dash)")]
        [Range(1f, 10f)] public float StaticChargeAngularMultiplier = 3f;
        
        [Tooltip("Angular speed multiplier for TARGETED charges (low = commits to direction, can miss)")]
        [Range(0.05f, 0.5f)] public float TargetedChargeAngularMultiplier = 0.15f;
        
        [Tooltip("Angular speed multiplier during turn phase (high = snappy turns)")]
        [Range(2f, 20f)] public float TurnAngularMultiplier = 5f;
        
        [Tooltip("Max time to wait at start position before charging (seconds)")]
        [Range(0f, 2f)] public float MaxDelayAtChargeStart = 0.5f;
        
        [Tooltip("Overshoot distance past target for targeted charge")]
        public float ChargeOvershootDistance = 5f;
        [Tooltip("Rest time between static charges and targeted charge")]
        public float ChargeRestDuration = 1.5f;
        [Tooltip("Number of static charge cycles before targeted charge")]
        public int StaticChargeCount = 3;
        [Tooltip("Distance threshold to consider 'arrived' at charge destination")]
        public float ChargeArrivalThreshold = 1.5f;

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
        private PlayerMovement playerMovement; // Cached PlayerMovement component
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

        // Cage Bull charge tracking
        private bool isCharging;
        private bool isTargetedCharge;
        private float baseAgentSpeed;
        private float baseAgentAngularSpeed;
        private float baseAgentAcceleration;
        private bool agentSettingsCached; // Ensures we only cache original settings once

        // Debug vacuum sequence tracking
        private Coroutine debugVacuumCoroutine;
        
        // Debug test mode - prevents normal AI from interfering
        private bool isDebugTestRunning;
        private Coroutine debugTestCoroutine;

        #region IQueuedAttacker Implementation
        
        
        /// <summary>
        /// Returns true - this is a boss enemy.
        /// </summary>
        public bool IsBoss => true;
        
        /// <summary>
        /// Returns true if the boss is alive and able to attack.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                var health = GetComponent<BossHealth>();
                return health == null || health.currentHP > 0f;
            }
        }
        
        /// <summary>
        /// Returns the GameObject for the queue manager.
        /// </summary>
        public GameObject AttackerGameObject => gameObject;
        
        /// <summary>
        /// Check if this boss can attack right now (is at front of queue).
        /// Only relevant if includeBossesInQueue is true.
        /// </summary>
        public bool CanAttackFromQueue()
        {
            if (EnemyAttackQueueManager.Instance == null) return true;
            return EnemyAttackQueueManager.Instance.CanAttack(this);
        }
        
        /// <summary>
        /// Notify the queue that the boss is beginning an attack.
        /// </summary>
        public void NotifyAttackBegin()
        {
            EnemyAttackQueueManager.Instance?.BeginAttack(this);
        }
        
        /// <summary>
        /// Notify the queue that the boss finished attacking.
        /// </summary>
        public void NotifyAttackEnd()
        {
            EnemyAttackQueueManager.Instance?.FinishAttack(this);
        }
        
        /// <summary>
        /// Register this boss with the attack queue.
        /// </summary>
        public void RegisterWithAttackQueue()
        {
            EnemyAttackQueueManager.Instance?.Register(this);
        }
        
        /// <summary>
        /// Unregister this boss from the attack queue.
        /// </summary>
        public void UnregisterFromAttackQueue()
        {
            EnemyAttackQueueManager.Instance?.Unregister(this);
        }
        
        #endregion

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
            
            // Cache player reference - search hierarchy for PlayerMovement
            CachePlayerReference();
            
            // Cache original agent settings EARLY, before any modifications
            CacheAgentSettings();
            
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

        /// <summary>
        /// Caches the player Transform and PlayerMovement component.
        /// Searches the hierarchy since PlayerMovement may be on a parent of the tagged object.
        /// </summary>
        private void CachePlayerReference()
        {
            // First try PlayerPresenceManager if available
            if (PlayerPresenceManager.IsPlayerPresent)
            {
                player = PlayerPresenceManager.PlayerTransform;
            }
            else
            {
                // Fallback to FindWithTag
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }

            if (player != null)
            {
                // PlayerMovement might be on the tagged object, parent, or child
                playerMovement = player.GetComponent<PlayerMovement>();
                if (playerMovement == null)
                {
                    playerMovement = player.GetComponentInParent<PlayerMovement>();
                }
                if (playerMovement == null)
                {
                    playerMovement = player.GetComponentInChildren<PlayerMovement>();
                }

                // Use PlayerMovement's transform as the canonical player reference
                if (playerMovement != null)
                {
                    player = playerMovement.transform;
                    Debug.Log($"[BossRoombaBrain] Player cached: {player.name} (has PlayerMovement)");
                }
                else
                {
                    Debug.LogWarning($"[BossRoombaBrain] Player found ({player.name}) but no PlayerMovement component!");
                }
            }
            else
            {
                Debug.LogWarning("[BossRoombaBrain] No player found in scene during Awake!");
            }
        }

        /// <summary>
        /// Gets the cached PlayerMovement component. Re-caches if null.
        /// </summary>
        public PlayerMovement GetPlayerMovement()
        {
            if (playerMovement == null)
            {
                CachePlayerReference();
            }
            return playerMovement;
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
            
            // Register with attack queue system
            RegisterWithAttackQueue();
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
            
            // Unregister from attack queue system
            UnregisterFromAttackQueue();
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

            // CRITICAL: Stop the controller's follow behavior so it doesn't override our destination
            ctrl.StopFollowing();
#if UNITY_EDITOR
            Debug.Log("[Boss] Stopped controller follow behavior for vacuum sequence");
#endif

            // Determine the target position for the vacuum attack
            // IMPORTANT: Cache the position as a Vector3, not a Transform reference
            // This prevents issues if VacuumPosition is parented to the boss
            Vector3 vacuumTargetPosition;
            if (VacuumPosition != null)
            {
                vacuumTargetPosition = VacuumPosition.position;
#if UNITY_EDITOR
                Debug.Log($"[Boss] Vacuum target: VacuumPosition at {vacuumTargetPosition}");
#endif
            }
            else if (ArenaCenterBounds != null)
            {
                vacuumTargetPosition = ArenaCenterBounds.bounds.center;
#if UNITY_EDITOR
                Debug.Log($"[Boss] Vacuum target: ArenaCenterBounds center at {vacuumTargetPosition}");
#endif
            }
            else
            {
                // No valid position - skip pathfinding and do vacuum from current position
                Debug.LogWarning("[Boss] No VacuumPosition or ArenaCenterBounds assigned! Doing vacuum from current position.");
                vacuumTargetPosition = transform.position;
            }

            // Flatten Y to match boss height (NavMesh movement is 2D)
            vacuumTargetPosition.y = transform.position.y;

            // Pathfind to the vacuum position
            float distanceToTarget = Vector3.Distance(transform.position, vacuumTargetPosition);
#if UNITY_EDITOR
            Debug.Log($"[Boss] Distance to vacuum position: {distanceToTarget:F1}m (boss at {transform.position}, target at {vacuumTargetPosition})");
#endif

            if (distanceToTarget > VacuumPositionThreshold)
            {
                // Store original agent settings to restore later
                float originalSpeed = agent.speed;
                float originalAngularSpeed = agent.angularSpeed;
                float originalAcceleration = agent.acceleration;
                float originalStoppingDistance = agent.stoppingDistance;
                
                // Boost speed, angular speed, and acceleration for vacuum approach
                // Set small stopping distance so it gets close to exact position
                agent.speed = originalSpeed * VacuumApproachSpeedMultiplier;
                agent.angularSpeed = originalAngularSpeed * VacuumApproachSpeedMultiplier; // Turn faster to match movement speed
                agent.acceleration = originalAcceleration * VacuumApproachSpeedMultiplier; // Accelerate faster
                agent.stoppingDistance = 0.5f;
                
#if UNITY_EDITOR
                Debug.Log($"[Boss] Vacuum approach: speed {originalSpeed:F1}→{agent.speed:F1}, angularSpeed {originalAngularSpeed:F1}→{agent.angularSpeed:F1}, accel {originalAcceleration:F1}→{agent.acceleration:F1}");
#endif

                // Ensure agent is ready to move
                agent.isStopped = false;
                agent.updateRotation = true;
                bool pathSet = agent.SetDestination(vacuumTargetPosition);
                
#if UNITY_EDITOR
                Debug.Log($"[Boss] SetDestination returned: {pathSet}, agent.pathPending: {agent.pathPending}");
#endif
                PushAction($"Moving to vacuum position ({distanceToTarget:F1}m away)...");

                // Wait for path to be calculated
                float pathWaitTimer = 0f;
                while (agent.pathPending && pathWaitTimer < 2f)
                {
                    pathWaitTimer += Time.deltaTime;
                    yield return null;
                }

#if UNITY_EDITOR
                Debug.Log($"[Boss] Path status: {agent.pathStatus}, hasPath: {agent.hasPath}, pathPending: {agent.pathPending}");
#endif

                // Wait until we reach the position (with timeout)
                float moveTimeout = 15f; // Increased timeout since we're moving faster
                float moveTimer = 0f;
                float logTimer = 0f;
                
                while (Vector3.Distance(transform.position, vacuumTargetPosition) > VacuumPositionThreshold && moveTimer < moveTimeout)
                {
                    // Check if agent has a valid path
                    if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                    {
                        Debug.LogWarning("[Boss] NavMesh path invalid! Skipping movement to vacuum position.");
                        break;
                    }
                    
                    // Check if agent stopped moving but is still far away (stoppingDistance issue)
                    if (agent.remainingDistance < agent.stoppingDistance && agent.velocity.sqrMagnitude < 0.01f)
                    {
                        // Agent thinks it arrived but we're still not close enough - force re-path
                        float currentDistCheck = Vector3.Distance(transform.position, vacuumTargetPosition);
                        if (currentDistCheck > VacuumPositionThreshold)
                        {
#if UNITY_EDITOR
                            Debug.Log($"[Boss] Agent stopped early at {currentDistCheck:F1}m - forcing re-path");
#endif
                            agent.SetDestination(vacuumTargetPosition);
                        }
                    }
                    
                    if (!agent.hasPath && !agent.pathPending)
                    {
                        Debug.LogWarning("[Boss] Agent has no path and none pending! Retrying SetDestination...");
                        agent.SetDestination(vacuumTargetPosition);
                    }

                    // Log progress every 1 second
#if UNITY_EDITOR
                    logTimer += Time.deltaTime;
                    if (logTimer >= 1f)
                    {
                        float currentDist = Vector3.Distance(transform.position, vacuumTargetPosition);
                        Debug.Log($"[Boss] Moving... dist={currentDist:F1}m, velocity={agent.velocity.magnitude:F1}, remainingDist={agent.remainingDistance:F1}");
                        logTimer = 0f;
                    }
#endif
                    
                    moveTimer += Time.deltaTime;
                    yield return null;
                }

                // Restore original agent settings (ALWAYS happens regardless of how loop exited)
                agent.speed = originalSpeed;
                agent.angularSpeed = originalAngularSpeed;
                agent.acceleration = originalAcceleration;
                agent.stoppingDistance = originalStoppingDistance;
#if UNITY_EDITOR
                Debug.Log($"[Boss] Agent settings RESTORED: speed={agent.speed:F1}, angularSpeed={agent.angularSpeed:F1}, accel={agent.acceleration:F1}");
#endif

                float finalDist = Vector3.Distance(transform.position, vacuumTargetPosition);
                if (moveTimer >= moveTimeout)
                {
                    Debug.LogWarning($"[Boss] Timed out moving to vacuum position after {moveTimeout}s (still {finalDist:F1}m away)");
                }
                else if (finalDist <= VacuumPositionThreshold)
                {
                    PushAction($"Reached vacuum position (dist={finalDist:F1}m)");
                }
#if UNITY_EDITOR
                else
                {
                    Debug.Log($"[Boss] Exited movement loop at {finalDist:F1}m (threshold={VacuumPositionThreshold:F1}m)");
                }
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("[Boss] Already at vacuum position, skipping movement");
#endif
            }

            // Stop movement before starting the attack
            agent.isStopped = true;

            // Execute vacuum attack WITHOUT incrementing the attack counter
            // (it's a special transition attack, not part of the regular attack cycle)
            yield return ExecuteVacuumAttackWithoutCounter();

            // Resume movement
            agent.isStopped = false;

            bool playerInCenter = false;
            if (player != null && ArenaCenterBounds != null)
            {
                playerInCenter = ArenaCenterBounds.bounds.Contains(player.position);
                Debug.Log($"[Boss] Player in center bounds: {playerInCenter} (player pos: {player.position})");
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

            // Resume the controller's follow behavior now that vacuum is complete
            ctrl.StartFollowingPlayer(0.1f);
#if UNITY_EDITOR
            Debug.Log("[Boss] Vacuum sequence END - follow behavior restarted");
#endif
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
            yield return WaitForSecondsCache.Get(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));
            
            // START VACUUM SUCTION EFFECT during the active phase
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            
            // Calculate active phase duration
            float activePhaseDuration = a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName);
            
            // Use the configured suction duration, or fall back to animation length
            float suctionDuration = VacuumSuctionDuration > 0 ? VacuumSuctionDuration : activePhaseDuration;
            
            // Start the vacuum suction
            StartVacuumSuction(suctionDuration);
            PushAction($"Vacuum suction ACTIVE for {suctionDuration}s");
            
            // Wait for the active phase to complete
            yield return WaitForSecondsCache.Get(Mathf.Max(activePhaseDuration, suctionDuration));
            
            // Stop suction if still active
            StopVacuumSuction();
            
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return WaitForSecondsCache.Get(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));

            // PROPERLY HANDLED: Lower horns AFTER vacuum attack (just like arms deploy/retract)
            yield return LowerHornsIfNeeded();

            MarkCooldown(a);
        }

        private void StartVacuumSuction(float duration)
        {
            Debug.Log($"[BossRoombaBrain] StartVacuumSuction called - duration={duration}");
            
            // Ensure player is cached
            if (player == null || playerMovement == null)
            {
                CachePlayerReference();
            }
            
            // Create or get the vacuum suction controller
            if (VacuumSuctionController == null)
            {
                VacuumSuctionController = GetComponent<VacuumSuctionEffect>();
                if (VacuumSuctionController == null)
                {
                    VacuumSuctionController = gameObject.AddComponent<VacuumSuctionEffect>();
                    Debug.Log("[BossRoombaBrain] Created VacuumSuctionEffect component");
                }
            }

            // Configure the suction effect
            VacuumSuctionController.BasePullStrength = VacuumPullStrength;
            VacuumSuctionController.MaxPullStrength = VacuumMaxPullStrength;
            VacuumSuctionController.EffectiveRadius = VacuumEffectiveRadius;
            VacuumSuctionController.ArenaManager = ArenaManager;
            
            // Pass cached player references to avoid repeated FindWithTag calls
            VacuumSuctionController.SetPlayerReferences(player, playerMovement);
            
            
            // Set the suction target to the arena center bounds center
            if (ArenaCenterBounds != null)
            {
                // Create a temporary transform for the target if needed
                if (VacuumSuctionController.SuctionTarget == null)
                {
                    var targetObj = new GameObject("VacuumSuctionTarget");
                    targetObj.transform.SetParent(transform);
                    VacuumSuctionController.SuctionTarget = targetObj.transform;
                }
                VacuumSuctionController.SuctionTarget.position = ArenaCenterBounds.bounds.center;
                Debug.Log($"[BossRoombaBrain] Suction target set to ArenaCenterBounds center: {ArenaCenterBounds.bounds.center}");
            }
            else if (VacuumPosition != null)
            {
                VacuumSuctionController.SuctionTarget = VacuumPosition;
                Debug.Log($"[BossRoombaBrain] Suction target set to VacuumPosition: {VacuumPosition.position}");
            }
            else
            {
                // Fallback: use the boss's current position as the suction target
                if (VacuumSuctionController.SuctionTarget == null)
                {
                    var targetObj = new GameObject("VacuumSuctionTarget");
                    targetObj.transform.SetParent(transform);
                    VacuumSuctionController.SuctionTarget = targetObj.transform;
                }
                VacuumSuctionController.SuctionTarget.position = transform.position;
                Debug.LogWarning($"[BossRoombaBrain] Neither ArenaCenterBounds nor VacuumPosition set! Using boss position as fallback: {transform.position}");
            }

            // Start the suction
            VacuumSuctionController.StartSuction(duration);
        }

        private void StopVacuumSuction()
        {
            if (VacuumSuctionController != null)
            {
                VacuumSuctionController.StopSuction();
            }
        }

        private IEnumerator CageBullLoop()
        {
            PushAction("Cage Bull loop START");

            // Cache base agent settings for charge modifications
            CacheAgentSettings();

            if (RequirePlayerOnTopForSpin && IsMountedWithGrace())
            {
                yield return ExecuteArmPokeSequenceThenSpin();
                yield break;
            }

            // Execute the charge combo system
            if (ArenaManager != null && ArenaManager.HasValidCombos)
            {
                yield return ExecuteChargeCombo();
            }
            else
            {
                PushAction("No valid combos configured - skipping static charges");
            }

            // Brief rest before targeted charge
            if (!isStunned)
            {
                yield return WaitForSecondsCache.Get(ChargeRestDuration);
                // Targeted charge at player with overshoot (can hit pillars!)
                yield return ExecuteTargetedChargeWithOvershoot();
            }

            yield return null;
        }

        /// <summary>
        /// Executes a random charge combo from the ArenaManager's combo list.
        /// </summary>
        private IEnumerator ExecuteChargeCombo()
        {
            if (ArenaManager == null)
            {
                PushAction("No ArenaManager for combo execution");
                yield break;
            }

            var combo = ArenaManager.GetRandomCombo();
            if (combo == null || !combo.IsValid)
            {
                PushAction("No valid combo found - skipping combo");
                yield break;
            }

            PushAction($"Executing combo: {combo.ComboName} ({combo.SegmentCount} segments)");

            var segments = ArenaManager.GetComboSegments(combo);
            if (segments == null)
            {
                PushAction("Failed to get combo segments");
                yield break;
            }

            // Execute each segment in the combo
            for (int i = 0; i < segments.Length; i++)
            {
                if (isStunned) break;

                var (start, end) = segments[i];
                PushAction($"Combo segment {i + 1}/{segments.Length}: charging...");

                // Check if we need to move to start position
                // (skip if we're already there from the previous segment's end)
                float distToStart = Vector3.Distance(transform.position, start);
                if (distToStart > ChargeArrivalThreshold)
                {
                    // Move to start position
                    agent.isStopped = false;
                    agent.SetDestination(start);
                    while (Vector3.Distance(transform.position, start) > ChargeArrivalThreshold && !isStunned)
                    {
                        yield return null;
                    }
                }

                if (isStunned) break;

                // Turn to face end position
                yield return TurnToFacePosition(end);

                if (isStunned) break;

                // Charge to end
                yield return ExecuteChargeDash(end, isTargeted: false);

                // Brief pause between segments
                yield return WaitForSecondsCache.Get(0.2f);
            }
        }

        /// <summary>
        /// Caches the original NavMeshAgent settings. Only caches ONCE to prevent
        /// storing modified values when called multiple times.
        /// </summary>
        private void CacheAgentSettings()
        {
            // Only cache once - prevent storing modified values
            if (agentSettingsCached)
                return;
            
            if (agent != null)
            {
                baseAgentSpeed = agent.speed;
                baseAgentAngularSpeed = agent.angularSpeed;
                baseAgentAcceleration = agent.acceleration;
                
                // Validate - if any value is 0 or very low, use sensible defaults
                if (baseAgentSpeed < 1f)
                {
                    Debug.LogWarning($"[BossRoombaBrain] Agent base speed was {baseAgentSpeed}, using default 5");
                    baseAgentSpeed = 5f;
                }
                if (baseAgentAngularSpeed < 10f)
                {
                    Debug.LogWarning($"[BossRoombaBrain] Agent base angular speed was {baseAgentAngularSpeed}, using default 120");
                    baseAgentAngularSpeed = 120f;
                }
                if (baseAgentAcceleration < 1f)
                {
                    Debug.LogWarning($"[BossRoombaBrain] Agent base acceleration was {baseAgentAcceleration}, using default 8");
                    baseAgentAcceleration = 8f;
                }
                
                agentSettingsCached = true;
                Debug.Log($"[BossRoombaBrain] Cached agent settings - Speed: {baseAgentSpeed}, Angular: {baseAgentAngularSpeed}, Accel: {baseAgentAcceleration}");
            }
        }

        private void RestoreAgentSettings()
        {
            if (agent != null)
            {
                agent.speed = baseAgentSpeed;
                agent.angularSpeed = baseAgentAngularSpeed;
                agent.acceleration = baseAgentAcceleration;
                agent.updateRotation = true; // Re-enable auto-rotation
            }
            isCharging = false;
            isTargetedCharge = false;
        }

        /// <summary>
        /// Apply settings for approaching charge start positions (fast movement).
        /// </summary>
        private void ApplyApproachSettings()
        {
            if (agent != null)
            {
                agent.speed = baseAgentSpeed * ChargeApproachSpeedMultiplier;
                agent.angularSpeed = baseAgentAngularSpeed * 2f; // Good turning while approaching
                agent.acceleration = baseAgentAcceleration * ChargeApproachSpeedMultiplier * 2f; // High acceleration for snappy movement
                agent.autoBraking = false; // Don't slow down early - we control stopping
                agent.stoppingDistance = 0.5f; // Very small - we handle arrival ourselves
                agent.updateRotation = true; // Allow rotation during approach
            }
        }

        /// <summary>
        /// Apply settings for static charge dashes (fast, with good turning).
        /// </summary>
        private void ApplyStaticChargeSettings()
        {
            if (agent != null)
            {
                agent.speed = baseAgentSpeed * StaticChargeSpeedMultiplier;
                agent.angularSpeed = baseAgentAngularSpeed * StaticChargeAngularMultiplier;
                agent.acceleration = baseAgentAcceleration * StaticChargeSpeedMultiplier * 2f;
                agent.autoBraking = false;
                agent.stoppingDistance = 0.5f;
                agent.updateRotation = true; // Allow rotation during static charges (can adjust)
            }
        }


        /// <summary>
        /// Apply settings for targeted charge at player (fast, but commits to direction).
        /// </summary>
        private void ApplyTargetedChargeSettings()
        {
            if (agent != null)
            {
                agent.speed = baseAgentSpeed * TargetedChargeSpeedMultiplier;
                agent.angularSpeed = baseAgentAngularSpeed * TargetedChargeAngularMultiplier;
                agent.acceleration = baseAgentAcceleration * TargetedChargeSpeedMultiplier * 2f;
                agent.autoBraking = false;
                agent.stoppingDistance = 0.5f;
                agent.updateRotation = false; // DON'T auto-rotate during targeted charge - we control rotation
            }
        }

        private void ApplyTurnSettings()
        {
            if (agent != null)
            {
                agent.speed = baseAgentSpeed * 0.1f; // Nearly stopped during turn
                agent.angularSpeed = baseAgentAngularSpeed * TurnAngularMultiplier;
                agent.updateRotation = true; // Allow rotation during turns
            }
        }

        /// <summary>
        /// Turn in place to face a target position.
        /// High angular speed, minimal movement.
        /// </summary>
        private IEnumerator TurnToFacePosition(Vector3 targetPosition)
        {
            Vector3 dirToTarget = (targetPosition - transform.position).normalized;
            dirToTarget.y = 0f;

            if (dirToTarget.sqrMagnitude < 0.001f)
            {
                Debug.Log($"[Boss] TIMING: TurnToFace - already facing target, skipping");
                yield break;
            }

            // Apply turn settings
            ApplyTurnSettings();
            
            if (agent != null)
            {
                agent.isStopped = true; // Stop movement during turn
            }

            Quaternion targetRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
            float initialAngle = Quaternion.Angle(transform.rotation, targetRotation);
            float turnTimer = 0f;
            float maxTurnTime = 1f; // Max 1 second to turn

            Debug.Log($"[Boss] TIMING: TurnToFace - initial angle: {initialAngle:F1}°, angular speed: {(agent != null ? agent.angularSpeed : 0)}");

            while (turnTimer < maxTurnTime && !isStunned)
            {
                // Manually rotate towards target
                float angularSpeed = agent != null ? agent.angularSpeed : 360f;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    angularSpeed * Time.deltaTime);

                float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
                if (angleDiff < 5f)
                    break;

                turnTimer += Time.deltaTime;
                yield return null;
            }

            // Snap to final rotation
            transform.rotation = targetRotation;
            Debug.Log($"[Boss] TIMING: TurnToFace - completed in {turnTimer:F2}s");
        }

        /// <summary>
        /// Execute a charge dash to destination.
        /// Static charges: high speed, HIGH angular speed (can adjust during dash)
        /// Targeted charges: high speed, LOW angular speed (commits to direction, can miss)
        /// </summary>
        private IEnumerator ExecuteChargeDash(Vector3 destination, bool isTargeted)
        {
            isCharging = true;
            isTargetedCharge = isTargeted;

            // Apply appropriate charge settings
            if (isTargeted)
            {
                ApplyTargetedChargeSettings(); // Low angular - commits to direction
            }
            else
            {
                ApplyStaticChargeSettings(); // High angular - can turn during dash
            }

            // Trigger animation
            var chargeAttack = isTargeted ? TargetedCharge : StaticCharge;
            if (animator != null && !string.IsNullOrEmpty(chargeAttack.AnimatorTriggerOnActive))
                animator.SetTrigger(chargeAttack.AnimatorTriggerOnActive);

            if (isTargeted)
            {
                // TARGETED CHARGE: Completely bypass NavMeshAgent pathfinding
                // Move in a STRAIGHT LINE towards the committed destination - no path recalculation!
                Vector3 startPos = transform.position;
                Vector3 chargeDirection = (destination - startPos).normalized;
                chargeDirection.y = 0; // Keep horizontal
                
                float totalDistance = Vector3.Distance(startPos, destination);
                float chargeSpeed = agent.speed; // Use the speed we set in ApplyTargetedChargeSettings
                
                // Lock rotation to face charge direction
                if (chargeDirection.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(chargeDirection, Vector3.up);
                }
                
                // STOP the NavMeshAgent - we're taking manual control
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                
                PushAction($"Targeted charge DASH to {destination} (MANUAL movement, speed {chargeSpeed})");

                // Wait until arrival or interruption
                float maxChargeTime = 5f; // Safety timeout
                float chargeTimer = 0f;
                float distanceTraveled = 0f;
                Vector3 lastPosition = transform.position;

                while (chargeTimer < maxChargeTime && !isStunned && distanceTraveled < totalDistance)
                {
                    // Keep rotation locked to original charge direction
                    if (chargeDirection.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(chargeDirection, Vector3.up);
                    }
                    
                    float moveDistance = chargeSpeed * Time.deltaTime;
                    Vector3 moveVector = chargeDirection * moveDistance;
                    
                    // Manually move in the locked direction using NavMeshAgent.Move()
                    // NavMesh Obstacle carving on walls will prevent passing through them
                    agent.Move(moveVector);
                    
                    // Check if we actually moved - if not, we hit something (wall/obstacle)
                    float actualMovement = Vector3.Distance(transform.position, lastPosition);
                    if (actualMovement < moveDistance * 0.1f && chargeTimer > 0.1f)
                    {
                        // We're blocked - likely hit a wall
                        PushAction($"Targeted charge BLOCKED (moved {actualMovement:F2} of {moveDistance:F2})");
                        break;
                    }
                    lastPosition = transform.position;
                    
                    distanceTraveled += actualMovement;
                    
                    // Also check actual distance to destination as backup
                    float distToTarget = Vector3.Distance(transform.position, destination);
                    if (distToTarget <= ChargeArrivalThreshold)
                    {
                        PushAction("Targeted charge arrived at destination (overshoot complete)");
                        break;
                    }

                    chargeTimer += Time.deltaTime;
                    yield return null;
                }
                
                if (chargeTimer >= maxChargeTime)
                {
                    PushAction($"Targeted charge TIMEOUT after {chargeTimer:F1}s");
                }
            }
            else
            {
                // STATIC CHARGE: Normal NavMeshAgent behavior with turning allowed
                agent.isStopped = false;
                agent.SetDestination(destination);


                PushAction($"Static charge DASH to {destination}");

                // Wait until arrival or interruption
                float maxChargeTime = 5f; // Safety timeout
                float chargeTimer = 0f;

                while (chargeTimer < maxChargeTime && !isStunned)
                {
                    float distToTarget = Vector3.Distance(transform.position, destination);

                    if (distToTarget <= ChargeArrivalThreshold)
                    {
                        PushAction("Charge arrived at destination");
                        break;
                    }

                    chargeTimer += Time.deltaTime;
                    yield return null;
                }
            }

            // Restore settings
            RestoreAgentSettings();
            agent.isStopped = true;


            // Trigger recovery animation
            if (animator != null && !string.IsNullOrEmpty(chargeAttack.AnimatorTriggerOnRecovery))
                animator.SetTrigger(chargeAttack.AnimatorTriggerOnRecovery);
        }

        /// <summary>
        /// Execute a targeted charge at the player with overshoot.
        /// This charge can result in pillar collision if player dodges!
        /// </summary>
        private IEnumerator ExecuteTargetedChargeWithOvershoot()
        {
            if (player == null) yield break;

            PushAction("Targeted charge START (with overshoot)");
            currentAttack = TargetedCharge;

            // Windup animation
            if (animator != null && !string.IsNullOrEmpty(TargetedCharge.AnimatorTriggerOnWindup))
                animator.SetTrigger(TargetedCharge.AnimatorTriggerOnWindup);

            yield return WaitForSecondsCache.Get(TargetedCharge.WindupSpeedMultiplier * GetClipLength(animator, TargetedCharge.WindupClipName));

            if (isStunned) yield break;

            // Calculate overshoot target (past player position)
            Vector3 playerPos = player.position;
            Vector3 dirToPlayer = (playerPos - transform.position).normalized;
            Vector3 overshootTarget = playerPos + dirToPlayer * ChargeOvershootDistance;

            // Turn to face player
            yield return TurnToFacePosition(playerPos);

            if (isStunned) yield break;

            // Execute the charge with overshoot
            yield return ExecuteChargeDash(overshootTarget, isTargeted: true);

            MarkCooldown(TargetedCharge);
        }

        /// <summary>
        /// Property to check if boss is currently in a charge (for collision detection).
        /// </summary>
        public bool IsCharging => isCharging;

        /// <summary>
        /// Property to check if current charge is a targeted charge (can stun on pillar hit).
        /// </summary>
        public bool IsTargetedCharge => isTargetedCharge;

        public void OnPillarCollision(int pillarIndex)
        {
            if (form != RoombaForm.CageBull) return;

            // Only stun during targeted charges
            if (!isTargetedCharge)
            {
                PushAction($"Pillar {pillarIndex} collision during static charge - no stun");
                return;
            }

            PushAction($"Pillar {pillarIndex} collision during TARGETED charge - STUNNED!");

            // Stop the charge immediately
            isCharging = false;
            isTargetedCharge = false;
            RestoreAgentSettings();
            agent.isStopped = true;

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
            yield return WaitForSecondsCache.Get(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return WaitForSecondsCache.Get(a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return WaitForSecondsCache.Get(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));

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
            yield return WaitForSecondsCache.Get(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            Vector3 overshootTarget = player.position + dirToPlayer * DashOvershootDistance;

            float baseSpeed = agent.speed;
            agent.speed = DashSpeed;

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);

            agent.SetDestination(overshootTarget);

            yield return WaitForSecondsCache.Get(a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName));

            agent.speed = baseSpeed;

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return WaitForSecondsCache.Get(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));

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
            yield return WaitForSecondsCache.Get(a.WindupSpeedMultiplier * GetClipLength(animator, a.WindupClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);

            if (player != null && IsPlayerMounted())
            {
                FlingPlayer();
            }

            yield return WaitForSecondsCache.Get(a.ActiveSpeedMultiplier * GetClipLength(animator, a.ActiveClipName));
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return WaitForSecondsCache.Get(a.RecoverySpeedMultiplier * GetClipLength(animator, a.RecoveryClipName));
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

            yield return WaitForSecondsCache.Get(windupDuration);
            
            // Trigger stun active phase
            if (animator != null && !string.IsNullOrEmpty(TriggerStunActive))
            {
                animator.SetTrigger(TriggerStunActive);
                PushAction($"Stun animation: Active triggered");
            }

            yield return WaitForSecondsCache.Get(activeDuration);
            
            // Trigger stun recovery phase
            if (animator != null && !string.IsNullOrEmpty(TriggerStunRecovery))
            {
                animator.SetTrigger(TriggerStunRecovery);
                PushAction($"Stun animation: Recovery triggered");
            }

            yield return WaitForSecondsCache.Get(recoveryDuration);

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

        [ContextMenu("Debug: Test Vacuum Suction Only")]
        public void DebugTestVacuumSuctionOnly()
        {
            // Direct test of vacuum suction without needing the full AI loop
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BossRoombaBrain] Debug: Test Vacuum Suction Only requires Play Mode!");
                return;
            }
            
            // Make sure player exists
            if (player == null)
            {
                player = GameObject.FindWithTag("Player")?.transform;
                if (player == null)
                {
                    Debug.LogError("[BossRoombaBrain] DEBUG: Cannot test vacuum - no Player found in scene!");
                    return;
                }
            }
            
            float testDuration = VacuumSuctionDuration > 0 ? VacuumSuctionDuration : 4f;
            Debug.Log($"[BossRoombaBrain] DEBUG: Starting vacuum suction test - duration={testDuration}, player={player.name}");
            StartVacuumSuction(testDuration);
            PushAction($"DEBUG: Vacuum suction started for {testDuration}s (suction only, no animations)");
        }


        [ContextMenu("Debug: Execute Full Vacuum Sequence")]
        public void DebugExecuteFullVacuumSequence()
        {
            // Direct execution of the full vacuum sequence (pathfind + animation + suction)
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BossRoombaBrain] Debug: Execute Full Vacuum Sequence requires Play Mode!");
                return;
            }
            
            // Make sure player exists
            if (player == null)
            {
                player = GameObject.FindWithTag("Player")?.transform;
                if (player == null)
                {
                    Debug.LogError("[BossRoombaBrain] DEBUG: Cannot execute vacuum - no Player found in scene!");
                    return;
                }
            }
            
            // CRITICAL: Stop the main AI loop so it doesn't interfere
            if (loop != null)
            {
                StopCoroutine(loop);
                loop = null;
#if UNITY_EDITOR
                Debug.Log("[BossRoombaBrain] DEBUG: Stopped main AI loop");
#endif
            }
            
            // Stop any existing debug vacuum sequence (prevents double-press issues)
            if (debugVacuumCoroutine != null)
            {
                StopCoroutine(debugVacuumCoroutine);
                debugVacuumCoroutine = null;
#if UNITY_EDITOR
                Debug.Log("[BossRoombaBrain] DEBUG: Stopped previous debug vacuum sequence");
#endif
            }
            
            // Stop any current attack in progress
            if (currentAttackRoutine != null)
            {
                StopCoroutine(currentAttackRoutine);
                currentAttackRoutine = null;
#if UNITY_EDITOR
                Debug.Log("[BossRoombaBrain] DEBUG: Stopped current attack");
#endif
            }
            
            // Stop arms retract routine if running
            if (armsRetractRoutine != null)
            {
                StopCoroutine(armsRetractRoutine);
                armsRetractRoutine = null;
            }
            
#if UNITY_EDITOR
            Debug.Log("[BossRoombaBrain] DEBUG: Starting full vacuum sequence (pathfind → animation → suction)");
#endif
            debugVacuumCoroutine = StartCoroutine(DebugVacuumSequenceWrapper());
        }

        /// <summary>
        /// Wrapper that executes vacuum sequence and then restarts the AI loop.
        /// </summary>
        private IEnumerator DebugVacuumSequenceWrapper()
        {
            // Execute the vacuum sequence
            yield return ExecuteVacuumSequence();
            
#if UNITY_EDITOR
            Debug.Log("[BossRoombaBrain] DEBUG: Vacuum sequence complete, restarting AI loop and following");
#endif
            
            // Clear the debug coroutine reference
            debugVacuumCoroutine = null;
            
            // Restart the controller's follow behavior
            ctrl.StartFollowingPlayer(0.1f);
            
            // Restart the main AI loop
            if (loop == null)
            {
                loop = StartCoroutine(FormLoop());
            }
        }

        [ContextMenu("Debug: Stop Vacuum Suction")]
        public void DebugStopVacuumSuction()
        {
            StopVacuumSuction();
            PushAction("DEBUG: Vacuum suction stopped");
        }

        [ContextMenu("Debug: Return to Duelist Form")]
        public void DebugReturnToDuelistForm()
        {
            if (form == RoombaForm.CageBull)
            {
                // Stop any active charge
                isCharging = false;
                isTargetedCharge = false;
                RestoreAgentSettings();
                
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

        [ContextMenu("Debug: Force Cage Bull Form")]
        public void DebugForceCageBullForm()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BossRoombaBrain] Debug: Force Cage Bull Form requires Play Mode!");
                return;
            }
            
            if (form == RoombaForm.CageBull)
            {
                PushAction("DEBUG: Already in Cage Bull form");
                return;
            }

            // Stop current AI loop
            if (loop != null)
            {
                StopCoroutine(loop);
                loop = null;
            }

            // Raise walls
            if (ArenaManager != null)
            {
                ArenaManager.RaiseWalls(true);
            }

            // Switch to Cage Bull
            form = RoombaForm.CageBull;
            
            // Raise horns
            StartCoroutine(RaiseHornsIfNeeded());
            
            // Deactivate alarm
            if (!alarmDestroyed)
            {
                ctrl.DeactivateAlarm();
            }

            // Restart the AI loop
            loop = StartCoroutine(FormLoop());
            
            PushAction("DEBUG: Forced Cage Bull form");
        }


        [ContextMenu("Debug: Execute Single Targeted Charge")]
        public void DebugExecuteTargetedCharge()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BossRoombaBrain] Debug: Execute Targeted Charge requires Play Mode!");
                return;
            }

            if (player == null)
            {
                CachePlayerReference();
                if (player == null)
                {
                    Debug.LogError("[BossRoombaBrain] DEBUG: Cannot charge - no Player found!");
                    return;
                }
            }

            // Ensure we're in CageBull form first
            StartCoroutine(EnsureCageBullFormThenExecute(() => StartCoroutine(ExecuteTargetedChargeWithOvershoot())));
            PushAction("DEBUG: Started targeted charge (ensuring CageBull form)");
        }

        [ContextMenu("Debug: Simulate Pillar Collision")]
        public void DebugSimulatePillarCollision()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BossRoombaBrain] Debug: Simulate Pillar Collision requires Play Mode!");
                return;
            }

            // Fake a targeted charge state
            isTargetedCharge = true;
            
            // Trigger pillar collision
            OnPillarCollision(0);
            
            PushAction("DEBUG: Simulated pillar collision");
        }

        #region Debug Lane Combo Tests

        [ContextMenu("Debug: Test Lane Combo 0")]
        public void DebugTestLaneCombo0() => DebugTestLaneCombo(0);

        [ContextMenu("Debug: Test Lane Combo 1")]
        public void DebugTestLaneCombo1() => DebugTestLaneCombo(1);

        [ContextMenu("Debug: Test Lane Combo 2")]
        public void DebugTestLaneCombo2() => DebugTestLaneCombo(2);

        [ContextMenu("Debug: Stop Current Test")]
        public void DebugStopCurrentTest()
        {
            if (debugTestCoroutine != null)
            {
                StopCoroutine(debugTestCoroutine);
                debugTestCoroutine = null;
            }
            isDebugTestRunning = false;
            RestoreAgentSettings();
            PushAction("DEBUG: Test stopped manually");
        }

        /// <summary>
        /// Tests a specific lane combo by index. Ensures CageBull form first.
        /// </summary>
        public void DebugTestLaneCombo(int comboIndex)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BossRoombaBrain] Debug: Test Lane Combo requires Play Mode!");
                return;
            }

            if (ArenaManager == null)
            {
                Debug.LogError("[BossRoombaBrain] DEBUG: Cannot test combo - ArenaManager is null!");
                return;
            }

            // Check if combo exists
            var combo = ArenaManager.GetCombo(comboIndex);
            if (combo == null)
            {
                Debug.LogError($"[BossRoombaBrain] DEBUG: Lane Combo {comboIndex} does not exist! Only {ArenaManager.ComboCount} combos configured.");
                return;
            }

            if (!combo.IsValid)
            {
                Debug.LogError($"[BossRoombaBrain] DEBUG: Lane Combo {comboIndex} ('{combo.ComboName}') is invalid - check segment transforms!");
                return;
            }

            // Stop any existing debug test
            if (debugTestCoroutine != null)
            {
                StopCoroutine(debugTestCoroutine);
            }

            PushAction($"DEBUG: Testing Lane Combo {comboIndex} ('{combo.ComboName}')");
            
            // Start the debug test
            debugTestCoroutine = StartCoroutine(DebugTestComboCoroutine(comboIndex));
        }

        /// <summary>
        /// Main debug test coroutine that handles form switching and combo execution.
        /// </summary>
        private IEnumerator DebugTestComboCoroutine(int comboIndex)
        {
            // Set flag to prevent normal AI from interfering
            isDebugTestRunning = true;
            
            // Stop the main AI loop
            if (loop != null)
            {
                StopCoroutine(loop);
                loop = null;
            }
            
            // Stop the agent from following player
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            
            // Wait a frame for everything to settle
            yield return null;

            // Switch to CageBull form if needed
            if (form != RoombaForm.CageBull)
            {
                PushAction("DEBUG: Switching to CageBull form...");
                
                // Raise walls
                if (ArenaManager != null)
                {
                    ArenaManager.RaiseWalls(true);
                }

                // Switch form
                form = RoombaForm.CageBull;

                // Raise horns (do this inline, not as a separate coroutine)
                if (!hornsRaised && !hornsRaiseInProgress)
                {
                    hornsRaised = true;
                    hornsRaiseInProgress = true;
                    
                    if (animator != null)
                    {
                        animator.SetTrigger(TriggerHornsRaise);
                    }
                    
                    
                    // Wait for animation
                    yield return WaitForSecondsCache.Get(0.8f);
                    
                    hornsRaiseInProgress = false;
                    PushAction("DEBUG: Horns raised");
                }

                // Deactivate alarm
                if (!alarmDestroyed)
                {
                    ctrl.DeactivateAlarm();
                }
            }
            else
            {
                PushAction("DEBUG: Already in CageBull form");
                
                // Make sure walls are raised
                if (ArenaManager != null && !ArenaManager.WallsAreRaised)
                {
                    ArenaManager.RaiseWalls(true);
                }
            }

            // Ensure agent settings are cached (will be skipped if already cached in Awake)
            CacheAgentSettings();
            
            // Restore agent to original settings before starting combo
            RestoreAgentSettings();
            
            // Ensure agent is ready to move
            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.ResetPath(); // Clear any pending path
                Debug.Log($"[Boss] TIMING: Agent ready - speed: {agent.speed}, isStopped: {agent.isStopped}, hasPath: {agent.hasPath}");
            }
            
            PushAction($"DEBUG: Starting combo with base speed {baseAgentSpeed}");
            
            float comboStartTime = Time.time;
            Debug.Log($"[Boss] TIMING: ===== COMBO EXECUTION STARTING at {comboStartTime:F2} =====");
            
            // Now execute the combo
            yield return DebugExecuteSpecificComboInternal(comboIndex);
            
            Debug.Log($"[Boss] TIMING: ===== COMBO EXECUTION COMPLETE - Total time: {Time.time - comboStartTime:F2}s =====");
            
            // Test complete
            isDebugTestRunning = false;
            debugTestCoroutine = null;
            RestoreAgentSettings();
            PushAction("DEBUG: Test complete - AI will remain stopped. Use context menu to restart.");
        }

        /// <summary>
        /// Executes a specific combo by index (for debug testing).
        /// </summary>
        private IEnumerator DebugExecuteSpecificComboInternal(int comboIndex)
        {
            var combo = ArenaManager.GetCombo(comboIndex);
            if (combo == null || !combo.IsValid)
            {
                Debug.LogError($"[BossRoombaBrain] DEBUG: Combo {comboIndex} became invalid!");
                yield break;
            }

            PushAction($"DEBUG: Executing combo '{combo.ComboName}' ({combo.SegmentCount} segments)");

            var segments = ArenaManager.GetComboSegments(combo);
            if (segments == null)
            {
                Debug.LogError("[BossRoombaBrain] DEBUG: Failed to get combo segments!");
                yield break;
            }

            // Execute each segment in the combo
            for (int i = 0; i < segments.Length; i++)
            {
                if (isStunned || !isDebugTestRunning) break;

                var (start, end) = segments[i];
                float distToStart = Vector3.Distance(transform.position, start);
                
                Debug.Log($"[Boss] TIMING: Segment {i+1} START - dist to start: {distToStart:F2}, threshold: {ChargeArrivalThreshold}");
                float segmentStartTime = Time.time;

                // Move to start if not already there (using FAST approach speed)
                if (distToStart > ChargeArrivalThreshold)
                {
                    // Apply fast approach settings
                    ApplyApproachSettings();
                    
                    if (agent != null)
                    {
                        // CRITICAL: Ensure agent is fully ready to move
                        agent.isStopped = false;
                        agent.updateRotation = true;
                        agent.updatePosition = true;
                        
                        // Set destination and verify it worked
                        bool pathSet = agent.SetDestination(start);
                        
                        Debug.Log($"[Boss] TIMING: Moving to start - speed: {agent.speed}, pathSet: {pathSet}, " +
                                  $"isStopped: {agent.isStopped}, enabled: {agent.enabled}, " +
                                  $"isOnNavMesh: {agent.isOnNavMesh}, hasPath: {agent.hasPath}");
                    }
                    
                    // Wait until we reach the start position (with reasonable timeout)
                    float timeout = 10f;
                    float elapsed = 0f;
                    float moveStartTime = Time.time;
                    float lastLogTime = 0f;
                    
                    while (Vector3.Distance(transform.position, start) > ChargeArrivalThreshold && !isStunned && isDebugTestRunning && elapsed < timeout)
                    {
                        // Log agent state every 2 seconds to diagnose why it's not moving
                        if (elapsed - lastLogTime > 2f && agent != null)
                        {
                            float currentDist = Vector3.Distance(transform.position, start);
                            Debug.Log($"[Boss] TIMING: Still moving... elapsed: {elapsed:F1}s, dist: {currentDist:F2}, " +
                                      $"velocity: {agent.velocity.magnitude:F2}, pathStatus: {agent.pathStatus}, " +
                                      $"isStopped: {agent.isStopped}, hasPath: {agent.hasPath}, " +
                                      $"remainingDistance: {agent.remainingDistance:F2}");
                            lastLogTime = elapsed;
                        }
                        
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    
                    Debug.Log($"[Boss] TIMING: Arrived at start in {Time.time - moveStartTime:F2}s (elapsed: {elapsed:F2}s)");
                    
                    if (elapsed >= timeout)
                    {
                        Debug.LogWarning($"[BossRoombaBrain] DEBUG: Timeout reaching start position for segment {i + 1}! " +
                                         $"Agent state - velocity: {agent?.velocity.magnitude ?? 0:F2}, " +
                                         $"pathStatus: {agent?.pathStatus}, hasPath: {agent?.hasPath}");
                    }
                }
                else
                {
                    Debug.Log($"[Boss] TIMING: Already at start position, skipping move");
                }

                if (isStunned || !isDebugTestRunning) break;

                // Brief pause at start position
                if (agent != null)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                
                // Very brief pause before turning (just 1 frame to let physics settle)
                yield return null;

                Debug.Log($"[Boss] TIMING: Starting turn at {Time.time - segmentStartTime:F2}s into segment");
                float turnStartTime = Time.time;

                // Turn to face end
                yield return TurnToFacePosition(end);

                Debug.Log($"[Boss] TIMING: Turn complete in {Time.time - turnStartTime:F2}s");

                if (isStunned || !isDebugTestRunning) break;

                Debug.Log($"[Boss] TIMING: Starting charge at {Time.time - segmentStartTime:F2}s into segment");
                float chargeStartTime = Time.time;

                // Charge to end
                yield return ExecuteChargeDash(end, isTargeted: false);

                Debug.Log($"[Boss] TIMING: Charge complete in {Time.time - chargeStartTime:F2}s, segment total: {Time.time - segmentStartTime:F2}s");

                // Very brief pause between segments (just 1 frame for snappy combos)
                yield return null;
            }

            PushAction($"DEBUG: Combo '{combo.ComboName}' complete!");
        }



        /// <summary>
        /// Ensures the boss is in CageBull form before executing an action.
        /// If already in CageBull form, executes immediately.
        /// Stops all other AI behavior during debug execution.
        /// </summary>
        private IEnumerator EnsureCageBullFormThenExecute(System.Action onReady)
        {
            // Use the new debug test system
            isDebugTestRunning = true;
            
            // Stop the main AI loop
            if (loop != null)
            {
                StopCoroutine(loop);
                loop = null;
            }
            
            // Stop agent
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            
            yield return null;

            if (form == RoombaForm.CageBull)
            {
                // Already in CageBull form - just setup and execute
                PushAction("DEBUG: Already in CageBull form, preparing...");
                
                // Raise walls if not already
                if (ArenaManager != null && !ArenaManager.WallsAreRaised)
                {
                    ArenaManager.RaiseWalls(true);
                }
                
                CacheAgentSettings();
                
                // Ensure agent is ready
                if (agent != null)
                {
                    agent.isStopped = false;
                }
                
                onReady?.Invoke();
                yield break;
            }

            PushAction("DEBUG: Switching to CageBull form...");

            // Raise walls
            if (ArenaManager != null)
            {
                ArenaManager.RaiseWalls(true);
            }

            // Switch to CageBull
            form = RoombaForm.CageBull;

            // Raise horns inline
            if (!hornsRaised && !hornsRaiseInProgress)
            {
                hornsRaised = true;
                hornsRaiseInProgress = true;
                
                if (animator != null)
                {
                    animator.SetTrigger(TriggerHornsRaise);
                }
                
                yield return WaitForSecondsCache.Get(0.8f);
                
                hornsRaiseInProgress = false;
            }

            // Deactivate alarm
            if (!alarmDestroyed)
            {
                ctrl.DeactivateAlarm();
            }

            // Cache agent settings
            CacheAgentSettings();
            
            // Ensure agent is ready
            if (agent != null)
            {
                agent.isStopped = false;
            }

            PushAction("DEBUG: Now in CageBull form, executing action...");

            // Execute the action
            onReady?.Invoke();
        }

        [ContextMenu("Debug: Test Targeted Charge (CageBull)")]
        public void DebugTestTargetedChargeCageBull()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[BossRoombaBrain] Debug: Test Targeted Charge requires Play Mode!");
                return;
            }

            if (player == null)
            {
                CachePlayerReference();
                if (player == null)
                {
                    Debug.LogError("[BossRoombaBrain] DEBUG: Cannot charge - no Player found!");
                    return;
                }
            }

            PushAction("DEBUG: Testing targeted charge (ensuring CageBull form first)");
            StartCoroutine(EnsureCageBullFormThenExecute(() => StartCoroutine(ExecuteTargetedChargeWithOvershoot())));
        }

        #endregion


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
