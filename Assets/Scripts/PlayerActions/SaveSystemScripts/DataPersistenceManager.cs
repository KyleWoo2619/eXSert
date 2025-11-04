/*
Written by Brandon Wahl

Handles data persistence and ensures data is properly saved and loaded

*/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;
using Singletons;

public class DataPersistenceManager : Singletons.Singleton<DataPersistenceManager>

{
    [Header("Debugging")]
    [SerializeField] private bool initializeDataIfNull = false;

    [SerializeField] private bool disableDataPersistence = false;

    [SerializeField] private bool overrideSelectedProfileId = false;

    [SerializeField] private string testSelectedProfile = "test";

    private GameData gameData;

    [SerializeField] private string fileName;

    private string selectedProfileId = "";
    public static DataPersistenceManager instance { get; private set; }

    public List<IDataPersistenceManager> dataPersistenceObjects;

    private FileDataHandler fileDataHandler;

    

    private void Awake()
    {
        //If a DataPersistenceManager already exists in the scene and error is returned
        if(instance != null)
        {
            Debug.LogError("Found another manager in the scene");
            Destroy(this.gameObject);
            return;
        }

        if (disableDataPersistence)
        {
            Debug.LogWarning("Data persistence is disabled.");
        }

        instance = this;

        DontDestroyOnLoad(this.gameObject);

        //Defines the save file
        this.fileDataHandler = new FileDataHandler(Application.persistentDataPath, fileName);

        this.selectedProfileId = fileDataHandler.GetMostRecentUpdatedProfile();

        //If the editor is using a test profile, it will warn them so they are aware
        if (overrideSelectedProfileId)
        {
            this.selectedProfileId = testSelectedProfile;
            Debug.LogWarning("Overrode selected profile id with test id: " + testSelectedProfile);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Defines the variable dataPersistenceObjects to be the function below
        this.dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();
    }

    public void ChangeSelectedProfileId(string newProfileId)
    { 
        this.selectedProfileId = newProfileId;
        LoadGame();
    }

    //When selecting a new game, new game data is created
    public void NewGame()
    {
        // Delete the existing save for the currently selected profile (clean reset)
        if (fileDataHandler != null && !string.IsNullOrEmpty(selectedProfileId))
        {
            fileDataHandler.DeleteProfile(selectedProfileId);
        }

        // Create fresh data with defaults
        this.gameData = new GameData();

        // Immediately persist the new defaults so the next scene load reads them
        if (!disableDataPersistence)
        {
            fileDataHandler.Save(this.gameData, selectedProfileId);
        }
    }

    public void LoadGame()
    {
        if (disableDataPersistence)
        {
            return;
        }

        //Loads the game data if it exists
        this.gameData = fileDataHandler.Load(selectedProfileId);

        if(this.gameData == null && initializeDataIfNull)
        {
            NewGame();
        }

        //If it doesnt, it will call the NewGame function
        if(this.gameData == null)
        {
            Debug.Log("Intializing");
            return;
        }
        //Goes through each of the found items that needs to be loaded and loads them
        foreach (IDataPersistenceManager dataPersistenceObj in dataPersistenceObjects)
        {
            dataPersistenceObj.LoadData(gameData);
        }
    }

    public void SaveGame()
    {
        if (disableDataPersistence)
        {
            return;
        }

        if (this.gameData == null)
        {
            Debug.LogWarning("No data was found");
        }

        //Goes through each of the found items that needs to be saved and saves them
        foreach (IDataPersistenceManager dataPersistenceObj in dataPersistenceObjects)
        {
            dataPersistenceObj.SaveData(gameData);
        }

        //Saves the current time, converts to binary, and assigns the data to gameData
        gameData.lastUpdated = System.DateTime.Now.ToBinary();

        fileDataHandler.Save(gameData, selectedProfileId);
    }

    /// <summary>
    /// Deletes the save file for the given profile id and refreshes menu data.
    /// </summary>
    public void DeleteProfile(string profileId)
    {
        if (disableDataPersistence) return;
        if (fileDataHandler == null || string.IsNullOrEmpty(profileId)) return;

        fileDataHandler.DeleteProfile(profileId);

        // If we deleted the currently-selected profile, pick another most-recent one
        if (selectedProfileId == profileId)
        {
            selectedProfileId = fileDataHandler.GetMostRecentUpdatedProfile();
            gameData = null;
        }
    }


    //Defines a list that will find any instances of game data that needs to be loaded/saved
    private List<IDataPersistenceManager> FindAllDataPersistenceObjects()
    {
        IEnumerable<IDataPersistenceManager> dataPeristenceObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IDataPersistenceManager>();

        return new List<IDataPersistenceManager>(dataPeristenceObjects);
    }

    //Returns true or false if there is game data
    public bool HasGameData()
    {
        return gameData != null;
    }

    public Dictionary<string, GameData> GetAllProfilesGameData()
    {
        return fileDataHandler.LoadAllProfiles();
    }
}
