using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//Written By Brandon
[System.Serializable]
public class GameData
{
    //All variables that need to be saved should be defined here
    public int test;
    public float health;
    public float maxHealth;

    //Base variable definitions should be here
    public GameData()
    {
        test = 0;
        maxHealth = 10;
        health = maxHealth;
    }
}
