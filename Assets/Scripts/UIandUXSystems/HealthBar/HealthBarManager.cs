/*
Written by Brandon Wahl

Uses the health interfaces to increase or decreae hp amount and sets the healthbar accordingly

*/

using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

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
       
    }

    public void Death()
    {
        if (health <= 0)
        {
            if (this.gameObject.tag == "Player")
            {
                SceneManager.LoadSceneAsync("PlaceholderScene");
                health = 0;
            }
            else
            {
                Destroy(gameObject);
            }
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

    private void OnTriggerEnter(Collider other)
    {
        HitboxDamageManager hitboxDamageManager = other.GetComponent<HitboxDamageManager>();

        if (other.tag == "Enemy" && this.gameObject.tag == "Player")
        { 
            LoseHP(hitboxDamageManager.damageAmount);
        }
    }
}