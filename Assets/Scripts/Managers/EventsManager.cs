/*
    This manager will manage specific events that occur in eXsert. Finding diary entries or logs are such examples

    Written by Brandon Wahl
*/
using Singletons;
public class EventsManager : Singleton<EventsManager>
{
    public LogEvents logEvents;

    protected override void Awake()
    {
        logEvents = new LogEvents();

        base.Awake();
    }
}
