using UnityEngine;

public class TestMove : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        // Hold W to move forward in world Z
        if (Input.GetKey(KeyCode.W))
            transform.position += Vector3.forward * speed * Time.deltaTime;
    }
}

