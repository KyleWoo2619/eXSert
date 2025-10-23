/*
    Manages the state and info of each indivdual Scriptable Object Log

    Written by Brandon Wahl
*/

public class Logs
{
    public NavigationLogSO info;

    public LogState state;

    //Default log values
    public Logs(NavigationLogSO logInfo)
    {
        this.info = logInfo;
        this.state = LogState.NOT_FOUND;
    }

    //This is used to updated the log SO
    public Logs(NavigationLogSO logInfo, LogState logState)
    {
        this.info = logInfo;
        this.state = logState;
    }

    //Retrieves saved log data
    public LogData GetLogData()
    {
        return new LogData(state);
    }

    public void StoreLogState(string id, LogState stateOfLog)
    {
        state = stateOfLog;
    }
}
