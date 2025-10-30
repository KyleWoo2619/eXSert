/*
    Written by Brandon

    This script is mainly used as a reference for a specfic scriptable object, mainly referencing the info variable
*/
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

    //Stores diary log
    public void StoreDiaryState(string id, DiarySO stateOfDiary)
    {
        info = stateOfDiary;
    }
}
