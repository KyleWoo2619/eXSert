using UnityEngine;
using UnityEngine.UI;

/*
Written for connecting player's health to UI slider
Finds the player's HealthBarManager and updates the slider accordingly
*/

public class PlayerHealthCanvas : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerHealthBarManager playerHealthManager;
    
    void Awake()
    {
        // Auto-find the slider if not assigned
        if (!healthSlider)
            healthSlider = GetComponentInChildren<Slider>();
            
        // Auto-find the player's HealthBarManager if not assigned
        if (!playerHealthManager)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player)
                playerHealthManager = player.GetComponent<PlayerHealthBarManager>();
        }
    }

    void Start()
    {
        // Initialize the slider with player's max health
        if (healthSlider && playerHealthManager)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = playerHealthManager.maxHealth;
            healthSlider.value = playerHealthManager.health;
        }
        else
        {
            Debug.LogError($"{gameObject.name}: Missing references! HealthSlider: {healthSlider != null}, PlayerHealthManager: {playerHealthManager != null}");
        }
    }

    void Update()
    {
        // Continuously update the slider value to match player's current health
        if (healthSlider && playerHealthManager)
        {
            healthSlider.value = playerHealthManager.health;
        }
    }
}
