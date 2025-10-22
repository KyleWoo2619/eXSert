using System;

public class LogEvents 
{
    public event Action<string> onFoundLog;
    public void FoundLog(string id)
    {
        if (onFoundLog != null)
        {
            onFoundLog(id);
        }
    }
    
    public event Action<Logs> onLogStateChange;
    public void LogStateChange(Logs id)
    {
        if(onLogStateChange != null)
        {
            onLogStateChange(id);
        }
    }
}
