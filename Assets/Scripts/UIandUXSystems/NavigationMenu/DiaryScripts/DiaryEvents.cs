/*
    Written by Brandon

    This script stores the specific events that occur in relation to player diaries
*/

using System;
public class DiaryEvents
{
    //This action will be subscribed when a diary is found
    public event Action<string> onFoundDiary;
    public void FoundDiary(string id)
    {
        if (onFoundDiary != null)
        {
            onFoundDiary(id);
        }
    }
    //This action is meant to be subscribed when the state of a diary is changed
    public event Action<Diaries> onDiaryStateChange;
    public void DiaryStateChange(Diaries id)
    {
        if(onDiaryStateChange != null)
        {
            onDiaryStateChange(id);
        }
    }
}
