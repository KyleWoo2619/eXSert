using UnityEngine;
using UnityEngine.AI;

// Minimal boss controller to wire movement patterns and attacks
[RequireComponent(typeof(NavMeshAgent))]
public class BossRoombaController : MonoBehaviour
{
 public EnemyBehaviorProfile profile;
 private NavMeshAgent agent;
 private Transform player;

 void Awake()
 {
 agent = GetComponent<NavMeshAgent>();
 }

 void Start()
 {
 ApplyProfile();
 player = GameObject.FindWithTag("Player")?.transform;
 }

 void ApplyProfile()
 {
 if (profile == null) return;
 agent.speed = Random.Range(profile.SpeedRange.x, profile.SpeedRange.y);
 agent.acceleration = profile.Acceleration;
 agent.angularSpeed = profile.AngularSpeed;
 agent.stoppingDistance = profile.StoppingDistance;
 agent.avoidancePriority = profile.AvoidancePriority;
 agent.autoBraking = false;
 }

 void Update()
 {
 if (player == null) return;
 // Simple behavior: approach player, occasionally do a dash
 agent.SetDestination(player.position);
 }
}
