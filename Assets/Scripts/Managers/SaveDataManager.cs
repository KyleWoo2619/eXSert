/*
    Written By Brandon Wahl

    This script grabs any variable that needs to be saved/loaded and does each respective task
*/
using Singletons;

public class SaveDataManager : Singleton<SaveDataManager>, IDataPersistenceManager
{
    public void LoadData(GameData data)
    {
        CombatManager.Instance.health = data.health;

    }

    public void SaveData(GameData data)
    {
        data.health = CombatManager.Instance.health;
    }
}
