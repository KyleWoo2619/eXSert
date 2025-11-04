using UnityEngine;
using TMPro;

/// <summary>
/// Updates UI text elements based on ObjectiveManager events.
/// Attach to a Canvas GameObject with TextMeshPro components.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField, Tooltip("TextMeshPro for 'OBJECTIVE' display")]
    private TextMeshProUGUI objectiveText;
    
    [SerializeField, Tooltip("TextMeshPro for 'NOTICE' display")]
    private TextMeshProUGUI noticeText;

    [Header("Animation Settings")]
    [SerializeField] private bool enableFadeAnimation = true;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        // Subscribe to ObjectiveManager events
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveChanged.AddListener(UpdateObjectiveText);
            ObjectiveManager.Instance.OnNoticeChanged.AddListener(UpdateNoticeText);
            
            // Initialize with current values
            UpdateObjectiveText(ObjectiveManager.Instance.GetCurrentObjective());
            UpdateNoticeText(ObjectiveManager.Instance.GetCurrentNotice());
        }
        else
        {
            Debug.LogError("[TutorialUI] ObjectiveManager not found in scene!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.OnObjectiveChanged.RemoveListener(UpdateObjectiveText);
            ObjectiveManager.Instance.OnNoticeChanged.RemoveListener(UpdateNoticeText);
        }
    }

    private void UpdateObjectiveText(string newText)
    {
        if (objectiveText == null)
        {
            Debug.LogWarning("[TutorialUI] Objective Text reference is null!");
            return;
        }

        objectiveText.text = newText;
        
        if (enableFadeAnimation && !string.IsNullOrEmpty(newText))
        {
            StartCoroutine(FadeInText(objectiveText));
        }
        
        Debug.Log($"🖥️ [TutorialUI] Objective Text Updated: '{newText}'");
    }

    private void UpdateNoticeText(string newText)
    {
        if (noticeText == null)
        {
            Debug.LogWarning("[TutorialUI] Notice Text reference is null!");
            return;
        }

        noticeText.text = newText;
        
        if (enableFadeAnimation && !string.IsNullOrEmpty(newText))
        {
            StartCoroutine(FadeInText(noticeText));
        }
        
        Debug.Log($"🖥️ [TutorialUI] Notice Text Updated: '{newText}'");
    }

    private System.Collections.IEnumerator FadeInText(TextMeshProUGUI textComponent)
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

    // Public methods for manual control if needed
    public void SetObjectiveText(string text) => UpdateObjectiveText(text);
    public void SetNoticeText(string text) => UpdateNoticeText(text);
    public void ClearAllText()
    {
        UpdateObjectiveText("");
        UpdateNoticeText("");
    }
}
