using UnityEngine;

[System.Serializable]
public class LogData
{
    public LogState state;

    public LogData(LogState state)
    {
        this.state = state;
    }
}
