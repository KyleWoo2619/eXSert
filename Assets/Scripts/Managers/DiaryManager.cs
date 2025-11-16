using UnityEngine;
using Singletons;
using System.Collections.Generic;
using System;


public class DiaryManager : Singleton<DiaryManager>
{
    [Header("Debug")]
    [SerializeField] private bool loadDiaryState = true;

    private Dictionary<string, Diaries> diaryMap;

    protected override void Awake()
    {

        diaryMap = CreateDiaryMap();

        base.Awake();
    }

     private void OnEnable()
    {
        EventsManager.Instance.diaryEvents.onFoundDiary += FindDiary;
    }
    
    private void OnDisable()
    {
        EventsManager.Instance.diaryEvents.onFoundDiary -= FindDiary;

    }

    private void Start()
    {
        foreach(Diaries diary in diaryMap.Values)
        {
            EventsManager.Instance.diaryEvents.DiaryStateChange(diary);
        }
    }

    //This function will be used so the findLog function can change the state of the log to true, it will then store the data of the log
    private void ChangeTheStateOfDiary(string id, DiarySO diary)
    {
        Diaries diaries = GetDiaryById(id);
        diaries.info.isFound = diary.isFound;
        EventsManager.Instance.diaryEvents.DiaryStateChange(diaries);
        diaries.StoreDiaryState(id, diary);
    }

    //Changes the state of the log and if it is Found, it will turn isLogFound true
    private void FindDiary(string id)
    {
        Debug.Log("Found Log: " + id);
        Diaries diaries = GetDiaryById(id);
        ChangeTheStateOfDiary(diaries.info.diaryID, diaries.info);
    }

    //This dictionary will hold all the unique log entries and ensure there is no dupes
    private Dictionary<string, Diaries> CreateDiaryMap()
    {
        DiarySO[] allDiaries = Resources.LoadAll<DiarySO>("Diaries");

        Dictionary<string, Diaries> idToDiaryMap = new Dictionary<string, Diaries>();
        foreach (DiarySO diaryInfo in allDiaries)
        {
            if (idToDiaryMap == null)
            {
                Debug.LogWarning("Duplicate ID found when creating diary map: " + diaryInfo.diaryID);
            }
            idToDiaryMap.Add((diaryInfo.diaryID), LoadDiary(diaryInfo));
        }
        return idToDiaryMap;
    }

    //Used to grab the specifc id string in a log
    private Diaries GetDiaryById(string id)
    {
        Diaries diaries = diaryMap[id];
        if (diaries == null)
        {
            Debug.LogError("ID not found in Diary Map: " + id);
        }
        return diaries;
    }

    private void OnApplicationQuit()
    {
        foreach (Diaries diaries in diaryMap.Values)
        {
            SaveDiary(diaries);
        }
    }

    //Temporary save feature for logs
    private void SaveDiary(Diaries diaries)
    {
        try
        {
            DiaryData diaryData = diaries.GetDiaryData();
            string serializedData = JsonUtility.ToJson(diaryData);
            PlayerPrefs.SetString(diaries.info.diaryID, serializedData);
        }
        catch (SystemException e)
        {
            Debug.LogError("Failed to save log with id " + diaries.info.diaryID + ": " + e);
        }
    }

    private Diaries LoadDiary(DiarySO diaryInfo)
    {
        Diaries diary = null;
        try
        {
            if (PlayerPrefs.HasKey(diaryInfo.diaryID) && loadDiaryState)
            {
                string serializedData = PlayerPrefs.GetString(diaryInfo.diaryID);
                LogData logData = JsonUtility.FromJson<LogData>(serializedData);
                diary = new Diaries(diaryInfo);
            }
            else
            {
                diary = new Diaries(diaryInfo);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load log with id " + diary.info.diaryID + ": " + e);
        }
        
        return diary;
    }
}
