/*
Written by Brandon Wahl

Detects whether the player character is touching the grab by using a boxcast and layermask

*/

using UnityEngine;
public class GroundCheck : MonoBehaviour
{

    private Vector3 boxSize = new Vector3(.8f, .1f, .8f);
    [SerializeField] private float maxDistance;
    [Tooltip("Which layer the ground check detects for")] public LayerMask layerMask;

    //Returns true or false if boxcast collides with the layermask
    public bool AmIGrounded()
    {
        if (Physics.BoxCast(transform.position, boxSize, -transform.up, transform.rotation, maxDistance, layerMask))
        {
            Debug.Log("Yes");

            return true;
        }
        else
        {
            Debug.Log("No");

            return false;
        }
    }

    //Draws the boxcast for debugging
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position - transform.up * maxDistance, boxSize);
    }

}