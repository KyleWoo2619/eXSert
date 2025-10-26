using System;

public class DiaryEvents
{
    
    public event Action<string> onFoundDiary;
    public void FoundDiary(string id)
    {
        if (onFoundDiary != null)
        {
            onFoundDiary(id);
        }
    }
    //This action is meant to be subscribed when the state of a log is changed
    public event Action<Diaries> onDiaryStateChange;
    public void DiaryStateChange(Diaries id)
    {
        if(onDiaryStateChange != null)
        {
            onDiaryStateChange(id);
        }
    }
}
