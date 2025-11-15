using System.Collections.Generic;
using UnityEngine;

namespace EnemyBehavior.Boss
{
    public sealed class BossArenaManager : MonoBehaviour
    {
        public List<GameObject> Walls;
        public List<GameObject> Pillars;
        public List<Transform> LaneStarts;
        public List<Transform> LaneEnds;

        public void RaiseWalls(bool up)
        {
            for (int i = 0; i < Walls.Count; i++) Walls[i].SetActive(up);
        }

        public (Vector3 start, Vector3 end) GetLane(int idx)
        {
            int i = Mathf.Clamp(idx, 0, Mathf.Min(LaneStarts.Count, LaneEnds.Count) - 1);
            return (LaneStarts[i].position, LaneEnds[i].position);
        }

        public void OnPillarCollision(int pillarIndex)
        {
            if (pillarIndex >= 0 && pillarIndex < Pillars.Count)
                Pillars[pillarIndex].SetActive(false);
        }
    }
}