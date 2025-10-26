using UnityEngine;
using TMPro;
public class DiaryUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentParent;
    [SerializeField] private DiaryScrollingList scrollingList;
    [SerializeField] private TMP_Text diaryID;
    [SerializeField] private TMP_Text diaryDescription;

    //LogStateChange beong subscribed and unsubscribed
    private void OnEnable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange += DiaryStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange -= DiaryStateChange;
    }

    //Creates the button with the info from SetLogInfo
    private void DiaryStateChange(Diaries diaries)
    {
        DiaryButton diaryButton = scrollingList.CreateButtonIfNotExists(diaries, () =>
        {
            SetDiaryInfo(diaries);
           
        });
    }

    //Sets each log info
    private void SetDiaryInfo(Diaries diaries)
    {
        diaryID.text = diaries.info.diaryID;
        diaryDescription.text = diaries.info.diaryDescription;
    }
}
