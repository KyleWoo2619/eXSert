/*
    Contains all of the different pieces of text that will be changed depending on which log is selected

    Written by Brandon Wahl
*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
public class LogUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentParent;
    [SerializeField] private LogScrollingList scrollingList;
    [SerializeField] private TMP_Text logName;
    [SerializeField] private GameObject logDescription;
    [SerializeField] private TMP_Text logLocation;
    [SerializeField] private TMP_Text logId_Date;
    [SerializeField] private Image logImage;

    //LogStateChange beong subscribed and unsubscribed
    private void OnEnable()
    {
        EventsManager.Instance.logEvents.onLogStateChange += LogStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.logEvents.onLogStateChange -= LogStateChange;
    }

    //Creates the button with the info from SetLogInfo
    private void LogStateChange(Logs log)
    {
        LogButton logButton = scrollingList.CreateButtonIfNotExists(log, () =>
        {
            SetLogInfo(log);
           
        });
    }

    //Sets each log info
    private void SetLogInfo(Logs log)
    {
        logName.text = log.info.logName;
        logDescription.GetComponent<TMP_Text>().text = log.info.logDescription;
        logLocation.text = log.info.locationFound;
        logId_Date.text = log.info.logID;
        logImage.sprite = log.info.logImage.sprite;
    }

}
