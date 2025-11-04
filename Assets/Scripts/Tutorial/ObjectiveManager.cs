using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;

/// <summary>
/// Manages tutorial objectives and notices dynamically.
/// Singleton pattern - persists across scenes if needed.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField, Tooltip("TextMeshPro component for OBJECTIVE display")]
    private TextMeshProUGUI objectiveTextUI;
    
    [SerializeField, Tooltip("TextMeshPro component for NOTICE display")]
    private TextMeshProUGUI noticeTextUI;

    [SerializeField, Tooltip("Parent GameObject containing Notice (Title, Text, Image, Sidebar) - will be hidden when tutorial completes")]
    private GameObject noticeParentObject;

    [Header("Current Tutorial State")]
    [SerializeField, Tooltip("Current objective text (e.g., 'Eliminate ALL ENEMIES')")]
    private string currentObjective = "Eliminate ALL ENEMIES";
    
    [SerializeField, Tooltip("Current notice/tutorial text (e.g., 'Use X Button to Light Attack!')")]
    private string currentNotice = "";

    [Header("Animation Settings")]
    [SerializeField] private bool enableFadeAnimation = true;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Events")]
    public UnityEvent<string> OnObjectiveChanged;
    public UnityEvent<string> OnNoticeChanged;
    public UnityEvent OnObjectiveComplete; // Fired when any objective completes

    [Header("Sound")]
    [SerializeField, Tooltip("Sound played when objective/notice changes")]
    private AudioClip objectiveChangeSFX;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;

    [Header("Player Action Tracking")]
    [SerializeField] private int lightAttacksPerformed = 0;
    [SerializeField] private int heavyAttacksPerformed = 0;
    [SerializeField] private int stanceChangesPerformed = 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if you want it to persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Initialize with starting objective/notice
        UpdateObjective(currentObjective, false);
        UpdateNotice(currentNotice, false);
    }

    /// <summary>
    /// Updates the objective text and notifies listeners.
    /// </summary>
    public void UpdateObjective(string newObjective, bool playSound = true)
    {
        if (currentObjective == newObjective) return;

        currentObjective = newObjective;
        OnObjectiveChanged?.Invoke(newObjective);
        
        // Update UI directly
        if (objectiveTextUI != null)
        {
            objectiveTextUI.text = newObjective;
            if (enableFadeAnimation && !string.IsNullOrEmpty(newObjective))
            {
                StartCoroutine(FadeInText(objectiveTextUI));
            }
        }
        
        Debug.Log($"📋 [ObjectiveManager] Objective Updated: {newObjective}");
        
        if (playSound)
            PlayObjectiveSound();
    }

    /// <summary>
    /// Updates the notice/tutorial text and notifies listeners.
    /// </summary>
    public void UpdateNotice(string newNotice, bool playSound = true)
    {
        if (currentNotice == newNotice) return;

        currentNotice = newNotice;
        OnNoticeChanged?.Invoke(newNotice);
        
        // Update UI directly
        if (noticeTextUI != null)
        {
            noticeTextUI.text = newNotice;
            if (enableFadeAnimation && !string.IsNullOrEmpty(newNotice))
            {
                StartCoroutine(FadeInText(noticeTextUI));
            }
        }
        
        Debug.Log($"💡 [ObjectiveManager] Notice Updated: {newNotice}");
        
        if (playSound)
            PlayObjectiveSound();
    }

    /// <summary>
    /// Marks the current objective as complete and fires completion event.
    /// </summary>
    public void CompleteObjective()
    {
        Debug.Log($"✅ [ObjectiveManager] Objective Complete: {currentObjective}");
        OnObjectiveComplete?.Invoke();
    }

    /// <summary>
    /// Clears both objective and notice text.
    /// </summary>
    public void ClearAll()
    {
        UpdateObjective("", false);
        UpdateNotice("", false);
    }

    /// <summary>
    /// Hides the notice parent GameObject (for when tutorial is complete).
    /// </summary>
    public void HideNotice()
    {
        if (noticeParentObject != null)
        {
            noticeParentObject.SetActive(false);
            Debug.Log("🙈 [ObjectiveManager] Notice parent hidden (including Title, Text, Image, Sidebar)");
        }
        else
        {
            Debug.LogWarning("[ObjectiveManager] No noticeParentObject assigned! Assign the Notice GameObject in inspector.");
        }
    }

    /// <summary>
    /// Shows the notice parent GameObject (for when tutorial starts).
    /// </summary>
    public void ShowNotice()
    {
        if (noticeParentObject != null)
        {
            noticeParentObject.SetActive(true);
            Debug.Log("👀 [ObjectiveManager] Notice parent shown");
        }
        else
        {
            Debug.LogWarning("[ObjectiveManager] No noticeParentObject assigned! Assign the Notice GameObject in inspector.");
        }
    }

    // ========== Player Action Tracking ==========

    public void RegisterLightAttack()
    {
        lightAttacksPerformed++;
        Debug.Log($"⚔️ [ObjectiveManager] Light Attack #{lightAttacksPerformed}");
    }

    public void RegisterHeavyAttack()
    {
        heavyAttacksPerformed++;
        Debug.Log($"⚔️ [ObjectiveManager] Heavy Attack #{heavyAttacksPerformed}");
    }

    public void RegisterStanceChange()
    {
        stanceChangesPerformed++;
        Debug.Log($"🔄 [ObjectiveManager] Stance Change #{stanceChangesPerformed}");
    }

    public int GetLightAttackCount() => lightAttacksPerformed;
    public int GetHeavyAttackCount() => heavyAttacksPerformed;
    public int GetStanceChangeCount() => stanceChangesPerformed;

    public void ResetActionCounts()
    {
        lightAttacksPerformed = 0;
        heavyAttacksPerformed = 0;
        stanceChangesPerformed = 0;
    }

    // ========== Audio ==========

    private void PlayObjectiveSound()
    {
        if (objectiveChangeSFX == null)
        {
            Debug.LogWarning("[ObjectiveManager] No objective change SFX assigned!");
            return;
        }

        if (SoundManager.Instance != null && SoundManager.Instance.sfxSource != null)
        {
            SoundManager.Instance.sfxSource.PlayOneShot(objectiveChangeSFX, sfxVolume);
        }
        else
        {
            // Fallback: create temporary AudioSource
            AudioSource.PlayClipAtPoint(objectiveChangeSFX, Camera.main.transform.position, sfxVolume);
        }
    }

    // ========== Animation ==========

    private IEnumerator FadeInText(TextMeshProUGUI textComponent)
    {
        Color originalColor = textComponent.color;
        Color transparent = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        
        textComponent.color = transparent;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, originalColor.a, elapsedTime / fadeDuration);
            textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        textComponent.color = originalColor;
    }

    // ========== Public Getters ==========

    public string GetCurrentObjective() => currentObjective;
    public string GetCurrentNotice() => currentNotice;
}
