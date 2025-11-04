/*
Written by Brandon Wahl

Any variables that need to be be saved and loaded should be defined here

*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    /*
     * WT: 
     * 
     * See if there is a way to dynamically add variables to be saved/loaded instead of hardcoding them
     * 
     * 
     */

    //All variables that need to be saved should be defined here
    public long lastUpdated;
    public float health;
    public float maxHealth;
    public Vector3 playerPos;
    
    // Checkpoint/Progress data
    public string currentSceneName;
    public string currentSpawnPointID;


    //Base variable definitions should be here
    public GameData()
    {
        // Starting stats for a new game
        maxHealth = 500;
        health = maxHealth;
        playerPos = Vector3.zero;
        
        // Default checkpoint is the first level
        currentSceneName = "FP_Elevator";
        currentSpawnPointID = "default";
    }
}
