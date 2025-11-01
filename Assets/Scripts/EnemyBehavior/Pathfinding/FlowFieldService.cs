using System.Collections.Generic;
using UnityEngine;

public sealed class FlowFieldService : IPathPlanner
{
 public bool SupportsDynamicUpdates => true;
 public bool SupportsManyToOne => true;
 public bool SupportsNavMesh => true;

 private readonly Dictionary<int, FlowField> _fields = new Dictionary<int, FlowField>(64);

 public PathTask RequestPath(PathQuery query)
 {
 var task = new PathTask();
 // Try reuse or build a trivial field; for now return start->goal
 task.Corners = new[] { query.Start, query.Goal };
 task.IsCompleted = true;
 task.Succeeded = true;
 return task;
 }

 public void Update(float dt)
 {
 // Optionally rebuild thermostatted fields over time
 }

 private sealed class FlowField { }
}
