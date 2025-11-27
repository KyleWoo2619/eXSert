using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class MovingObstacle : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    public bool loop = true; // ping-pong by default

    NavMeshObstacle _ob;
    Vector3 _target;

    void Awake()
    {
        _ob = GetComponent<NavMeshObstacle>();
        _ob.carving = true;
        if (pointA != null) transform.position = pointA.position;
        _target = pointB != null ? pointB.position : transform.position;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;
        transform.position = Vector3.MoveTowards(transform.position, _target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _target) < 0.01f)
        {
            _target = _target == pointA.position ? pointB.position : pointA.position;
            if (!loop && _target == pointA.position) enabled = false;
        }
    }
}