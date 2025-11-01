// PlannerSelector.cs
// Purpose: Chooses the best planner for a given PathQuery based on capabilities and world state.
// Works with: PathRequestManager and IPathPlanner implementations.

using System.Collections.Generic;
using UnityEngine;

public sealed class PlannerSelector : IPlannerSelector
{
 private readonly IPathPlanner _astar;
 private readonly IPathPlanner _dijkstra;
 private readonly IPathPlanner _flow;

 public PlannerSelector(IEnumerable<IPathPlanner> planners)
 {
 foreach (var p in planners)
 {
 if (p is FlowFieldService) _flow = p;
 else if (p is DijkstraPlanner) _dijkstra = p;
 else if (p is NavMeshAStarPlanner) _astar = p;
 }
 }

 public IPathPlanner Choose(PathQuery query, WorldState state)
 {
 if ((query.Hints & PlannerHints.ManyAgentsToSameGoal) !=0 && _flow != null) return _flow;
 if ((query.Hints & PlannerHints.HighDynamics) !=0 && _dijkstra != null) return _dijkstra;
 if (state.MapIsVeryLarge && _astar != null) return _astar;
 if ((query.Hints & PlannerHints.NoGoodHeuristic) !=0 && _dijkstra != null) return _dijkstra;
 return _astar ?? _dijkstra ?? _flow;
 }
}
