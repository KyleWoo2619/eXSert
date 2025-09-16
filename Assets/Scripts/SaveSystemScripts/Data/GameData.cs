using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//Written By Brandon
[System.Serializable]
public class GameData
{
    //All variables that need to be saved should be defined here
    public long lastUpdated;
    public float health;
    public float maxHealth;

    //Base variable definitions should be here
    public GameData()
    {
        maxHealth = 10;
        health = maxHealth;
    }
}
