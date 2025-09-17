/*
Written by Brandon Wahl

Uses the health interfaces to increase or decreae hp amount and sets the healthbar accordingly

*/

using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HealthBarManager : MonoBehaviour, IHealthSystem, IDataPersistenceManager
{
    public float maxHealth;
    public float health;
    public Slider slider;

    //Assigns the variables from the health interfaces to variables in this class
    float IHealthSystem.currentHP => health; 
    float IHealthSystem.maxHP => maxHealth;

    //Grabs the function from the health interface, updates the health count, and updates the health bar
    public void HealHP(float hp)
    {
        health += hp;

        SetHealth();

        //prevents going over max health
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    //Grabs the function from the health interface, updates the health count, and updates the health bar
    public void LoseHP(float damage)
    {
        health -= damage;
        
        SetHealth();

        //detects if the gameobject has gone below their health count
        if (health <= 0)
        {
            Debug.Log("You're Dead");
            health = 0;
        }
    }

    //sets the healthbar according to which function is done
    public void SetHealth()
    {
        slider.value = health;
    }

    //saves and loads data from this script
    public void LoadData(GameData data)
    {
        health = data.health;
        slider.value = data.health;
    }

    public void SaveData(GameData data)
    {
        data.health = health;
        data.health = slider.value;
    }

}