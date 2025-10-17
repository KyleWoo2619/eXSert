/*
Written by Brandon Wahl

This interface defines these two functions that will be injected into any class that has variables that need to be saved

*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IDataPersistenceManager
{

    void LoadData(GameData data);

    void SaveData(GameData data);
}
