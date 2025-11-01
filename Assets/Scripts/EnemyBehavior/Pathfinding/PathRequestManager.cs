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

 public PathTask Enqueue(PathQuery q)
 {
 var task = new PathTask();
 _queue.Enqueue(q);
 return task;
 }

 void Update()
 {
 int budget = maxPlansPerFrame;
 while (budget-- >0 && _queue.Count >0)
 {
 var q = _queue.Dequeue();
 var planner = _selector.Choose(q, _worldState);
 var task = planner.RequestPath(q);
 // For now we just discard; agents will poll planner or use NavMeshAgent directly
 }

 float dt = Time.deltaTime;
 foreach (var p in _allPlanners)
 p.Update(dt);
 }
 }
}
