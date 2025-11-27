// IPathPlanner.cs
// Purpose: Interface describing path planners used by PathRequestManager. Implementations include NavMeshAStarPlanner, FlowFieldService, DijkstraPlanner.
// Works with: PathRequestManager, PathQuery/PathTask types.

using System.Collections.Generic;
using UnityEngine;

// Planner hints
[System.Flags]
public enum PlannerHints
{
 None =0,
 ManyAgentsToSameGoal =1 <<0,
 HighDynamics =1 <<1,
 NoGoodHeuristic =1 <<2,
 PreferStraight =1 <<3,
 AvoidCrowds =1 <<4,
 BossCharge =1 <<5,
}

// Simple result container
public sealed class PathTask
{
 public bool IsCompleted;
 public bool Succeeded;
 public Vector3[] Corners;
 public object PlannerData;
}

// Query describing a path request
public sealed class PathQuery
{
 public Vector3 Start;
 public Vector3 Goal;
 public int AreaMask = -1; // NavMesh.AllAreas is -1
 public float AgentRadius =0.5f;
 public PlannerHints Hints = PlannerHints.None;
 public int GroupId;
}

public interface IPathPlanner
{
 bool SupportsDynamicUpdates { get; }
 bool SupportsManyToOne { get; }
 bool SupportsNavMesh { get; }

 PathTask RequestPath(PathQuery query);
 void Update(float dt);
}

public class WorldState
{
 public bool MapIsVeryLarge;
 public bool FrequentTopologyChanges;
 public float DensitySpikeLevel;
}

public interface IPlannerSelector
{
 IPathPlanner Choose(PathQuery query, WorldState state);
}
