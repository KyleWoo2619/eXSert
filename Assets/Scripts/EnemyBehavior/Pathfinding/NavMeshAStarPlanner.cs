using UnityEngine;
using UnityEngine.AI;

public sealed class NavMeshAStarPlanner : IPathPlanner
{
 public bool SupportsDynamicUpdates => false;
 public bool SupportsManyToOne => false;
 public bool SupportsNavMesh => true;

 public NavMeshAStarPlanner()
 {
 // Intentionally light: complex graph building deferred
 }

 public PathTask RequestPath(PathQuery query)
 {
 var task = new PathTask();
 var path = new NavMeshPath();
 int mask = query.AreaMask == -1 ? NavMesh.AllAreas : query.AreaMask;
 NavMesh.CalculatePath(query.Start, query.Goal, mask, path);
 task.Corners = path.corners;
 task.IsCompleted = true;
 task.Succeeded = path.status == NavMeshPathStatus.PathComplete;
 return task;
 }

 public void Update(float dt) { }
}
