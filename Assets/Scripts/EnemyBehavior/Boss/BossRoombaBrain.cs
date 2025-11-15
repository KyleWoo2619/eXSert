using System.Collections;
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
    }

    public interface IParrySink
    {
        void OnParry(string attackId, GameObject player);
    }

    [RequireComponent(typeof(BossRoombaController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class BossRoombaBrain : MonoBehaviour, IParrySink
    {
        [Header("Component Help")]
        [SerializeField, TextArea(3, 6)] private string inspectorHelp =
            "BossRoombaBrain: high-level boss behavior and attack sequencing.\n" +
            "Configure forms and attack descriptors. Optional animator triggers per phase (Windup/Active/Recovery).";

        public RoombaForm StartForm = RoombaForm.DuelistSummoner;

        [Header("Attacks")]
        public BossAttackDescriptor BasicSwipe = new BossAttackDescriptor {
            Id = "BasicSwipe", Parryable = false, Cooldown = 1.2f,
            RangeMin = 0.0f, RangeMax = 3.0f,
            Windup = 0.35f, Active = 0.25f, Recovery = 0.5f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Swipe_Windup", AnimatorTriggerOnActive = "Swipe_Active", AnimatorTriggerOnRecovery = "Swipe_Recovery"
        };
        public BossAttackDescriptor ArmSweep = new BossAttackDescriptor {
            Id = "ArmSweep", Parryable = true, Cooldown = 2.0f,
            RangeMin = 0.5f, RangeMax = 3.5f,
            Windup = 0.6f, Active = 0.4f, Recovery = 0.6f,
            ParryWindowStart = 0.45f, ParryWindowEnd = 0.55f,
            AnimatorTriggerOnWindup = "Sweep_Windup", AnimatorTriggerOnActive = "Sweep_Active", AnimatorTriggerOnRecovery = "Sweep_Recovery"
        };
        public BossAttackDescriptor DashLunge = new BossAttackDescriptor {
            Id = "DashLunge", Parryable = true, Cooldown = 3.0f,
            RangeMin = 5.0f, RangeMax = 25.0f,
            Windup = 0.25f, Active = 0.4f, Recovery = 0.35f,
            ParryWindowStart = 0.30f, ParryWindowEnd = 0.45f,
            AnimatorTriggerOnWindup = "Lunge_Windup", AnimatorTriggerOnActive = "Lunge_Active", AnimatorTriggerOnRecovery = "Lunge_Recovery"
        };
        public BossAttackDescriptor KnockOffSpin = new BossAttackDescriptor {
            Id = "KnockOffSpin", Parryable = false, Cooldown = 4.0f,
            RangeMin = 0.0f, RangeMax = 2.0f,
            Windup = 0.3f, Active = 0.6f, Recovery = 0.6f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Spin_Windup", AnimatorTriggerOnActive = "Spin_Active", AnimatorTriggerOnRecovery = "Spin_Recovery"
        };
        public BossAttackDescriptor VacuumSuction = new BossAttackDescriptor {
            Id = "VacuumSuction", Parryable = false, Cooldown = 8.0f,
            RangeMin = 0.0f, RangeMax = 999f,
            Windup = 0.6f, Active = 2.5f, Recovery = 0.8f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Vacuum_Windup", AnimatorTriggerOnActive = "Vacuum_Active", AnimatorTriggerOnRecovery = "Vacuum_Recovery"
        };
        public BossAttackDescriptor StaticCharge = new BossAttackDescriptor {
            Id = "StaticCharge", Parryable = false, Cooldown = 1.5f,
            RangeMin = 0.0f, RangeMax = 999f,
            Windup = 0.5f, Active = 1.2f, Recovery = 0.6f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Charge_Windup", AnimatorTriggerOnActive = "Charge_Active", AnimatorTriggerOnRecovery = "Charge_Recovery"
        };
        public BossAttackDescriptor TargetedCharge = new BossAttackDescriptor {
            Id = "TargetedCharge", Parryable = false, Cooldown = 3.0f,
            RangeMin = 0.0f, RangeMax = 999f,
            Windup = 0.35f, Active = 1.0f, Recovery = 0.8f,
            ParryWindowStart = 0f, ParryWindowEnd = 0f,
            AnimatorTriggerOnWindup = "Target_Windup", AnimatorTriggerOnActive = "Target_Active", AnimatorTriggerOnRecovery = "Target_Recovery"
        };

        [Header("Summons")]
        public float SummonInterval = 10f;
        public int DronesPerSpawn = 4;
        public int CrawlersPerSpawn = 2;

        [Header("Windows")]
        public float ParryStaggerSeconds = 3.0f;
        public float PillarStunSeconds = 2.0f;
        public float DefensesDownMultiplier = 1.2f;

        [Header("Top Zone/Spin")]
        [Tooltip("If true, KnockOffSpin will only execute when the player is detected on top via BossTopZone.")]
        public bool RequirePlayerOnTopForSpin = true;

        private BossRoombaController ctrl;
        private NavMeshAgent agent;
        private Transform player;
        private RoombaForm form;
        private Coroutine loop;
        private bool alarmDestroyed;
        private Animator animator;
        private bool playerOnTop;

        void Awake()
        {
            ctrl = GetComponent<BossRoombaController>();
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            player = GameObject.FindWithTag("Player")?.transform;
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

                // If player is on top, prioritize knock-off spin
                if (RequirePlayerOnTopForSpin && playerOnTop)
                {
                    yield return ExecuteKnockOffSpin();
                    continue;
                }

                summonTimer += Time.deltaTime;
                if (!alarmDestroyed && summonTimer >= SummonInterval)
                {
                    summonTimer = 0f;
                    ctrl.dronesPerSpawn = DronesPerSpawn;
                    ctrl.crawlersPerSpawn = CrawlersPerSpawn;
                    ctrl.ActivateAlarm();
                }

                float dist = Vector3.Distance(transform.position, player.position);

                if (dist <= ArmSweep.RangeMax && dist >= ArmSweep.RangeMin)
                {
                    yield return ExecuteArmSweep();
                }
                else if (dist <= BasicSwipe.RangeMax)
                {
                    yield return ExecuteAttack(BasicSwipe);
                }
                else
                {
                    if (Random.value < 0.25f)
                        yield return ExecuteDashOrLunge();
                    else
                        yield return MoveTowardPlayer(0.35f);
                }

                if (Random.value < 0.15f)
                    yield return ExecuteVacuum();
            }
        }

        private IEnumerator CageBullLoop()
        {
            // If player is on top, spin has priority as a knock-off mechanic
            if (RequirePlayerOnTopForSpin && playerOnTop)
            {
                yield return ExecuteKnockOffSpin();
                yield break;
            }

            for (int i = 0; i < 3; i++)
                yield return ExecuteStaticCharge();

            yield return ExecuteTargetedCharge();
        }

        private IEnumerator ExecuteAttack(BossAttackDescriptor a)
        {
            if (player == null) yield break;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);
        }

        private IEnumerator ExecuteArmSweep()
        {
            if (player == null) yield break;
            var a = ArmSweep;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);
        }

        private IEnumerator ExecuteDashOrLunge()
        {
            var a = DashLunge;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            float baseSpeed = agent.speed;
            agent.speed = baseSpeed * ctrl.dashSpeedMultiplier;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.Active);
            agent.speed = baseSpeed;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);
        }

        private IEnumerator ExecuteVacuum()
        {
            var a = VacuumSuction;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            ctrl.BeginSuction(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);
        }

        private IEnumerator ExecuteStaticCharge()
        {
            var a = StaticCharge;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.Active + a.Recovery);
        }

        private IEnumerator ExecuteTargetedCharge()
        {
            var a = TargetedCharge;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            yield return new WaitForSeconds(a.Active + a.Recovery);
        }

        private IEnumerator ExecuteKnockOffSpin()
        {
            var a = KnockOffSpin;
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnWindup)) animator.SetTrigger(a.AnimatorTriggerOnWindup);
            yield return new WaitForSeconds(a.Windup);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnActive)) animator.SetTrigger(a.AnimatorTriggerOnActive);
            // Here you can apply radial impulse to nearby rigidbodies / player knock-off effect
            yield return new WaitForSeconds(a.Active);
            if (animator != null && !string.IsNullOrEmpty(a.AnimatorTriggerOnRecovery)) animator.SetTrigger(a.AnimatorTriggerOnRecovery);
            yield return new WaitForSeconds(a.Recovery);
        }

        public void SetPlayerOnTop(bool value)
        {
            playerOnTop = value;
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
            StartCoroutine(ParryStagger());
        }

        private IEnumerator ParryStagger()
        {
            var prev = agent.speed;
            agent.isStopped = true;
            yield return new WaitForSeconds(ParryStaggerSeconds);
            agent.isStopped = false;
            agent.speed = prev;
        }

        public void OnAlarmDestroyed() { alarmDestroyed = true; }
    }
}