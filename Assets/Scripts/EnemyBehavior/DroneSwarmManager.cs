using System.Collections.Generic;
using UnityEngine;

public class DroneSwarmManager : MonoBehaviour
{
    [Header("Spawning Control")]
    [Tooltip("Disable to prevent this manager from spawning any drone clusters or drones.")]
    [SerializeField] private bool spawningEnabled = true;

    [Header("Drone Swarm Settings")]
    public GameObject dronePrefab;
    public int dronesPerCluster = 4;
    public float spawnRadius = 10f;
    public List<Transform> clusterSpawnPoints;

    private List<DroneCluster> clusters = new List<DroneCluster>();

    private void Start()
    {
        if (!spawningEnabled)
            return;

        SpawnClusters();
    }

    public void SpawnClusters()
    {
        if (!spawningEnabled)
        {
            Debug.Log("[DroneSwarmManager] Spawning is disabled. No clusters will be created.");
            return;
        }

        if (clusterSpawnPoints == null || clusterSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[DroneSwarmManager] No cluster spawn points assigned.");
            return;
        }

        for (int c = 0; c < clusterSpawnPoints.Count; c++)
        {
            var clusterGO = new GameObject($"DroneCluster_{c + 1}");
            var cluster = clusterGO.AddComponent<DroneCluster>();
            clusters.Add(cluster);

            for (int i = 0; i < dronesPerCluster; i++)
            {
                // Randomize only X and Z, keep Y at spawn point
                Vector2 circle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = clusterSpawnPoints[c].position + new Vector3(circle.x, 0, circle.y);

                // Optionally, raise spawnPos.y a bit to avoid ground clipping
                spawnPos.y += 1.0f;

                // Snap to NavMesh
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                }
                else
                {
                    Debug.LogWarning($"[DroneSwarmManager] No NavMesh found near {spawnPos}, drone may not spawn correctly.");
                }

                var droneGO = Instantiate(dronePrefab, spawnPos, Quaternion.identity, clusterGO.transform);
                var drone = droneGO.GetComponent<DroneEnemy>();
                if (drone != null)
                {
                    drone.Cluster = cluster;
                    cluster.drones.Add(drone);
                }
                else
                {
                    Debug.LogWarning("[DroneSwarmManager] Spawned prefab does not contain a DroneEnemy component.");
                }
            }
        }
    }
}