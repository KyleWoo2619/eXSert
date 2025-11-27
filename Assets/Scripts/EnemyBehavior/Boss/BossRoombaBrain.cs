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
        [Tooltip("If true, the attack can be parried during its window.")]
        public bool Parryable;
        [Tooltip("Minimum seconds between uses of this attack.")]
        public float Cooldown;
        [Tooltip("Min distance to the player at which this attack is eligible.")]
        public float RangeMin;
        [Tooltip("Max distance to the player at which this attack is eligible.")]
        public float RangeMax;
        [Tooltip("Seconds from start until the hit becomes active (wind-up/telegraph).")]
        public float Windup;
        [Tooltip("Seconds when the hit is active (damage window).")]
        public float Active;
        [Tooltip("Seconds after active ends before boss can chain next action.")]
        public float Recovery;
        [Tooltip("Seconds from windup start when the parry window opens (relative time).")]
        public float ParryWindowStart;
        [Tooltip("Seconds from windup start when the parry window closes (relative time).")]
        public float ParryWindowEnd;
        [Header("Animation Hooks")]
        [Tooltip("Fired when an attack starts winding up (telegraph). Use it to start anticipation animations, glow, or pose.")]
        public string AnimatorTriggerOnWindup;
        [Tooltip("Fired at the start of the damage-active frames. Use it to play the hit/dash/sweep animation.")]
        public string AnimatorTriggerOnActive;
        [Tooltip("Fired when the damage window ends. Use it for easing out or returning to idle/move loops.")]
        public string AnimatorTriggerOnRecovery;
        [Tooltip("Trigger fired when this attack is parried (transition from Active to Parry state). Optional.")]
        public string AnimatorTriggerOnParry;
        [Tooltip("Set true if this attack requires arms to be deployed before windup.")]
        public bool RequiresArms;
    }

    [System.Serializable]
    public sealed class SidePanel
    {
        public GameObject panelObject;
        public float maxHealth = 100f;
        public float currentHealth;
        public bool isDestroyed;
        [Tooltip("Damage multiplier when this panel is destroyed")]
        public float vulnerabilityMultiplier = 1.5f;
    }

    public interface IParrySink { void OnParry(string attackId, GameObject player); }

    [RequireComponent(typeof(BossRoombaController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class BossRoombaBrain : MonoBehaviour, IParrySink
    {
        [Header("Component Help")]
        [SerializeField, TextArea(3, 6)] private string inspectorHelp =
            "BossRoombaBrain: high-level boss behavior and attack sequencing.\n" +
            "Supports arm deploy/retract, per-attack parry triggers, and randomized left/right lunges.";

        public RoombaForm StartForm = RoombaForm.DuelistSummoner;

        [Header("Attacks")]
        public BossAttackDescriptor BasicSwipe;
        public BossAttackDescriptor BasicSwipeLeft;
        public BossAttackDescriptor BasicSwipeRight;
        public BossAttackDescriptor ArmSweep;
        public BossAttackDescriptor ArmSweepLeft;
        public BossAttackDescriptor ArmSweepRight;
        public BossAttackDescriptor DashLungeLeft;
        public BossAttackDescriptor DashLungeRight;
        public BossAttackDescriptor DashLungeNoArms;
        public BossAttackDescriptor ArmPoke;
        public BossAttackDescriptor KnockOffSpin;
        public BossAttackDescriptor VacuumSuction;
        public BossAttackDescriptor StaticCharge;
        public BossAttackDescriptor TargetedCharge;

        [Header("Arms Deploy/ Retract")]
        public string ArmsDeployTrigger = "Arms_Deploy";
        public string ArmsRetractTrigger = "Arms_Retract";
        public float ArmsDeployDuration = 0.4f;
        public float ArmsRetractDuration = 0.4f;
        public float ArmsAutoRetractCooldown = 3.0f;

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

        private BossRoombaController ctrl;
        private NavMeshAgent agent;
        private Transform player;
        private RoombaForm form;
        private Coroutine loop;
        private bool alarmDestroyed;
        private Animator animator;
        private bool playerOnTop;

        private bool armsDeployed;
        private Coroutine armsRetractRoutine;

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
            animator = GetComponentInChildren<Animator>();
            player = GameObject.FindWithTag("Player")?.transform;
            lastMountedTime = -999f;
            hasEverMounted = false;

            attackThresholdForVacuum = Random.Range(AttackCountForVacuumRange.x, AttackCountForVacuumRange.y + 1);

            foreach (var panel in SidePanels)
            {
                panel.currentHealth = panel.maxHealth;
                panel.isDestroyed = false;
            }

            InitializeAttackDescriptors();
        }

        private void InitializeAttackDescriptors()
        {
            BasicSwipe = new BossAttackDescriptor {
                Id = "BasicSwipe", Parryable = false, Cooldown = 1.2f,
                RangeMin = 0.0f, RangeMax = 3.0f,
                Windup = 0.35f, Active = 0.25f, Recovery = 0.5f,
                AnimatorTriggerOnWindup = "Swipe_Windup", AnimatorTriggerOnActive = "Swipe_Active", 
                AnimatorTriggerOnRecovery = "Swipe_Recovery", RequiresArms = true
            };

            BasicSwipeLeft = new BossAttackDescriptor {
                Id = "BasicSwipeLeft", Parryable = false, Cooldown = 1.2f,
                RangeMin = 0.0f, RangeMax = 3.0f,
                Windup = 0.35f, Active = 0.25f, Recovery = 0.5f,
                AnimatorTriggerOnWindup = "SwipeLeft_Windup", AnimatorTriggerOnActive = "SwipeLeft_Active",
                AnimatorTriggerOnRecovery = "SwipeLeft_Recovery", RequiresArms = true
            };

            BasicSwipeRight = new BossAttackDescriptor {
                Id = "BasicSwipeRight", Parryable = false, Cooldown = 1.2f,
                RangeMin = 0.0f, RangeMax = 3.0f,
                Windup = 0.35f, Active = 0.25f, Recovery = 0.5f,
                AnimatorTriggerOnWindup = "SwipeRight_Windup", AnimatorTriggerOnActive = "SwipeRight_Active",
                AnimatorTriggerOnRecovery = "SwipeRight_Recovery", RequiresArms = true
            };

            ArmSweep = new BossAttackDescriptor {
                Id = "ArmSweep", Parryable = true, Cooldown = 2.0f,
                RangeMin = 0.5f, RangeMax = 3.5f,
                Windup = 0.6f, Active = 0.4f, Recovery = 0.6f,
                ParryWindowStart = 0.45f, ParryWindowEnd = 0.55f,
                AnimatorTriggerOnWindup = "Sweep_Windup", AnimatorTriggerOnActive = "Sweep_Active",
                AnimatorTriggerOnRecovery = "Sweep_Recovery", AnimatorTriggerOnParry = "Sweep_Parry", RequiresArms = true
            };

            ArmSweepLeft = new BossAttackDescriptor {
                Id = "ArmSweepLeft", Parryable = true, Cooldown = 2.0f,
                RangeMin = 0.5f, RangeMax = 3.5f,
                Windup = 0.6f, Active = 0.4f, Recovery = 0.6f,
                ParryWindowStart = 0.45f, ParryWindowEnd = 0.55f,
                AnimatorTriggerOnWindup = "SweepLeft_Windup", AnimatorTriggerOnActive = "SweepLeft_Active",
                AnimatorTriggerOnRecovery = "SweepLeft_Recovery", AnimatorTriggerOnParry = "SweepLeft_Parry", RequiresArms = true
            };

            ArmSweepRight = new BossAttackDescriptor {
                Id = "ArmSweepRight", Parryable = true, Cooldown = 2.0f,
                RangeMin = 0.5f, RangeMax = 3.5f,
                Windup = 0.6f, Active = 0.4f, Recovery = 0.6f,
                ParryWindowStart = 0.45f, ParryWindowEnd = 0.55f,
                AnimatorTriggerOnWindup = "SweepRight_Windup", AnimatorTriggerOnActive = "SweepRight_Active",
                AnimatorTriggerOnRecovery = "SweepRight_Recovery", AnimatorTriggerOnParry = "SweepRight_Parry", RequiresArms = true
            };

            DashLungeLeft = new BossAttackDescriptor {
                Id = "DashLungeLeft", Parryable = true, Cooldown = 3.0f,
                RangeMin = 6.0f, RangeMax = 25.0f,
                Windup = 0.25f, Active = 0.4f, Recovery = 0.35f,
                ParryWindowStart = 0.30f, ParryWindowEnd = 0.45f,
                AnimatorTriggerOnWindup = "LungeLeft_Windup", AnimatorTriggerOnActive = "LungeLeft_Active",
                AnimatorTriggerOnRecovery = "LungeLeft_Recovery", AnimatorTriggerOnParry = "LungeLeft_Parry", RequiresArms = true
            };

            DashLungeRight = new BossAttackDescriptor {
                Id = "DashLungeRight", Parryable = true, Cooldown = 3.0f,
                RangeMin = 6.0f, RangeMax = 25.0f,
                Windup = 0.25f, Active = 0.4f, Recovery = 0.35f,
                ParryWindowStart = 0.30f, ParryWindowEnd = 0.45f,
                AnimatorTriggerOnWindup = "LungeRight_Windup", AnimatorTriggerOnActive = "LungeRight_Active",
                AnimatorTriggerOnRecovery = "LungeRight_Recovery", AnimatorTriggerOnParry = "LungeRight_Parry", RequiresArms = true
            };

            DashLungeNoArms = new BossAttackDescriptor {
                Id = "DashLungeNoArms", Parryable = false, Cooldown = 2.5f,
                RangeMin = 6.0f, RangeMax = 25.0f,
                Windup = 0.2f, Active = 0.4f, Recovery = 0.35f,
                AnimatorTriggerOnWindup = "LungeNoArms_Windup", AnimatorTriggerOnActive = "LungeNoArms_Active",
                AnimatorTriggerOnRecovery = "LungeNoArms_Recovery", RequiresArms = false
            };

            ArmPoke = new BossAttackDescriptor {
                Id = "ArmPoke", Parryable = true, Cooldown = 0.8f,
                RangeMin = 0.0f, RangeMax = 999f,
                Windup = 0.25f, Active = 0.2f, Recovery = 0.25f,
                ParryWindowStart = 0.15f, ParryWindowEnd = 0.20f,
                AnimatorTriggerOnWindup = "Poke_Windup", AnimatorTriggerOnActive = "Poke_Active",
                AnimatorTriggerOnRecovery = "Poke_Recovery", AnimatorTriggerOnParry = "Poke_Parry", RequiresArms = true
            };

            KnockOffSpin = new BossAttackDescriptor {
                Id = "KnockOffSpin", Parryable = false, Cooldown = 12.0f,
                RangeMin = 0.0f, RangeMax = 2.0f,
                Windup = 0.3f, Active = 0.6f, Recovery = 0.6f,
                AnimatorTriggerOnWindup = "Spin_Windup", AnimatorTriggerOnActive = "Spin_Active",
                AnimatorTriggerOnRecovery = "Spin_Recovery", RequiresArms = true
            };

            VacuumSuction = new BossAttackDescriptor {
                Id = "VacuumSuction", Parryable = false, Cooldown = 12.0f,
                RangeMin = 3.0f, RangeMax = 10.0f,
                Windup = 0.6f, Active = 2.5f, Recovery = 0.8f,
                AnimatorTriggerOnWindup = "Vacuum_Windup", AnimatorTriggerOnActive = "Vacuum_Active",
                AnimatorTriggerOnRecovery = "Vacuum_Recovery", RequiresArms = false
            };

            StaticCharge = new BossAttackDescriptor {
                Id = "StaticCharge", Parryable = false, Cooldown = 1.5f,
                RangeMin = 0.0f, RangeMax = 999f,
                Windup = 0.5f, Active = 1.2f, Recovery = 0.6f,
                AnimatorTriggerOnWindup = "Charge_Windup", AnimatorTriggerOnActive = "Charge_Active",
                AnimatorTriggerOnRecovery = "Charge_Recovery", RequiresArms = false
            };

            TargetedCharge = new BossAttackDescriptor {
                Id = "TargetedCharge", Parryable = false, Cooldown = 3.0f,
                RangeMin = 0.0f, RangeMax = 999f,
                Windup = 0.35f, Active = 1.0f, Recovery = 0.8f,
                AnimatorTriggerOnWindup = "Target_Windup", AnimatorTriggerOnActive = "Target_Active",
                AnimatorTriggerOnRecovery = "Target_Recovery", RequiresArms = false
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
            
            // Only select from Left/Right variants, not the base versions
            if (dist >= BasicSwipe.RangeMin && dist <= BasicSwipe.RangeMax)
            {
                if (IsOffCooldown(BasicSwipeLeft)) options.Add(BasicSwipeLeft);
                if (IsOffCooldown(BasicSwipeRight)) options.Add(BasicSwipeRight);
            }
            
            if (dist >= ArmSweep.RangeMin && dist <= ArmSweep.RangeMax)
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

                if (attackCounter >= attackThresholdForVacuum)
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

                if (dist >= VacuumSuction.RangeMin && dist <= VacuumSuction.RangeMax && 
                    IsOffCooldown(VacuumSuction) && Random.value < 0.08f)
                    yield return ExecuteAttackChain(VacuumSuction);
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

            yield return ExecuteAttackChain(VacuumSuction);

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
                yield return new WaitForSeconds(ChargeRestDuration);
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
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator ExecuteTargetedCharge()
        {
            if (player == null) yield break;

            PushAction("Targeted charge START");
            currentAttack = TargetedCharge;

            if (animator != null && !string.IsNullOrEmpty(TargetedCharge.AnimatorTriggerOnWindup)) 
                animator.SetTrigger(TargetedCharge.AnimatorTriggerOnWindup);
            
            yield return new WaitForSeconds(TargetedCharge.Windup);

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            Vector3 overshootTarget = player.position + dirToPlayer * ChargeOvershootDistance;

            float baseSpeed = agent.speed;
            agent.speed = baseSpeed * ChargeSpeedMultiplier;

            if (animator != null && !string.IsNullOrEmpty(TargetedCharge.AnimatorTriggerOnActive))
                animator.SetTrigger(TargetedCharge.AnimatorTriggerOnActive);

            agent.SetDestination(overshootTarget);

            float chargeTime = 0f;
            while (chargeTime < TargetedCharge.Active && !isStunned)
            {
                chargeTime += Time.deltaTime;
                yield return null;
            }

            agent.speed = baseSpeed;

            if (animator != null && !string.IsNullOrEmpty(TargetedCharge.AnimatorTriggerOnRecovery))
                animator.SetTrigger(TargetedCharge.AnimatorTriggerOnRecovery);

            yield return new WaitForSeconds(TargetedCharge.Recovery);
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
                        yield return new WaitForSeconds(SpinAfterLastPokeDelay);
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

            if (a.RequiresArms && !armsDeployed)
            {
                DeployArms();
                yield return new WaitForSeconds(ArmsDeployDuration);
            }

            if (armsRetractRoutine != null) { StopCoroutine(armsRetractRoutine); armsRetractRoutine = null; }

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);

            ApplyBossDamageIfPlayerPresent(1.0f);

            MarkCooldown(a);

            if (a.RequiresArms)
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

            if (a.RequiresArms && !armsDeployed)
            {
                DeployArms();
                yield return new WaitForSeconds(ArmsDeployDuration);
            }
            if (armsRetractRoutine != null) { StopCoroutine(armsRetractRoutine); armsRetractRoutine = null; }

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            Vector3 overshootTarget = player.position + dirToPlayer * DashOvershootDistance;

            float baseSpeed = agent.speed;
            agent.speed = DashSpeed;

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);

            agent.SetDestination(overshootTarget);

            yield return new WaitForSeconds(a.Active);
            
            agent.speed = baseSpeed;

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);

            MarkCooldown(a);

            if (a.RequiresArms)
                ScheduleArmsAutoRetract();
        }

        private IEnumerator ExecuteKnockOffSpin()
        {
            currentAttack = KnockOffSpin;
            PushAction("Attack: KnockOffSpin");

            if (KnockOffSpin.RequiresArms && !armsDeployed)
            {
                DeployArms();
                yield return new WaitForSeconds(ArmsDeployDuration);
            }
            if (armsRetractRoutine != null) { StopCoroutine(armsRetractRoutine); armsRetractRoutine = null; }

            var a = KnockOffSpin;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);

            if (player != null && IsPlayerMounted())
            {
                FlingPlayer();
            }

            yield return new WaitForSeconds(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);
            MarkCooldown(a);
            if (a.RequiresArms)
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
            
            agent.isStopped = true;
            PushAction($"Stunned ({stunType}) for {duration}s");

            yield return new WaitForSeconds(duration);

            agent.isStopped = false;
            isStunned = false;
            currentStunType = StunType.None;
            PushAction("Stun ended");

            if (armsDeployed)
                ScheduleArmsAutoRetract();
        }

        public void DamageSidePanel(int panelIndex, float damage)
        {
            if (panelIndex < 0 || panelIndex >= SidePanels.Count) return;

            var panel = SidePanels[panelIndex];
            if (panel.isDestroyed) return;

            panel.currentHealth -= damage;
            
            if (panel.currentHealth <= 0)
            {
                panel.isDestroyed = true;
                PushAction($"Side panel {panelIndex} destroyed!");
                
                if (panel.panelObject != null)
                {
                    panel.panelObject.transform.SetParent(null);
                    var rb = panel.panelObject.GetComponent<Rigidbody>();
                    if (rb == null) rb = panel.panelObject.AddComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.AddForce(Random.onUnitSphere * 300f);
                }
            }
        }

        public float GetDamageMultiplierForPanel(int panelIndex)
        {
            if (panelIndex < 0 || panelIndex >= SidePanels.Count) return 1f;
            return SidePanels[panelIndex].isDestroyed ? SidePanels[panelIndex].vulnerabilityMultiplier : 1f;
        }

        public void DebugTriggerVacuumSequence()
        {
            if (form == RoombaForm.DuelistSummoner)
            {
                attackCounter = attackThresholdForVacuum;
                PushAction("DEBUG: Forced vacuum sequence");
            }
        }

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
                PushAction("DEBUG: Returned to Duelist form");
            }
        }

        public void DebugApplyParryStun()
        {
            StartCoroutine(ApplyStun(StunType.Parry, ParryStaggerSeconds));
        }

        public void DebugApplyPillarStun()
        {
            StartCoroutine(ApplyStun(StunType.PillarCollision, PillarStunSeconds));
        }

        private void DeployArms()
        {
            armsDeployed = true;
            if (animator != null && !string.IsNullOrEmpty(ArmsDeployTrigger)) animator.SetTrigger(ArmsDeployTrigger);
        }

        private void ScheduleArmsAutoRetract()
        {
            if (armsRetractRoutine != null) StopCoroutine(armsRetractRoutine);
            armsRetractRoutine = StartCoroutine(ArmsRetractAfterCooldown());
        }

        private IEnumerator ArmsRetractAfterCooldown()
        {
            float t = 0f;
            while (t < ArmsAutoRetractCooldown)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (armsDeployed)
            {
                armsDeployed = false;
                if (animator != null && !string.IsNullOrEmpty(ArmsRetractTrigger)) animator.SetTrigger(ArmsRetractTrigger);
            }
            armsRetractRoutine = null;
        }

        public void SetPlayerOnTop(bool value)
        {
            playerOnTop = value;
            if (value)
            {
                lastMountedTime = Time.time;
                hasEverMounted = true;
                ctrl.StartTopWander();
            }
            else
            {
                if (hasEverMounted) lastMountedTime = Time.time;
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
                if (animator != null && !string.IsNullOrEmpty(currentAttack.AnimatorTriggerOnParry))
                {
                    animator.SetTrigger(currentAttack.AnimatorTriggerOnParry);
                }
                if (currentAttackRoutine != null) { StopCoroutine(currentAttackRoutine); currentAttackRoutine = null; }
                StartCoroutine(ApplyStun(StunType.Parry, ParryStaggerSeconds));
                PushAction($"Parried: {attackId}");
            }
        }

        public void OnAlarmDestroyed() { alarmDestroyed = true; }

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
    }
}
