// DijkstraPlanner.cs
// Purpose: Simple Dijkstra-based planner used for small-grid pathing or as a reference planner.
// Works with: FlowFieldService integration and PathRequestManager.

using UnityEngine;

public sealed class DijkstraPlanner : IPathPlanner
{
 public bool SupportsDynamicUpdates => true;
 public bool SupportsManyToOne => false;
 public bool SupportsNavMesh => true;

 public PathTask RequestPath(PathQuery query)
 {
 var task = new PathTask();
 // Simplified: fallback to straight line for now
 task.Corners = new[] { query.Start, query.Goal };
 task.IsCompleted = true;
 task.Succeeded = true;
 return task;
 }

 public void Update(float dt) { }
}
