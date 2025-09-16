using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//Written By Brandon
public interface IDataPersistenceManager
{
    //This interface defines these two functions that will be injected into any class that has variables that need to be saved
    void LoadData(GameData data);

    void SaveData(GameData data);
}
