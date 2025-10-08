/*
Written by Brandon Wahl

Uses the health interfaces to increase or decreae hp amount and sets the healthbar accordingly

*/

using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class PlayerHealthBarManager : MonoBehaviour, IHealthSystem, IDataPersistenceManager

{
    public float maxHealth;
    public float health;
    [SerializeField] private Slider slider;

    //Temporary ways to reset the scene the player is currently in for demonstration
    Scene scene;
    string sceneName;

    //Assigns the variables from the health interfaces to variables in this class
    float IHealthSystem.currentHP => health; 
    float IHealthSystem.maxHP => maxHealth;

    void Start()
    {
        scene = SceneManager.GetActiveScene();
        sceneName = scene.name;
        
        // Initialize slider if not assigned
        if (!slider)
        {
            Debug.LogWarning($"{gameObject.name}: HealthBarManager slider is not assigned. The PlayerHealthCanvas should handle UI updates instead.");
        }

    }

    void Update()
    {
        Death();
    }

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

    //On death, if this is assigned to the player it will take them to the "Gameover" screen. If it is on any other object however, they will be destroyed.
    public void Death()
    {
        if (this.health <= 0)
        {

            if (this.gameObject.tag == "Player")
            {
                SceneManager.LoadSceneAsync(sceneName);
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
        if (slider != null)
        {
            slider.value = health;
        }
        // Note: PlayerHealthCanvas will handle UI updates if this slider is null
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

    //If the player collides with a trigger tagged with enemy, it'll gather it's hitbox damage amount and apply it to the player
    private void OnTriggerEnter(Collider other)
    {
        var hitbox = other.GetComponent<HitboxDamageManager>();

        if(other.tag == "Enemy")
        {
            LoseHP(hitbox.damageAmount);

            if(this.health <= 0)
            {
                Death();
            }
        }
    }

}