using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace EnemyBehavior.Boss
{
    public sealed class BossArenaManager : MonoBehaviour
    {
        public List<GameObject> Walls;
        public List<GameObject> Pillars;
        public List<Transform> LaneStarts;
        public List<Transform> LaneEnds;

        [Header("Cage Bounds")]
        [Tooltip("Collider defining the inside of the cage area")]
        public Collider CageBounds;

        private List<NavMeshAgent> disabledAgentsOutsideCage = new List<NavMeshAgent>();

        public void RaiseWalls(bool up)
        {
            for (int i = 0; i < Walls.Count; i++)
            {
                if (Walls[i] != null)
                    Walls[i].SetActive(up);
            }

            if (up)
            {
                DisableAgentsOutsideCage();
            }
            else
            {
                ReenableDisabledAgents();
            }
        }

        private void DisableAgentsOutsideCage()
        {
            if (CageBounds == null) return;

            disabledAgentsOutsideCage.Clear();

            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies)
            {
                var agent = enemy.GetComponent<NavMeshAgent>();
                if (agent != null && agent.enabled)
                {
                    if (!CageBounds.bounds.Contains(enemy.transform.position))
                    {
                        agent.enabled = false;
                        disabledAgentsOutsideCage.Add(agent);
                        Debug.Log($"Disabled agent outside cage: {enemy.name}");
                    }
                }
            }
        }

        private void ReenableDisabledAgents()
        {
            foreach (var agent in disabledAgentsOutsideCage)
            {
                if (agent != null)
                {
                    agent.enabled = true;
                    Debug.Log($"Re-enabled agent: {agent.name}");
                }
            }
            disabledAgentsOutsideCage.Clear();
        }

        public (Vector3 start, Vector3 end) GetLane(int idx)
        {
            int i = Mathf.Clamp(idx, 0, Mathf.Min(LaneStarts.Count, LaneEnds.Count) - 1);
            return (LaneStarts[i].position, LaneEnds[i].position);
        }

        public void OnPillarCollision(int pillarIndex)
        {
            if (pillarIndex >= 0 && pillarIndex < Pillars.Count)
            {
                if (Pillars[pillarIndex] != null)
                {
                    Pillars[pillarIndex].SetActive(false);
                    Debug.Log($"Pillar {pillarIndex} disabled");
                }
            }
        }
    }
}