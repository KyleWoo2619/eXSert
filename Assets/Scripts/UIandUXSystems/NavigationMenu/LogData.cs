/*
    Temp file to store log data

    Written by Brandon Wahl
*/

[System.Serializable]
public class LogData
{
    public LogState state;

    public LogData(LogState state)
    {
        this.state = state;
    }
}
