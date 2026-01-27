/*
    Written by Brandon

    Temp script to allow the diary state to be saved.
*/

[System.Serializable]
public class DiaryData
{
    public string diaryID;
    public bool isFound;

    public DiaryData(DiarySO info)
    {
        this.diaryID = info.diaryID;
        this.isFound = info.isFound;
    }
}

