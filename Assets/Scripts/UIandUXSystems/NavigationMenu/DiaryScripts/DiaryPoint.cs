/*
    This script will be used for the actual points in the world where the diary will be located

    Written by Brandon Wahl
*/
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DiaryPoint : MonoBehaviour
{

    [Header("Diary Entry")]
    [SerializeField] private DiarySO diaryInfoForPoint;

    private bool playerIsNear = false;
    private string diaryId;
    private bool currentDiaryState;

    private void Awake()
    {
        diaryId = diaryInfoForPoint.diaryID;
    }

    //Subscribes and unsubscribes to onDiaryStateChange
    private void OnEnable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange += DiaryStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.diaryEvents.onDiaryStateChange -= DiaryStateChange;
    }

    //This script will directly switch the diary state to found
    private void SubmitPressed()
    {
        if (!playerIsNear)
        {
            return;
        }


    }

    //Changes the diary state if any such changes occur
    private void DiaryStateChange(Diaries diaries)
    {
        if (diaries.info.diaryID.Equals(diaryId))
        {
            currentDiaryState = diaries.info.isFound;
            Debug.Log("Diary with id " + diaryId + " updated to state: Is Found " + currentDiaryState);
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
