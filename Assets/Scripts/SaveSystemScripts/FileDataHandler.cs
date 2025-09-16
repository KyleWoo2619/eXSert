using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
//Written By Brandon
public class FileDataHandler
{
    //These two variables make up the file path
    private string dataDirPath = "";

    private string dataFileName = "";

    //Defines the two above variables
    public FileDataHandler(string dataDirPath, string dataFileName)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
    }

    //This function properly loads the saved data
    public GameData Load(string profileId)
    {
        if(profileId == null)
        {
            return null;
        }

        //Combines the two path variables into one
        string fullPath = Path.Combine(dataDirPath, profileId, dataFileName);

        GameData loadedData = null;

        //If the file above can be located, then this will execute
        if (File.Exists(fullPath))
        {
            try
            {
                string dataToLoad = "";

                //Here the file is located, opened, and defined to the variable stream
                using (FileStream stream = new FileStream(fullPath, FileMode.Open))
                {
                    //The file/variable is being read and assigned to the variable reader
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        //the data that is read is being assigned to dataToLoad
                        dataToLoad = reader.ReadToEnd();
                    }
                }
                //The read data is serialiazed with JsonUtility and assigned to loadedData
                loadedData = JsonUtility.FromJson<GameData>(dataToLoad);

            }
            //If the file has any errors with being open, this error is returned
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to load date to file: " + fullPath + "\n" + e);
            }

        }

        return loadedData;
    }

    public void Save(GameData data, string profileId)
    {
        if (profileId == null)
        {
            return;
        }

        //Combines the two path variables into one
        string fullPath = Path.Combine(dataDirPath, profileId, dataFileName);

        try
        {
            //Creates a directory with the fullPath
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            //string variable that will convert the data that needs to be saved to Json
            string dataToStore = JsonUtility.ToJson(data, true);

            //Using FileStream, a variable is assigned to create a new file with the fullPath
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                //Writes the data that is being saved and assigns it to dataToStore
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataToStore);
                }
            }
        }
        //If the file cant be saved it will return this error
        catch (Exception e)
        {
            Debug.LogError("Error occured when trying to save date to file: " + fullPath + "\n" + e);
        }
    }

    public Dictionary<string, GameData> LoadAllProfiles()
    {
        Dictionary<string, GameData> profileDictionary = new Dictionary<string, GameData>();

        IEnumerable<DirectoryInfo> dirInfos = new DirectoryInfo(dataDirPath).EnumerateDirectories();
        foreach (DirectoryInfo dirInfo in dirInfos)
        {
            string profileId = dirInfo.Name;

            string fullPath = Path.Combine(dataDirPath, profileId, dataFileName);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("This directory does not contain data");
                continue;
            }

            GameData profileData = Load(profileId);

            if (profileData != null)
            {
                profileDictionary.Add(profileId, profileData);
            }
            else
            {
                Debug.LogWarning("Tried to load profile but something went wrong. ProfileId: " + profileId);
            }
        }

        return profileDictionary;
    }

    public string GetMostRecentUpdatedProfile()
    {
        string mostRecentProfileId = null;

        Dictionary<string, GameData> profilesGameData = LoadAllProfiles();

        foreach (KeyValuePair<string, GameData> pair in profilesGameData)
        {
            string profileId = pair.Key;
            GameData gameData = pair.Value;

            if (gameData == null)
            {
                continue;
            }

            if (mostRecentProfileId == null)
            {
                mostRecentProfileId = profileId;
            }
            else
            {
                DateTime mostRecentDateTime = DateTime.FromBinary(profilesGameData[mostRecentProfileId].lastUpdated);
                DateTime newDateTime = DateTime.FromBinary(gameData.lastUpdated);

                if (newDateTime > mostRecentDateTime)
                {
                    mostRecentProfileId = profileId;
                }
            }
        }
        return mostRecentProfileId;
    }
}
