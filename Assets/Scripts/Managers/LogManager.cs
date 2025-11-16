/*
    This singleton is suppose to manage of the existing log scriptable objects in eXsert.
    It will manage if they're found or a duplicate, and temporarily handles the saving of the logs

    Written by Brandon Wahl
*/

using UnityEngine;
using Singletons;
using System.Collections.Generic;
using System;

public class LogManager : Singleton<LogManager>
{
    [Header("Debug")]
    [SerializeField] private bool loadLogState = true;

    private Dictionary<string, Logs> logMap;


    protected override void Awake()
    {

        logMap = CreateLogMap();

        base.Awake();
    }

    private void OnEnable()
    {
        EventsManager.Instance.logEvents.onFoundLog += FindLog;
    }
    
    private void OnDisable()
    {
        EventsManager.Instance.logEvents.onFoundLog -= FindLog;

    }

    private void Start()
    {
        foreach(Logs log in logMap.Values)
        {
            EventsManager.Instance.logEvents.LogStateChange(log);
        }
    }

    //This function will be used so the findLog function can change the state of the log to true, it will then store the data of the log
    private void ChangeTheStateOfLog(string id, NavigationLogSO log)
    {
        Logs logs = GetLogById(id);
        logs.info.isFound = log.isFound;
        EventsManager.Instance.logEvents.LogStateChange(logs);
        logs.StoreLogState(id, log);
    }

    //Changes the state of the log and if it is Found, it will turn isLogFound true
    private void FindLog(string id)
    {
        Debug.Log("Found Log: " + id);
        Logs logs = GetLogById(id);
        ChangeTheStateOfLog(logs.info.logID, logs.info);
    }

    //This dictionary will hold all the unique log entries and ensure there is no dupes
    private Dictionary<string, Logs> CreateLogMap()
    {
        NavigationLogSO[] allLogs = Resources.LoadAll<NavigationLogSO>("Logs");

        Dictionary<string, Logs> idToLogMap = new Dictionary<string, Logs>();
        foreach (NavigationLogSO logInfo in allLogs)
        {
            if (idToLogMap == null)
            {
                Debug.LogWarning("Duplicate ID found when creating log map: " + logInfo.logID);
            }
            idToLogMap.Add(logInfo.logID, LoadLog(logInfo));
        }
        return idToLogMap;
    }

    //Used to grab the specifc id string in a log
    private Logs GetLogById(string id)
    {
        Logs logs = logMap[id];
        if (logs == null)
        {
            Debug.LogError("ID not found in Log Map: " + id);
        }
        return logs;
    }

    private void OnApplicationQuit()
    {
        foreach (Logs log in logMap.Values)
        {
            SaveLog(log);
        }
    }

    //Temporary save feature for logs
    private void SaveLog(Logs log)
    {
        try
        {
            LogData logData = log.GetLogData();
            string serializedData = JsonUtility.ToJson(logData);
            PlayerPrefs.SetString(log.info.logID, serializedData);
        }
        catch (SystemException e)
        {
            Debug.LogError("Failed to save log with id " + log.info.logID + ": " + e);
        }
    }

    private Logs LoadLog(NavigationLogSO logInfo)
    {
        Logs log = null;
        try
        {
            if (PlayerPrefs.HasKey(logInfo.logID) && loadLogState)
            {
                string serializedData = PlayerPrefs.GetString(logInfo.logID);
                LogData logData = JsonUtility.FromJson<LogData>(serializedData);
                log = new Logs(logInfo);
            }
            else
            {
                log = new Logs(logInfo);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load log with id " + log.info.logID + ": " + e);
        }
        
        return log;
    }
}
