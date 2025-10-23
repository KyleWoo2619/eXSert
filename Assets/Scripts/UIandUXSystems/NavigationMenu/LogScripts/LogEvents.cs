using System;
/*
    Handles the events assiocated with logs

    Written by Brandon Wahl
*/
public class LogEvents
{
    //This action is meant to be subscribed when the log is found
    public event Action<string> onFoundLog;
    public void FoundLog(string id)
    {
        if (onFoundLog != null)
        {
            onFoundLog(id);
        }
    }
    //This action is meant to be subscribed when the state of a log is changed
    public event Action<Logs> onLogStateChange;
    public void LogStateChange(Logs id)
    {
        if(onLogStateChange != null)
        {
            onLogStateChange(id);
        }
    }
}
