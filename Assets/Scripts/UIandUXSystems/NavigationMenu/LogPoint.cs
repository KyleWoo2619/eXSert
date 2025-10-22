using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LogPoint : MonoBehaviour
{

    [Header("Log Entry")]
    [SerializeField] private NavigationLogSO logInfoForPoint;

    private bool playerIsNear = false;
    private string logId;
    private LogState currentLogState;

    private void Awake()
    {
        logId = logInfoForPoint.logID;
    }

    private void OnEnable()
    {
        EventsManager.Instance.logEvents.onLogStateChange += LogStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.logEvents.onLogStateChange -= LogStateChange;
    }

   /* private void SubmitPressed()
    {
        if (!playerIsNear)
        {
            return;
        }


    }*/

    private void LogStateChange(Logs logs)
    {
        if (logs.info.logID.Equals(logId))
        {
            currentLogState = logs.state;
            Debug.Log("Log with id " + logId + " updated to state: " + currentLogState);
        }
    }

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
