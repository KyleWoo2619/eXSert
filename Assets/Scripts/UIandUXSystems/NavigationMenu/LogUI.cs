using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class LogUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentParent;
    [SerializeField] private LogScrollingList scrollingList;
    [SerializeField] private TMP_Text logName;
    [SerializeField] private TMP_Text logDescription;
    [SerializeField] private TMP_Text logLocation;
    [SerializeField] private TMP_Text logId_Date;
    private Button firstSelected;

    private void OnEnable()
    {
        EventsManager.Instance.logEvents.onLogStateChange += LogStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.logEvents.onLogStateChange -= LogStateChange;
    }

    private void LogStateChange(Logs log)
    {
        LogButton logButton = scrollingList.CreateButtonIfNotExists(log, () =>
        {
            SetLogInfo(log);
        });

        if(firstSelected = null)
        {
            firstSelected = logButton.button;
            firstSelected.Select();
        }
    }

    private void SetLogInfo(Logs log)
    {
        logName.text = log.info.logName;
        logDescription.text = log.info.logDescription;
        logLocation.text = log.info.locationFound;
        logId_Date.text = (log.info.dateFound + " " + log.info.logID).ToString();
    }
}
