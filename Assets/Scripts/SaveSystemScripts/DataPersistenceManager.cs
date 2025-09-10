using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//Written By Brandon
public class DataPersistenceManager : MonoBehaviour
{

    private GameData gameData;

    [SerializeField] private string fileName;
    public static DataPersistenceManager instance { get; private set; }

    public List<IDataPersistenceManager> dataPersistenceObjects;

    private FileDataHandler fileDataHandler;

    private void Awake()
    {
        //If a DataPersistenceManager already exists in the scene and error is returned
        if(instance != null)
        {
            Debug.LogError("Found another manager in the scene");
        }
        
        instance = this;
    }

    private void Start()
    {
        //Defines the save file
        this.fileDataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        //Defines the variable dataPersistenceObjects to be the function below
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();
    }

    //When selecting a new game, new game data is created
    public void NewGame()
    {
        this.gameData = new GameData();
    }

    public void LoadGame()
    {
        //Loads the game data if it exists
        this.gameData = fileDataHandler.Load();

        //If it doesnt, it will call the NewGame function
        if(this.gameData == null)
        {
            Debug.Log("Intializing");
            NewGame();
        }
        //Goes through each of the found items that needs to be loaded and loads them
        foreach (IDataPersistenceManager dataPersistenceObj in dataPersistenceObjects)
        {
            dataPersistenceObj.LoadData(gameData);
        }
    }

    public void SaveGame()
    {
        //Goes through each of the found items that needs to be saved and saves them
        foreach (IDataPersistenceManager dataPersistenceObj in dataPersistenceObjects)
        {
            dataPersistenceObj.SaveData(gameData);
        }

        fileDataHandler.Save(gameData);
    }

    //When you quit the scene it will save the data, temporary
    private void OnApplicationQuit()
    {
        SaveGame();
    }

    //Defines a list that will find any instances of game data that needs to be loaded/saved
    private List<IDataPersistenceManager> FindAllDataPersistenceObjects()
    {
        IEnumerable<IDataPersistenceManager> dataPeristenceObjects = FindObjectsOfType<MonoBehaviour>().OfType<IDataPersistenceManager>();

        return new List<IDataPersistenceManager>(dataPeristenceObjects);
    }

}
