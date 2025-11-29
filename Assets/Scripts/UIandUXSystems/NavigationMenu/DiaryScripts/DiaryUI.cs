/*
    Written by Brandon

    This script will change the text and description of the diary view depending on which button is clicked.
*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DiaryUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject contentParent;
    [SerializeField] private DiaryScrollingList scrollingList;
    [SerializeField] private TMP_Text diaryID;
    [SerializeField] private GameObject diaryDescription;
    [SerializeField] private Image diaryImage;

    //DiaryStateChange being subscribed and unsubscribed
    private void OnEnable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange += DiaryStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange -= DiaryStateChange;
    }

    //Creates the button with the info from SetDiaryInfo
    private void DiaryStateChange(Diaries diaries)
    {
        DiaryButton diaryButton = scrollingList.CreateButtonIfNotExists(diaries, () =>
        {
            SetDiaryInfo(diaries);
           
        });
    }

    //Sets each diary info
    private void SetDiaryInfo(Diaries diaries)
    {
        diaryID.text = diaries.info.diaryID;
        diaryDescription.GetComponent<TMP_Text>().text = diaries.info.diaryDescription;
        diaryImage.sprite = diaries.info.diaryImage.sprite;
    }
}
