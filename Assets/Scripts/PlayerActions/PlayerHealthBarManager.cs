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
    // WT: these variables should be moved to CombatManager
    public float maxHealth;
    public float health;
    [SerializeField] private Slider slider;

    // WT: Audio variables and logic should be under the player object and subscribed to the player hurt event
    [Space, Header("Audio")]
    private AudioSource playSFX;
    [SerializeField] AudioClip damagedAudioClip;

    //Assigns the variables from the health interfaces to variables in this class
    float IHealthSystem.currentHP => health; 
    float IHealthSystem.maxHP => maxHealth;


    void Start()
    {

        // Initialize slider if not assigned
        if (!slider)
        {
            Debug.LogWarning($"{gameObject.name}: HealthBarManager slider is not assigned. The PlayerHealthCanvas should handle UI updates instead.");
        }
        
        playSFX = SoundManager.Instance.sfxSource;

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
        playSFX.clip = damagedAudioClip;
        
        health -= damage;
        playSFX.Play();
        
        SetHealth();

        //detects if the gameobject has gone below their health count
       
    }

    /* WT: 
     * 
     * Please rename this function to be more specific
     * 
     * Additionally, when the player dies it shouldn't reload the current scene
     * We will have a player death/game over event that other systems can subscribe to to handle death logic
     * That death event will be inside CombatManager
     */

    //On death, if this is assigned to the player it will take them to the "Gameover" screen. If it is on any other object however, they will be destroyed.
    public void Death()
    {
        if (this.health <= 0)
        {

            if (this.gameObject.tag == "Player")
            {

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

    /*
     * WT:
     * 
     * I've noticed a few scripts each having their own LoadData and SaveData functions
     * 
     * It would probably be best to have a dedicated SaveSystemManager that handles all saving and loading of data
     * 
     * This way we can avoid having multiple scripts implementing IDataPersistenceManager and having redundant LoadData and SaveData functions
     */

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


    /*
     * WT:
     * 
     * Logic regarding taking damage should be handled in the source of the damage instead of the receiver
     * 
     * The source of the damage should check if it intersects with the player then tell the player to take damage
     * 
     * This should also be the same regarding the player's attacks hitting enemies
     */

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