using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class GeneratedMovingPlatform : MonoBehaviour
{
 public Transform[] pathPoints;
 public float speed =1.5f;
 public bool loop = true;
 public float lingerSeconds =2f;
 // If true, a separate pit obstacle will carve the NavMesh while the platform is away
 public NavMeshObstacle pitObstacle;
 public bool carvePitWhileAway = true;

 NavMeshObstacle _ob;
 int _index =0;

 void Awake()
 {
 _ob = GetComponent<NavMeshObstacle>();
 // Platform itself should not carve (so agents can stand on it)
 if (_ob != null) _ob.carving = false;

 // ensure pit starts disabled (platform present at start)
 if (pitObstacle != null && carvePitWhileAway)
 {
 pitObstacle.carving = false;
 }

 if (pathPoints == null || pathPoints.Length ==0) return;
 transform.position = pathPoints[0].position;
 _index =1 % pathPoints.Length;
 }

 void OnEnable()
 {
 if (pathPoints != null && pathPoints.Length >0)
 StartCoroutine(MoveLoop());
 }

 void OnDisable()
 {
 StopAllCoroutines();
 }

 IEnumerator MoveLoop()
 {
 if (pathPoints == null || pathPoints.Length ==0) yield break;
 while (true)
 {
 // Before moving away, enable pit carving so area becomes non-walkable
 if (pitObstacle != null && carvePitWhileAway)
 pitObstacle.carving = true;

 Vector3 target = pathPoints[_index].position;
 while (Vector3.Distance(transform.position, target) >0.01f)
 {
 transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
 yield return null;
 }

 // Arrived: disable pit carving so agents can step onto platform
 if (pitObstacle != null && carvePitWhileAway)
 pitObstacle.carving = false;

 // Arrived, linger
 yield return new WaitForSeconds(Mathf.Max(0f, lingerSeconds));

 _index++;
 if (_index >= pathPoints.Length)
 {
 if (loop) _index =0;
 else yield break;
 }
 }
 }
}
