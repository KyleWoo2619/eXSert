/*
    Temp file to store log data

    Written by Brandon Wahl
*/

[System.Serializable]
public class LogData
{
    public string logID;
    public bool isFound;

    public LogData(NavigationLogSO info)
    {
        this.logID = info.logID;
        this.isFound = info.isFound;
    }
}
