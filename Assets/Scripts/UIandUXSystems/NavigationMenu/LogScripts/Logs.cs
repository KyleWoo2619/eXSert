/*
    Manages the state and info of each indivdual Scriptable Object Log

    Written by Brandon Wahl
*/

public class Logs
{
    public NavigationLogSO info;


    //Default log values
    public Logs(NavigationLogSO logInfo)
    {
        this.info = logInfo;
    }

    //Retrieves saved log data
    public LogData GetLogData()
    {
        return new LogData(info);
    }

    public void StoreLogState(string id, NavigationLogSO stateOfLog)
    {
        info = stateOfLog;
    }
}
