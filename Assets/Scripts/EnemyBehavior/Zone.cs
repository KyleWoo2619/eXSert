using UnityEngine;

[ExecuteAlways]
public class Zone : MonoBehaviour
{
    public Vector3 size = new Vector3(10, 5, 10);

    public Vector3 GetRandomPointInZone()
    {
        Vector3 center = transform.position;
        float x = Random.Range(center.x - size.x / 2, center.x + size.x / 2);
        float z = Random.Range(center.z - size.z / 2, center.z + size.z / 2);
        float y = center.y; // Keep y at zone's center or use NavMesh.SamplePosition for ground
        return new Vector3(x, y, z);
    }

    public bool Contains(Vector3 position)
    {
        Vector3 center = transform.position;
        Vector3 halfSize = size * 0.5f;
        return
            position.x >= center.x - halfSize.x && position.x <= center.x + halfSize.x &&
            position.y >= center.y - halfSize.y && position.y <= center.y + halfSize.y &&
            position.z >= center.z - halfSize.z && position.z <= center.z + halfSize.z;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawCube(transform.position, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, size);
    }
#endif
}
