using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class MovingPlatform : MonoBehaviour
{
    public Transform[] pathPoints;
    public float speed = 2f;
    public bool loop = true;
    public bool carveWhileMoving = true; // if true, platform will carve navmesh

    NavMeshObstacle _ob;
    int _index = 0;
    Vector3 _target;

    void Awake()
    {
        _ob = GetComponent<NavMeshObstacle>();
        _ob.carving = carveWhileMoving;
        if (pathPoints == null || pathPoints.Length == 0) return;
        transform.position = pathPoints[0].position;
        _index = 1 % pathPoints.Length;
        _target = pathPoints[_index].position;
    }

    void Update()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;
        transform.position = Vector3.MoveTowards(transform.position, _target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _target) < 0.01f)
        {
            _index++;
            if (_index >= pathPoints.Length)
            {
                if (loop) _index = 0;
                else { enabled = false; return; }
            }
            _target = pathPoints[_index].position;
        }
    }
}