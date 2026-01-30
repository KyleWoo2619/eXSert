// FlowFieldService.cs
// Purpose: Builds and caches flow fields (reverse Dijkstra) for many-to-one movement. Provides sampling for agents to follow inexpensive guidance vectors.
// Works with: PathRequestManager, CrowdController, DensityGrid for cost integration.
// Notes: Fields cached by goal key; supports invalidation hooks.

using System.Collections.Generic;
using UnityEngine;

public sealed class FlowFieldService : IPathPlanner
{
 public bool SupportsDynamicUpdates => true;
 public bool SupportsManyToOne => true;
 public bool SupportsNavMesh => true;

 // Density multiplier applied to sampled density values to influence cost (0 = disabled)
 public float DensityCostMultiplier { get; set; } =0f;

 // Maximum cached flow fields to prevent unbounded memory growth
 private const int MaxCachedFields = 32;

 private readonly Dictionary<int, FlowField> _fields = new Dictionary<int, FlowField>(64);
 private readonly int gridW =128;
 private readonly int gridH =128;
 private readonly float cellSize =1.0f;
 private readonly Vector2 gridOrigin = new Vector2(-64f, -64f);

 public PathTask RequestPath(PathQuery query)
 {
 var task = new PathTask();
 var key = KeyFrom(query.Goal);
 if (!_fields.TryGetValue(key, out var field))
 {
 // Limit cache size to prevent unbounded memory growth
 if (_fields.Count >= MaxCachedFields)
 {
  _fields.Clear(); // Simple eviction - clear all when full
 }
 
 field = BuildField(query.Goal);
 _fields[key] = field;
 }
 // sample short guide path by following directions until close to goal
 var pts = new List<Vector3>();
 Vector3 cur = query.Start;
 pts.Add(cur);
 for (int i =0; i <512; i++)
 {
 var dir = field.SampleDirection(cur);
 if (dir == Vector3.zero) break;
 cur += dir * cellSize *0.9f; // step
 pts.Add(cur);
 if ((cur - query.Goal).sqrMagnitude < (cellSize * cellSize)) break;
 }
 pts.Add(query.Goal);
 task.Corners = pts.ToArray();
 task.IsCompleted = true;
 task.Succeeded = true;
 return task;
 }

 /// <summary>
 /// Clears the flow field cache. Call when changing scenes or when memory pressure is high.
 /// </summary>
 public void ClearCache()
 {
 _fields.Clear();
 }

 public void Update(float dt) { }

 private static int KeyFrom(Vector3 goal)
 {
 int qx = Mathf.RoundToInt(goal.x);
 int qz = Mathf.RoundToInt(goal.z);
 return qx *73856093 ^ qz *19349663;
 }

 private FlowField BuildField(Vector3 goal)
 {
 var f = new FlowField(gridW, gridH, cellSize, gridOrigin, goal, DensityCostMultiplier);
 f.Build();
 return f;
 }

 private sealed class FlowField
 {
 public readonly int w, h;
 public readonly float cs;
 public readonly Vector2 origin;
 public readonly float[] cost; // potential
 public readonly Vector2[] dir; // best direction
 public readonly Vector3 goalWorld;
 private readonly float densityMultiplier;

 public FlowField(int w, int h, float cs, Vector2 origin, Vector3 goal, float densityMultiplier)
 {
 this.w = w; this.h = h; this.cs = cs; this.origin = origin; this.goalWorld = goal;
 this.densityMultiplier = densityMultiplier;
 cost = new float[w * h];
 dir = new Vector2[w * h];
 for (int i =0; i < cost.Length; i++) cost[i] = float.PositiveInfinity;
 }

 public void Build()
 {
 int gx = WorldToX(goalWorld.x);
 int gz = WorldToZ(goalWorld.z);
 var open = new Queue<int>();
 int gi = gx + gz * w;
 if (gx <0 || gz <0 || gx >= w || gz >= h) return;
 cost[gi] =0f;
 open.Enqueue(gi);
 int[] dx = new[] { -1,1,0,0 };
 int[] dz = new[] {0,0, -1,1 };
 while (open.Count >0)
 {
 int idx = open.Dequeue();
 int cx = idx % w; int cz = idx / w;
 for (int k =0; k <4; k++)
 {
 int nx = cx + dx[k]; int nz = cz + dz[k];
 if (nx <0 || nz <0 || nx >= w || nz >= h) continue;
 int ni = nx + nz * w;
 // base movement cost
 float stepCost =1f;
 // sample density at neighbor cell center
 float density =0f;
 if (densityMultiplier >0f && EnemyBehavior.Density.DensityGrid.Instance != null)
 {
 float wx = origin.x + (nx +0.5f) * cs;
 float wz = origin.y + (nz +0.5f) * cs;
 density = EnemyBehavior.Density.DensityGrid.Instance.SampleCost(new Vector3(wx,0f, wz));
 }
 float ncost = cost[idx] + stepCost + densityMultiplier * density;
 if (ncost < cost[ni])
 {
 cost[ni] = ncost;
 open.Enqueue(ni);
 }
 }
 }

 // compute simple gradient
 for (int z =0; z < h; z++)
 for (int x =0; x < w; x++)
 {
 int i = x + z * w;
 if (cost[i] == float.PositiveInfinity) { dir[i] = Vector2.zero; continue; }
 // find lowest neighbor
 float best = cost[i]; int bx = x, bz = z;
 for (int k =0; k <4; k++)
 {
 int nx = x + dx[k]; int nz = z + dz[k];
 if (nx <0 || nz <0 || nx >= w || nz >= h) continue;
 int ni = nx + nz * w;
 if (cost[ni] < best) { best = cost[ni]; bx = nx; bz = nz; }
 }
 var d = new Vector2(bx - x, bz - z);
 if (d.sqrMagnitude >0) dir[i] = d.normalized; else dir[i] = Vector2.zero;
 }
 }

 public Vector3 SampleDirection(Vector3 worldPos)
 {
 int x = WorldToX(worldPos.x); int z = WorldToZ(worldPos.z);
 if (x <0 || z <0 || x >= w || z >= h) return Vector3.zero;
 var v = dir[x + z * w];
 return new Vector3(v.x,0f, v.y);
 }

 private int WorldToX(float wx) => Mathf.FloorToInt((wx - origin.x) / cs);
 private int WorldToZ(float wz) => Mathf.FloorToInt((wz - origin.y) / cs);
 }
}
