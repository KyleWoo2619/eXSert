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
    
    [Header("UI References")]
    [SerializeField] private Slider slider; // Old slider system (optional)
    [SerializeField] private HealthBar healthBar; // New fill-based health bar

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
        
        // Initialize health bar
        if (healthBar != null)
        {
            healthBar.SetHealth(health, maxHealth);
        }
        
        // Initialize slider if assigned (backwards compatibility)
        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = health;
        }
        
        if (!slider && !healthBar)
        {
            Debug.LogWarning($"{gameObject.name}: No health bar UI assigned!");
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

    //On death, if this is assigned to the player it will trigger game over. If it is on any other object however, they will be destroyed.
    public void Death()
    {
        if (this.health <= 0)
        {
            if (this.gameObject.tag == "Player")
            {
                // Don't reload the scene here! This causes infinite loading.
                // Instead, trigger a game over state or use SceneLoader.RestartFromCheckpoint()
                // For now, just log it and let the game over screen handle it
                Debug.Log("[PlayerHealthBarManager] Player died! (Removed auto scene reload to prevent infinite loading)");
                
                // TODO: Trigger game over UI or call SceneLoader.Instance.RestartFromCheckpoint() once
                // Make sure this only happens ONCE, not every frame!
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
        // Update new HealthBar (fillAmount based)
        if (healthBar != null)
        {
            healthBar.SetHealth(health, maxHealth);
        }
        
        // Update old slider (backwards compatibility)
        if (slider != null)
        {
            slider.value = health;
        }
    }

    //saves and loads data from this script
    public void LoadData(GameData data)
    {
        // Restore health from save
        maxHealth = data.maxHealth > 0 ? data.maxHealth : maxHealth;
        health = Mathf.Clamp(data.health, 0, maxHealth);

        // Push to UI safely
        if (healthBar == null)
        {
            // Try to auto-bind if not wired in prefab
            healthBar = FindAnyObjectByType<HealthBar>();
        }
        if (healthBar != null)
        {
            healthBar.SetHealth(health, maxHealth);
        }
        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = health;
        }
    }

    public void SaveData(GameData data)
    {
        // Persist current health (prefer slider if present, else field)
        data.maxHealth = maxHealth;
        data.health = slider != null ? slider.value : health;
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