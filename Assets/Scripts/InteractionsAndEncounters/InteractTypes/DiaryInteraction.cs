/*
    Written by Brandon Wahl

    Place this script where you want a diary entry to be interacted with and collected into the player's inventory.
*/

using UnityEngine;

public class DiaryInteraction : InteractionManager
{

    [SerializeField] private ScriptableObject diaryData;

    private void OnEnable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange += OnDiaryStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange -= OnDiaryStateChange;
    }

    private void OnDiaryStateChange(Diaries diaries)
    {
        if (diaries.info.diaryID.Equals(this.interactId))
        {
            Debug.Log("Diary with id " + this.interactId + " updated to state: Is Found " + diaries.info.isFound);
        }
    }


    protected override void Interact()
    {
        var diarySO = diaryData as NavigationLogSO;
        diarySO.isFound = true;

        EventsManager.Instance.diaryEvents.FoundDiary(this.interactId);
        DeactivateInteractable(this);

    }

}
