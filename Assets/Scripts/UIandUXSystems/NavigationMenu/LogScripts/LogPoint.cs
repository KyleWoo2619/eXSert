/*
    This script will be used for the actual points in the world where the logs will be located

    Written by Brandon Wahl
*/
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LogPoint : MonoBehaviour
{

    [Header("Log Entry")]
    [SerializeField] private NavigationLogSO logInfoForPoint;

    private bool playerIsNear = false;
    private string logId;
    private bool currentLogState;

    private void Awake()
    {
        logId = logInfoForPoint.logID;
    }

    //Subscribes and unsubscribes to onLogStateChange
    private void OnEnable()
    {
        EventsManager.Instance.logEvents.onLogStateChange += LogStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.logEvents.onLogStateChange -= LogStateChange;
    }

    //This script will directly switch the log state to found
    private void SubmitPressed()
    {
        if (!playerIsNear)
        {
            return;
        }


    }

    //Changes the log state if any such changes occur
    private void LogStateChange(Logs logs)
    {
        if (logs.info.logID.Equals(logId))
        {
            currentLogState = logs.info.isFound;
            Debug.Log("Log with id " + logId + " updated to state: Is Found " + currentLogState);
        }
    }

    //PlayerIsNear bool changes depending on these
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNear = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerIsNear = false;
        }
    }

}
