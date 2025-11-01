using System.Collections.Generic;
using UnityEngine;

namespace EnemyBehavior.Crowd
{
 public sealed class CrowdController : MonoBehaviour
 {
 [SerializeField] float highUpdateRate =10f;
 [SerializeField] float midUpdateRate =3.3f;
 [SerializeField] float lowUpdateRate =0.5f;

 private readonly List<CrowdAgent> _agents = new List<CrowdAgent>(512);

 public void Register(CrowdAgent a) => _agents.Add(a);
 public void Unregister(CrowdAgent a) => _agents.Remove(a);

 void Update()
 {
 float t = Time.time;
 foreach (var a in _agents)
 {
 if (a.ShouldTick(t))
 {
 if (a.NeedsReplan) a.RequestPath();
 a.ApplySteering();
 a.StampDensity();
 }
 }
 }
 }
}
