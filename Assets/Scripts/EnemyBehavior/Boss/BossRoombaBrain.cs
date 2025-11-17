using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace EnemyBehavior.Boss
{
    public enum RoombaForm { DuelistSummoner, CageBull }

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

    public interface IParrySink { void OnParry(string attackId, GameObject player); }

    [RequireComponent(typeof(BossRoombaController))
    ]
    [RequireComponent(typeof(NavMeshAgent))
    ]
    public sealed class BossRoombaBrain : MonoBehaviour, IParrySink
    {
        [Header("Component Help")]
        [SerializeField, TextArea(3, 6)] private string inspectorHelp =
            "BossRoombaBrain: high-level boss behavior and attack sequencing.\n" +
            "Supports arm deploy/retract, per-attack parry triggers, and randomized left/right lunges.";

        public RoombaForm StartForm = RoombaForm.DuelistSummoner;

        [Header("Attacks")]
        public BossAttackDescriptor BasicSwipe = new BossAttackDescriptor {
            Id = "BasicSwipe", Parryable = false, Cooldown = 1.2f,
            RangeMin = 0.0f, RangeMax = 3.0f,
            Windup = 0.35f, Active = 0.25f, Recovery = 0.5f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Swipe_Windup", AnimatorTriggerOnActive = "Swipe_Active", AnimatorTriggerOnRecovery = "Swipe_Recovery",
            AnimatorTriggerOnParry = "Swipe_Parry", RequiresArms = true
        };
        public BossAttackDescriptor ArmSweep = new BossAttackDescriptor {
            Id = "ArmSweep", Parryable = true, Cooldown = 2.0f,
            RangeMin = 0.5f, RangeMax = 3.5f,
            Windup = 0.6f, Active = 0.4f, Recovery = 0.6f,
            ParryWindowStart = 0.45f, ParryWindowEnd = 0.55f,
            AnimatorTriggerOnWindup = "Sweep_Windup", AnimatorTriggerOnActive = "Sweep_Active", AnimatorTriggerOnRecovery = "Sweep_Recovery",
            AnimatorTriggerOnParry = "Sweep_Parry", RequiresArms = true
        };
        public BossAttackDescriptor DashLungeLeft = new BossAttackDescriptor {
            Id = "DashLungeLeft", Parryable = true, Cooldown = 3.0f,
            RangeMin = 6.0f, RangeMax = 25.0f,
            Windup = 0.25f, Active = 0.4f, Recovery = 0.35f,
            ParryWindowStart = 0.30f, ParryWindowEnd = 0.45f,
            AnimatorTriggerOnWindup = "LungeLeft_Windup", AnimatorTriggerOnActive = "LungeLeft_Active", AnimatorTriggerOnRecovery = "LungeLeft_Recovery",
            AnimatorTriggerOnParry = "LungeLeft_Parry", RequiresArms = true
        };
        public BossAttackDescriptor DashLungeRight = new BossAttackDescriptor {
            Id = "DashLungeRight", Parryable = true, Cooldown = 3.0f,
            RangeMin = 6.0f, RangeMax = 25.0f,
            Windup = 0.25f, Active = 0.4f, Recovery = 0.35f,
            ParryWindowStart = 0.30f, ParryWindowEnd = 0.45f,
            AnimatorTriggerOnWindup = "LungeRight_Windup", AnimatorTriggerOnActive = "LungeRight_Active", AnimatorTriggerOnRecovery = "LungeRight_Recovery",
            AnimatorTriggerOnParry = "LungeRight_Parry", RequiresArms = true
        };
        public BossAttackDescriptor DashLungeNoArms = new BossAttackDescriptor {
            Id = "DashLungeNoArms", Parryable = false, Cooldown = 2.5f,
            RangeMin = 6.0f, RangeMax = 25.0f,
            Windup = 0.2f, Active = 0.4f, Recovery = 0.35f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "LungeNoArms_Windup", AnimatorTriggerOnActive = "LungeNoArms_Active", AnimatorTriggerOnRecovery = "LungeNoArms_Recovery",
            AnimatorTriggerOnParry = "", RequiresArms = false
        };
        public BossAttackDescriptor ArmPoke = new BossAttackDescriptor {
            Id = "ArmPoke", Parryable = true, Cooldown = 0.8f,
            RangeMin = 0.0f, RangeMax = 999f,
            Windup = 0.25f, Active = 0.2f, Recovery = 0.25f,
            ParryWindowStart = 0.15f, ParryWindowEnd = 0.20f,
            AnimatorTriggerOnWindup = "Poke_Windup", AnimatorTriggerOnActive = "Poke_Active", AnimatorTriggerOnRecovery = "Poke_Recovery",
            AnimatorTriggerOnParry = "Poke_Parry", RequiresArms = true
        };
        public BossAttackDescriptor KnockOffSpin = new BossAttackDescriptor {
            Id = "KnockOffSpin", Parryable = false, Cooldown = 12.0f,
            RangeMin = 0.0f, RangeMax = 2.0f,
            Windup = 0.3f, Active = 0.6f, Recovery = 0.6f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Spin_Windup", AnimatorTriggerOnActive = "Spin_Active", AnimatorTriggerOnRecovery = "Spin_Recovery",
            AnimatorTriggerOnParry = "Spin_Parry", RequiresArms = true
        };
        public BossAttackDescriptor VacuumSuction = new BossAttackDescriptor {
            Id = "VacuumSuction", Parryable = false, Cooldown = 12.0f,
            RangeMin = 3.0f, RangeMax = 10.0f,
            Windup = 0.6f, Active = 2.5f, Recovery = 0.8f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Vacuum_Windup", AnimatorTriggerOnActive = "Vacuum_Active", AnimatorTriggerOnRecovery = "Vacuum_Recovery",
            AnimatorTriggerOnParry = "Vacuum_Parry", RequiresArms = false
        };
        public BossAttackDescriptor StaticCharge = new BossAttackDescriptor {
            Id = "StaticCharge", Parryable = false, Cooldown = 1.5f,
            RangeMin = 0.0f, RangeMax = 999f,
            Windup = 0.5f, Active = 1.2f, Recovery = 0.6f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Charge_Windup", AnimatorTriggerOnActive = "Charge_Active", AnimatorTriggerOnRecovery = "Charge_Recovery",
            AnimatorTriggerOnParry = "Charge_Parry", RequiresArms = false
        };
        public BossAttackDescriptor TargetedCharge = new BossAttackDescriptor {
            Id = "TargetedCharge", Parryable = false, Cooldown = 3.0f,
            RangeMin = 0.0f, RangeMax = 999f,
            Windup = 0.35f, Active = 1.0f, Recovery = 0.8f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Target_Windup", AnimatorTriggerOnActive = "Target_Active", AnimatorTriggerOnRecovery = "Target_Recovery",
            AnimatorTriggerOnParry = "Target_Parry", RequiresArms = false
        };

        [Header("Arms Deploy/ Retract")]
        public string ArmsDeployTrigger = "Arms_Deploy";
        public string ArmsRetractTrigger = "Arms_Retract";
        public float ArmsDeployDuration = 0.4f;
        public float ArmsRetractDuration = 0.4f;
        public float ArmsAutoRetractCooldown = 3.0f;

        [Header("Summons")]
        public float SummonInterval = 10f;
        [Range(0f,1f)] public float SummonChance = 0.75f;
        public int DronesPerSpawn = 4;
        public int CrawlersPerSpawn = 2;

        [Header("Windows")]
        public float ParryStaggerSeconds = 3.0f;
        public float PillarStunSeconds = 2.0f;
        public float DefensesDownMultiplier = 1.2f;

        [Header("Top Zone/Spin")]
        public bool RequirePlayerOnTopForSpin = true;
        public float SpinAfterLastPokeDelay = 0.75f;
        [Tooltip("If the mounted state flickers off, continue treating as mounted for this many seconds.")]
        public float MountedGraceSeconds = 0.2f;

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
        private bool hasEverMounted; // NEW: track first actual mount to suppress startup grace

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
                hasEverMounted = true; // activate grace only after first true mount
                return true;
            }
            if (!hasEverMounted) return false; // do not apply grace before first mount event
            return (Time.time - lastMountedTime) <= MountedGraceSeconds;
        }

        void Awake()
        {
            ctrl = GetComponent<BossRoombaController>();
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            player = GameObject.FindWithTag("Player")?.transform;
            lastMountedTime = -999f; // ensure no initial grace
            hasEverMounted = false;
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

        private bool IsOffCooldown(BossAttackDescriptor a)
        {
            float t;
            return !nextReadyTime.TryGetValue(a.Id, out t) || Time.time >= t;
        }

        private void MarkCooldown(BossAttackDescriptor a)
        {
            nextReadyTime[a.Id] = Time.time + Mathf.Max(0f, a.Cooldown);
        }

        private BossAttackDescriptor SelectCloseRangeAttack(float dist)
        {
            var options = new List<BossAttackDescriptor>(2);
            if (dist >= BasicSwipe.RangeMin && dist <= BasicSwipe.RangeMax && IsOffCooldown(BasicSwipe)) options.Add(BasicSwipe);
            if (dist >= ArmSweep.RangeMin && dist <= ArmSweep.RangeMax && IsOffCooldown(ArmSweep)) options.Add(ArmSweep);
            if (options.Count == 0) return null;
            if (options.Count == 1) return options[0];
            return Random.value < 0.5f ? options[0] : options[1];
        }

        private void ResetTopSequence()
        {
            topPokeCount = 0;
            requiredPokesForSpin = Random.Range(3, 6);
        }

        private IEnumerator FormLoop()
        {
            while (true)
            {
                switch (form)
                {
                    case RoombaForm.DuelistSummoner:
                        yield return DuelistSummonerLoop();
                        form = RoombaForm.CageBull;
                        break;
                    case RoombaForm.CageBull:
                        yield return CageBullLoop();
                        form = RoombaForm.DuelistSummoner;
                        break;
                }
                yield return null;
            }
        }

        private IEnumerator DuelistSummonerLoop()
        {
            float summonTimer = 0f;
            ctrl.ActivateAlarm();

            while (form == RoombaForm.DuelistSummoner)
            {
                if (player == null) yield break;

                if (RequirePlayerOnTopForSpin && IsMountedWithGrace())
                {
                    yield return ExecuteArmPokeSequenceThenSpin();
                    continue;
                }

                summonTimer += Time.deltaTime;
                if (!alarmDestroyed && summonTimer >= SummonInterval)
                {
                    summonTimer = 0f;
                    ctrl.dronesPerSpawn = DronesPerSpawn;
                    ctrl.crawlersPerSpawn = CrawlersPerSpawn;
                    ctrl.ActivateAlarm();
                    if (Random.value <= SummonChance)
                    {
                        ctrl.TriggerSpawnWave(DronesPerSpawn, CrawlersPerSpawn);
                        PushAction($"Summon wave: drones {DronesPerSpawn}, crawlers {CrawlersPerSpawn}");
                    }
                    else
                    {
                        PushAction("Summon skipped");
                    }
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

                if (dist >= VacuumSuction.RangeMin && dist <= VacuumSuction.RangeMax && IsOffCooldown(VacuumSuction) && Random.value < 0.08f)
                    yield return ExecuteAttackChain(VacuumSuction);
            }
        }

        private IEnumerator CageBullLoop()
        {
            if (RequirePlayerOnTopForSpin && IsMountedWithGrace())
            {
                yield return ExecuteArmPokeSequenceThenSpin();
                yield break;
            }

            for (int i = 0; i < 3; i++)
                yield return ExecuteAttackChain(StaticCharge);

            yield return ExecuteAttackChain(TargetedCharge);
        }

        private IEnumerator ExecuteArmPokeSequenceThenSpin()
        {
            if (topPokeCount == 0)
            {
                // Initialize threshold once when starting a new mounted streak
                requiredPokesForSpin = requiredPokesForSpin <= 0 ? Random.Range(3, 6) : requiredPokesForSpin;
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
                        // Spin on cooldown: keep harassing with pokes but do not increase the threshold requirement
                        if (IsOffCooldown(ArmPoke))
                        {
                            yield return ExecuteAttackChain(ArmPoke);
                            // do not change topPokeCount here
                        }
                        else
                        {
                            yield return null;
                        }
                    }
                }
            }

            // Fully dismounted after grace
            ResetTopSequence();
        }

        private bool IsTopExclusive(BossAttackDescriptor a)
        {
            return a == ArmPoke || a == KnockOffSpin;
        }

        private IEnumerator ExecuteAttackChain(BossAttackDescriptor a)
        {
            if (player == null) yield break;
            if (IsMountedWithGrace() && !IsTopExclusive(a)) yield break;
            currentAttack = a;
            PushAction($"Attack: {a.Id}");

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
            currentAttack = a;
            PushAction($"Attack: {a.Id}");
            if (player == null) yield break;
            if (a.RequiresArms && !armsDeployed)
            {
                DeployArms();
                yield return new WaitForSeconds(ArmsDeployDuration);
            }
            if (armsRetractRoutine != null) { StopCoroutine(armsRetractRoutine); armsRetractRoutine = null; }

            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            float baseSpeed = agent.speed;
            agent.speed = baseSpeed * ctrl.dashSpeedMultiplier;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
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
            yield return new WaitForSeconds(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);
            MarkCooldown(a);
            if (a.RequiresArms)
                ScheduleArmsAutoRetract();
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
                // only stamp dismount time if we had a real mount
                if (hasEverMounted) lastMountedTime = Time.time;
                ctrl.StopTopWander();
                // Do NOT reset sequence here; wait until grace fully expires
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
                StartCoroutine(ParryStagger());
                PushAction($"Parried: {attackId}");
            }
        }

        private IEnumerator ParryStagger()
        {
            var prev = agent.speed;
            agent.isStopped = true;
            yield return new WaitForSeconds(ParryStaggerSeconds);
            agent.isStopped = false;
            agent.speed = prev;
            if (armsDeployed)
                ScheduleArmsAutoRetract();
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