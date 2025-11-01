using UnityEngine;

[CreateAssetMenu(menuName = "AI/EnemyBehaviorProfile")]
public sealed class EnemyBehaviorProfile : ScriptableObject
{
 public Vector2 SpeedRange = new Vector2(2f,6f);
 public float Acceleration =12f;
 public float AngularSpeed =360f;
 public float StoppingDistance =0.2f;
 public int AvoidancePriority =50;
 public float PersonalSpaceRadius =0.6f;
 public string[] PreferredAreas;
 public float PreferredAreaCostMultiplier =0.8f;
 public bool ManyToOne;
 public bool AvoidCrowds;
 public int Importance =1;

 public PlannerHints ToPlannerHints()
 {
 PlannerHints h = PlannerHints.None;
 if (ManyToOne) h |= PlannerHints.ManyAgentsToSameGoal;
 if (AvoidCrowds) h |= PlannerHints.AvoidCrowds;
 return h;
 }
}
