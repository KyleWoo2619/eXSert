using UnityEngine;

public class Logs
{
    public NavigationLogSO info;

    public LogState state;

    public Logs(NavigationLogSO logInfo)
    {
        this.info = logInfo;
        this.state = LogState.NOT_FOUND;
    }

    public Logs(NavigationLogSO logInfo, LogState logState)
    {
        this.info = logInfo;
        this.state = logState;
    }

    public LogData GetLogData()
    {
        return new LogData(state);
    }

    public void StoreLogState(string id, LogState stateOfLog)
    {
        state = stateOfLog;
    }
}
