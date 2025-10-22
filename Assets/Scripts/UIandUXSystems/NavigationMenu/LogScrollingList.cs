using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class LogScrollingList : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentParent;

    [Header("Log Entry Button")]
    [SerializeField] private GameObject logEntryButtonPrefab;

    [Header("Rect Transforms")]
    [SerializeField] private RectTransform scrollRectTransform;
    [SerializeField] private RectTransform contentRectTransform;
    private Dictionary<string, LogButton> idToButtonMap = new Dictionary<string, LogButton>();


    public LogButton CreateButtonIfNotExists(Logs log, UnityAction selectAction)
    {
        LogButton logButton = null;

        if (!idToButtonMap.ContainsKey(log.info.logID))
        {
            logButton = InstantiateLogButton(log, selectAction);
        }
        else
        {
            logButton = idToButtonMap[log.info.logID];
        }
        return logButton;
    }

    private LogButton InstantiateLogButton(Logs log, UnityAction selectAction)
    {
        LogButton logButton = Instantiate(
            logEntryButtonPrefab,
            contentParent.transform).GetComponent<LogButton>();

        logButton.gameObject.name = log.info.logID + "_button";

        RectTransform buttonRectTranform = logButton.GetComponent<RectTransform>();

        logButton.InitializeButton(log.info.logName, () =>
        {
            selectAction();
            UpdateScrolling(buttonRectTranform);
        });

        idToButtonMap[log.info.logID] = logButton;

        return logButton;
    }

    private void UpdateScrolling(RectTransform buttonRectTransform)
    {
        float buttonYMin = Mathf.Abs(buttonRectTransform.anchoredPosition.y);
        float buttonYMax = buttonYMin + buttonRectTransform.rect.height;

        float contentYMin = contentRectTransform.anchoredPosition.y;
        float contentYMax = contentYMin + scrollRectTransform.rect.height;

        if (buttonYMax > contentYMax)
        {
            contentRectTransform.anchoredPosition = new Vector2(
                contentRectTransform.anchoredPosition.x,
                buttonYMax - scrollRectTransform.rect.height
            );
        }
        else
        {
            contentRectTransform.anchoredPosition = new Vector2(
                contentRectTransform.anchoredPosition.x,
                buttonYMin
            );
        }
    }
}
