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

    [Header("Stance Detection")]
    [SerializeField] private ChangeStance playerStanceScript;
    
    [Header("Color Animation")]
    [SerializeField] private Color activeColor1 = Color.white;
    [SerializeField] private Color activeColor2 = Color.grey;
    [SerializeField] private Color inactiveColor = Color.grey;
    [SerializeField] private float fadeSpeed = 2f;

    private int previousStance = -1; // Track previous stance to avoid unnecessary updates
    
    // Component references for color animation
    private Image pileDriverImage;
    private Image aoeImage;
    private float colorTimer = 0f;

    void Start()
    {
        // Auto-find ChangeStance script if not assigned
        if (playerStanceScript == null)
        {
            playerStanceScript = FindAnyObjectByType<ChangeStance>();
            if (playerStanceScript == null)
            {
                Debug.LogError($"{gameObject.name}: No ChangeStance script found in the scene!");
                enabled = false;
                return;
            }
        }

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
        // Only update if stance has changed
        if (playerStanceScript != null && playerStanceScript.currentStance != previousStance)
        {
            UpdateStanceIcons();
            previousStance = playerStanceScript.currentStance;
        }
        
        // Animate active icon colors
        AnimateActiveIcon();
    }
    
    private void AnimateActiveIcon()
    {
        if (playerStanceScript == null) return;
        
        // Update color timer
        colorTimer += Time.deltaTime * fadeSpeed;
        
        // Calculate fade between colors using sine wave for smooth back-and-forth
        float fadeValue = (Mathf.Sin(colorTimer) + 1f) / 2f; // Convert -1,1 range to 0,1 range
        Color animatedColor = Color.Lerp(activeColor1, activeColor2, fadeValue);
        
        // Apply animated color to active icon, static color to inactive icon
        int currentStance = playerStanceScript.currentStance;
        
        if (currentStance == 0) // Single Attack stance - animate PileDriver
        {
            if (pileDriverImage != null)
                pileDriverImage.color = animatedColor;
            if (aoeImage != null)
                aoeImage.color = inactiveColor;
        }
        else if (currentStance == 1) // AOE stance - animate AOE
        {
            if (aoeImage != null)
                aoeImage.color = animatedColor;
            if (pileDriverImage != null)
                pileDriverImage.color = inactiveColor;
        }
    }

    private void UpdateStanceIcons()
    {
        if (playerStanceScript == null) return;

        int currentStance = playerStanceScript.currentStance;

        switch (currentStance)
        {
            case 0: // Single Attack stance
                SetIconVisibility(pileDriverActive: true, aoeActive: true);
                Debug.Log("StanceIconManager: Switched to Single Attack stance - PileDriver icon active");
                break;

            case 1: // Area of Effect stance
                SetIconVisibility(pileDriverActive: true, aoeActive: true);
                Debug.Log("StanceIconManager: Switched to AOE stance - AOE icon active");
                break;

            default:
                Debug.LogWarning($"StanceIconManager: Unknown stance index {currentStance}");
                break;
        }
    }

    private void SetIconVisibility(bool pileDriverActive, bool aoeActive)
    {
        // Keep both icons visible, color animation will show which is active
        if (pileDriverIcon != null)
            pileDriverIcon.SetActive(pileDriverActive);

        if (aoeIcon != null)
            aoeIcon.SetActive(aoeActive);
    }

    // Public method to manually refresh icons (useful for initialization or external calls)
    public void RefreshIcons()
    {
        UpdateStanceIcons();
    }

    // Public method to set icon references at runtime if needed
    public void SetIconReferences(GameObject pileDriver, GameObject aoe)
    {
        pileDriverIcon = pileDriver;
        aoeIcon = aoe;
        
        // Update Image component references
        pileDriverImage = pileDriverIcon != null ? pileDriverIcon.GetComponent<Image>() : null;
        aoeImage = aoeIcon != null ? aoeIcon.GetComponent<Image>() : null;
        
        UpdateStanceIcons();
    }
}