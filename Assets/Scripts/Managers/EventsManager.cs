using UnityEngine;
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
