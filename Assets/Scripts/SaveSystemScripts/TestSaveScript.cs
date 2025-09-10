using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
//Written By Brandon
public class TestSaveScript : MonoBehaviour, IDataPersistenceManager
{
    //Temporary file to demonstrate save feature

    private int testCounter;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    //Any class that has variables that needs to be saved should have these two functions from the IDPM Interface
    public void LoadData(GameData data)
    {
        testCounter = data.test;
    }

    public void SaveData(GameData data)
    {
        data.test = testCounter;
    }

    //Button script
    public void IncreaseCounter()
    {
        testCounter++;
        Debug.Log(testCounter);
    }
}
