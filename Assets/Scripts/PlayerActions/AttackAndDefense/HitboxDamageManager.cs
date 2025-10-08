/*
Written by Brandon Wahl

This script is used to determine how much damage should be dealt when collided with using the attack interface. It also counts the weapon name for debugging purposes.


*/

using UnityEngine;

public class HitboxDamageManager : MonoBehaviour, IAttackSystem
{
    [SerializeField] internal string weaponName = "";
    [SerializeField] internal float damageAmount;

    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            Debug.LogWarning("This object MUST have a box collider");
        }
    }


    float IAttackSystem.damageAmount => damageAmount;
    string IAttackSystem.weaponName => weaponName;


    //If the box collider exists AND it is being used it will appear as red on the screen for debugging
    private void OnDrawGizmos()
    {
        if(boxCollider != null && boxCollider.enabled)
        {
            Gizmos.color = Color.red;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);

        }
    }
}
