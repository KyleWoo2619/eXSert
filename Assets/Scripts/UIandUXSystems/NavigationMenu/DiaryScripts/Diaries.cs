using UnityEngine;

public class Diaries
{
    public DiarySO info;


    //Default log values
    public Diaries(DiarySO diaryInfo)
    {
        this.info = diaryInfo;
    }

    //Retrieves saved log data
    public DiaryData GetDiaryData()
    {
        return new DiaryData(info);
    }

    public void StoreDiaryState(string id, DiarySO stateOfDiary)
    {
        info = stateOfDiary;
    }
}
