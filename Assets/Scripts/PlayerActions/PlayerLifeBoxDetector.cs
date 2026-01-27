using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLifeBoxDetector : MonoBehaviour
{
    [Header("Debugging")]
    [SerializeField] private bool killPlayerWhenOutOfLifeBox = true;

    [Space(10)]

    [Header("Lifebox Settings")]
    [SerializeField] private float checkInterval = 0.5f;

    [SerializeField] private List<LifeBox> lifeBoxes = new List<LifeBox>();

    protected string lifeBoxTag = "LifeBox";

    private void Start()
    {
        StartCoroutine(CheckIfInLifeBox());
    }

    // Continuously check if the player is inside any life boxes   
    private IEnumerator CheckIfInLifeBox()
    {
        while(true)
        {
            if (CheckIfLifeBoxesEmpty() && killPlayerWhenOutOfLifeBox)
            {
                // Player is out of life boxes, trigger death
                Debug.Log("Player has left all life boxes and will die.");
                yield break; // Exit the coroutine after death
            }
            yield return new WaitForSeconds(checkInterval); // Check every half second            
        }
    }

    private bool CheckIfLifeBoxesEmpty()
    {
        if(lifeBoxes.Count == 0)
        {
            return true;
        }

        return false;
    }

    // Doesnt immediately remove the life box to avoid issues with OnTriggerExit being called before OnTriggerEnter
    private IEnumerator WaitToDeleteLifeBox(LifeBox box)
    {
        yield return new WaitForSeconds(0.5f);
        lifeBoxes.Remove(box);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(lifeBoxTag))
        {
            if(!lifeBoxes.Contains(other.GetComponent<LifeBox>()))
                lifeBoxes.Add(other.GetComponent<LifeBox>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(lifeBoxTag))
        { 
            StartCoroutine(WaitToDeleteLifeBox(other.GetComponent<LifeBox>()));
        }
    }
}
