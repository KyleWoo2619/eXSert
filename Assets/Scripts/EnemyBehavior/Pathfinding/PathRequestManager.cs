// PathRequestManager.cs
// Purpose: Central manager for path requests. Selects an appropriate planner and returns PathTask results synchronously.
// Works with: IPathPlanner implementations, CrowdAgent path requests and BaseEnemy.

using System.Collections.Generic;
using UnityEngine;

namespace EnemyBehavior.Pathfinding
{
 public sealed class PathRequestManager : MonoBehaviour
 {
 public static PathRequestManager Instance { get; private set; }

 [SerializeField] int maxPlansPerFrame =8;
 [SerializeField] int maxReplansPerFrame =4;

 private readonly Queue<PathQuery> _queue = new Queue<PathQuery>(256);
 private readonly List<IPathPlanner> _allPlanners = new List<IPathPlanner>(8);
 private IPlannerSelector _selector;
 private WorldState _worldState = new WorldState();

 void Awake()
 {
 Instance = this;
 // Register planners (simple defaults)
 var astar = new NavMeshAStarPlanner();
 var dijkstra = new DijkstraPlanner();
 var flow = new FlowFieldService();
 _allPlanners.Add(astar);
 _allPlanners.Add(dijkstra);
 _allPlanners.Add(flow);
 _selector = new PlannerSelector(_allPlanners);
 }

 // Enqueue now performs an immediate synchronous path request and returns the PathTask.
 public PathTask Enqueue(PathQuery q)
 {
 if (_selector == null)
 {
 // ensure selector exists
 _selector = new PlannerSelector(_allPlanners);
 }
 var planner = _selector.Choose(q, _worldState);
 if (planner == null)
 {
 // fallback: direct NavMesh.CalculatePath
 var task = new PathTask();
 var path = new UnityEngine.AI.NavMeshPath();
 int mask = q.AreaMask == -1 ? UnityEngine.AI.NavMesh.AllAreas : q.AreaMask;
 UnityEngine.AI.NavMesh.CalculatePath(q.Start, q.Goal, mask, path);
 task.Corners = path.corners;
 task.IsCompleted = true;
 task.Succeeded = path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
 return task;
 }
 var res = planner.RequestPath(q);
 return res;
 }

 void Update()
 {
 // keep planners updated for incremental planners
 float dt = Time.deltaTime;
 foreach (var p in _allPlanners)
 p.Update(dt);
 }
 }
}
