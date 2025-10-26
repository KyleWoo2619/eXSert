/*
    This manager will manage specific events that occur in eXsert. Finding diary entries or logs are such examples

    Written by Brandon Wahl
*/
using Singletons;
public class EventsManager : Singleton<EventsManager>
{
    public LogEvents logEvents;
    public DiaryEvents diaryEvents;

    protected override void Awake()
    {
        logEvents = new LogEvents();
        diaryEvents = new DiaryEvents();

        base.Awake();
    }
}
