/*
Written by Brandon Wahl

Uses the health interfaces to increase or decreae hp amount and sets the healthbar accordingly

*/

/*
 * Written by Will T
 * 
 * Health Bar Manager should be attached to the health bar UI element of the player instead of the player object itself
 * 
 * A different script like CombatManager should handle the player's health logic 
 * and variables and this script should just handle the health bar UI updates
 * 
 * Health Bar Manager should pull the health values from CombatManager 
 * or another health system script instead of storing its own health values and handling health logic
 * 
 */

using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarManager : MonoBehaviour, IHealthSystem, IDataPersistenceManager

{
    public float maxHealth;
    public float health;

    //Assigns the variables from the health interfaces to variables in this class
    float IHealthSystem.currentHP => health; 
    float IHealthSystem.maxHP => maxHealth;

    //Grabs the function from the health interface, updates the health count, and updates the health bar
    public void HealHP(float hp)
    {
        health += hp;

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
    }

    //On death, if this is assigned to the player it will take them to the "Gameover" screen. If it is on any other object however, they will be destroyed.
    public void OnPlayerDeath()
    {
        if (health <= 0)
        {
            
        }
    }
    public void LoadData(GameData data)
    {
        health = data.health;
        //slider.value = data.health;
    }

    public void SaveData(GameData data)
    {
        data.health = health;
        //data.health = slider.value;
    }

}