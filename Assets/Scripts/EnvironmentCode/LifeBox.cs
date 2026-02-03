/*
    Written By Brandon Wahl

    This script will be used to create life box objects in the game. 
    A life box is an area that will allow the player to stay alive.
    Once the player leaves its bounds, they will die.
*/
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider))]
public class LifeBox : MonoBehaviour
{
    [SerializeField] private bool showHitBox = true;

    [SerializeField] private Vector3 boxSize = Vector3.one;

    private BoxCollider boxCollider;


    private void Awake()
    {
        this.tag = "LifeBox";
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.size = boxSize;
        boxCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();
        
        if (boxCollider != null)
        {
            boxCollider.size = boxSize;
            boxCollider.isTrigger = true;
        }
    }

#if UNITY_EDITOR
    [MenuItem("GameObject/Environment/LifeBox", false, 10)]
    public static void CreateLifeBox(MenuCommand menuCommand)
    {
        GameObject lifeBoxGO = new GameObject("LifeBox");
        lifeBoxGO.AddComponent<LifeBox>();
        GameObjectUtility.SetParentAndAlign(lifeBoxGO, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(lifeBoxGO, "Create LifeBox");
        Selection.activeObject = lifeBoxGO;
    }
#endif

    private void OnDrawGizmos()
    {
        if (showHitBox)
        {
            Gizmos.color = Color.purple * new Color(1, 1, 1, 0.25f);
            Gizmos.DrawCube(transform.position, boxSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, boxSize);
        }
    }

}
