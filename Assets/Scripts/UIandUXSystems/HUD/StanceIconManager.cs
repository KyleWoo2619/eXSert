using UnityEngine;
using UnityEngine.UI;

/*
Written by Kyle Woo

Detects the player's current stance and toggles UI icons accordingly.
Shows PileDriver icon for Single Attack stance and AOE icon for Area of Effect stance.
Active icons fade/wave between two colors.
*/

public class StanceIconManager : MonoBehaviour
{
    [Header("UI Icon References")]
    [SerializeField] private GameObject pileDriverIcon;
    [SerializeField] private GameObject aoeIcon;
    
    [Header("Color Animation")]
    [SerializeField] private Color activeColor1 = Color.white;
    [SerializeField] private Color activeColor2 = Color.grey;
    [SerializeField] private Color inactiveColor = Color.grey;
    [SerializeField] private float fadeSpeed = 2f;

    private Image activeStance => CombatManager.singleTargetMode ? pileDriverImage : aoeImage;
    private Image inactiveStance => CombatManager.singleTargetMode ? aoeImage : pileDriverImage;

    // Component references for color animation
    private Image pileDriverImage;
    private Image aoeImage;
    private float colorTimer = 0f;

    private void OnEnable()
    {
        // Subscribe to stance change event
        CombatManager.OnStanceChanged += UpdateStanceIcons;
    }

    private void OnDisable()
    {
        // Unsubscribe from stance change event
        CombatManager.OnStanceChanged -= UpdateStanceIcons;
    }

    void Start()
    {
        // Get Image components for color animation
        if (pileDriverIcon != null)
        {
            pileDriverImage = pileDriverIcon.GetComponent<Image>();
            if (pileDriverImage == null)
                Debug.LogWarning($"{gameObject.name}: PileDriver icon doesn't have an Image component!");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: PileDriver icon GameObject not assigned!");
        }

        if (aoeIcon != null)
        {
            aoeImage = aoeIcon.GetComponent<Image>();
            if (aoeImage == null)
                Debug.LogWarning($"{gameObject.name}: AOE icon doesn't have an Image component!");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: AOE icon GameObject not assigned!");
        }

        // Initialize icons based on starting stance
        UpdateStanceIcons();
    }

    void Update()
    {        
        // Animate active icon colors
        AnimateActiveIcon();
    }
    
    private void AnimateActiveIcon()
    {
        // Update color timer
        colorTimer += Time.deltaTime * fadeSpeed;
        
        // Calculate fade between colors using sine wave for smooth back-and-forth
        float fadeValue = (Mathf.Sin(colorTimer) + 1f) / 2f; // Convert -1,1 range to 0,1 range
        Color animatedColor = Color.Lerp(activeColor1, activeColor2, fadeValue);

        if (activeStance != null)
            activeStance.color = animatedColor;
        if (inactiveStance != null)
            inactiveStance.color = inactiveColor;
    }

    private void UpdateStanceIcons()
    {
        // Reset color timer for immediate color change on stance switch
        colorTimer = 0f;
    }
}