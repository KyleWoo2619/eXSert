using UnityEngine;

namespace EnemyBehavior.Crowd
{
 public sealed class CrowdAgent
 {
 public NavMeshAgent Agent;
 public PathQuery Query;
 public PlannerHints Hints;
 public EnemyBehaviorProfile Profile;

 private float _nextTick;

 public bool ShouldTick(float timeNow)
 {
 if (timeNow >= _nextTick)
 {
 float hz = GetHzByImportance(Profile != null ? Profile.Importance :1);
 _nextTick = timeNow +1f / hz;
 return true;
 }
 return false;
 }

 public bool NeedsReplan => false; // TODO: implement triggers

 public void RequestPath()
 {
 var q = Query; q.Hints |= Profile != null ? Profile.ToPlannerHints() : PlannerHints.None;
 PathRequestManager.Instance.Enqueue(q);
 }

 public void ApplySteering()
 {
 if (Agent == null) return;
 // For now rely on NavMeshAgent built-in following
 }

 public void StampDensity()
 {
 if (Agent == null || Density.DensityGrid.Instance == null) return;
 Density.DensityGrid.Instance.Stamp(Agent.transform.position, Profile != null ? Profile.PersonalSpaceRadius :0.5f,1f);
 }

 private static float GetHzByImportance(int imp) => imp >=2 ?10f : imp ==1 ?3.3f :0.5f;
 }
}
