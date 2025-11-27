using UnityEngine;
using UnityEngine.AI;

// Simple component to make test agents wander randomly on the NavMesh
[RequireComponent(typeof(NavMeshAgent))]
public class AutoAgentWander : MonoBehaviour
{
 NavMeshAgent _agent;
 float _timer;
 void Awake()
 {
 _agent = GetComponent<NavMeshAgent>();
 _timer = Random.Range(0f,1f);
 }

 void Update()
 {
 _timer -= Time.deltaTime;
 if (_timer <=0f)
 {
 _timer = Random.Range(1f,3f);
 Vector3 randomDir = Random.insideUnitSphere *10f;
 randomDir += transform.position;
 if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit,5f, NavMesh.AllAreas))
 {
 _agent.SetDestination(hit.position);
 }
 }
 }
}
