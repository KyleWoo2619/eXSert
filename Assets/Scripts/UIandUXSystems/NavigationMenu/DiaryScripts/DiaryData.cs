/*
    Written by Brandon

    Temp script to allow the diary state to be saved.
*/

[System.Serializable]
public class DiaryData
{
    public DiarySO info;

    public DiaryData(DiarySO info)
    {
        this.info = info;
    }
}

