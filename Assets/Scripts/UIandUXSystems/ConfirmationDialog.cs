using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Universal confirmation dialog system.
/// Shows "Are you sure?" panels with Yes/No options.
/// A button = Cancel (No), B button = Confirm (Yes)
/// </summary>
public class ConfirmationDialog : MonoBehaviour
{
    // Global modal tracking so other systems (e.g., PauseManager) can ignore inputs while a dialog is open
    private static int s_openDialogs = 0;
    public static bool AnyOpen => s_openDialogs > 0;

    [Header("Dialog Panel")]
    [SerializeField, Tooltip("The dialog panel GameObject to show/hide")]
    private GameObject dialogPanel;

    [Header("Background Blocking")]
    [SerializeField, Tooltip("GameObject(s) to disable interaction when dialog opens (e.g., PauseMenuButtons)")]
    private GameObject[] objectsToDisableWhenOpen;

    [SerializeField, Tooltip("The first button to select when dialog opens (usually 'No' button)")]
    private Button defaultSelectedButton;

    [SerializeField, Tooltip("The button to select when dialog closes (e.g., the button that opened this dialog)")]
    private Button buttonToSelectOnClose;

    [Header("Input Actions")]
    [SerializeField, Tooltip("Select/Cancel button (A button) - closes dialog")]
    private InputActionReference selectActionReference;
    
    [SerializeField, Tooltip("Confirm button (B button) - executes the action")]
    private InputActionReference confirmActionReference;

    [Header("Events")]
    [Tooltip("Called when player confirms (presses B/Yes)")]
    public UnityEvent OnConfirm;
    
    [Tooltip("Called when player cancels (presses A/No or closes dialog)")]
    public UnityEvent OnCancel;

    private bool isDialogOpen = false;
    private float dialogOpenTime = 0f;
    private const float INPUT_IGNORE_DURATION = 0.3f; // Ignore input for 0.3 seconds after opening

    [Header("Behavior")]
    [SerializeField, Tooltip("If ON: A cancels and B confirms (Xbox/PlayStation south=Cancel, east=Confirm). If OFF: A confirms and B cancels.")]
    private bool aCancels_bConfirms = true;

    [SerializeField, Tooltip("Additional cooldown after closing before background UI is re-enabled (prevents immediate re-trigger on held A/B)")]
    private float inputIgnoreAfterClose = 0.25f;

    private float lastCloseTime = -999f;
    private Coroutine reenableRoutine;

    private void OnEnable()
    {
        Debug.Log($"[ConfirmationDialog] OnEnable called on {gameObject.name}");
        
        // Subscribe to input actions with higher priority
        if (selectActionReference != null && selectActionReference.action != null)
        {
            selectActionReference.action.performed += OnSelectPressed;
            // Enable the action to ensure it's listening
            if (!selectActionReference.action.enabled)
                selectActionReference.action.Enable();
            
            Debug.Log($"[ConfirmationDialog] Select action subscribed: {selectActionReference.action.name}, Enabled: {selectActionReference.action.enabled}");
        }
        else
        {
            Debug.LogWarning($"[ConfirmationDialog] Select action reference is NULL or action is NULL on {gameObject.name}!");
        }
        
        if (confirmActionReference != null && confirmActionReference.action != null)
        {
            confirmActionReference.action.performed += OnConfirmPressed;
            // Enable the action to ensure it's listening
            if (!confirmActionReference.action.enabled)
                confirmActionReference.action.Enable();
            
            Debug.Log($"[ConfirmationDialog] Confirm action subscribed: {confirmActionReference.action.name}, Enabled: {confirmActionReference.action.enabled}");
        }
        else
        {
            Debug.LogWarning($"[ConfirmationDialog] Confirm action reference is NULL or action is NULL on {gameObject.name}!");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from input actions
        if (selectActionReference != null && selectActionReference.action != null)
            selectActionReference.action.performed -= OnSelectPressed;
        
        if (confirmActionReference != null && confirmActionReference.action != null)
            confirmActionReference.action.performed -= OnConfirmPressed;
    }

    private void Update()
    {
        // Additional safety check - if dialog is open and Select is pressed, close it
        // This catches cases where the event might not fire properly
        if (isDialogOpen)
        {
            float timeSinceOpen = Time.unscaledTime - dialogOpenTime;
            
            // Only process input after the ignore duration
            if (timeSinceOpen >= INPUT_IGNORE_DURATION)
            {
                // Manual input check as fallback
                if (selectActionReference != null && selectActionReference.action != null)
                {
                    if (selectActionReference.action.WasPerformedThisFrame())
                    {
                        Debug.Log($"[ConfirmationDialog] ⚠️ Select detected in Update (backup detection) - Closing dialog");
                        CloseDialog();
                        return;
                    }
                }
                
                // Alternative: Check devices using the new Input System (works when old Input is disabled)
                var kb = Keyboard.current;
                var gp = Gamepad.current;

                bool south = gp != null && gp.buttonSouth.wasPressedThisFrame;
                bool east  = gp != null && gp.buttonEast.wasPressedThisFrame;
                bool kbCancel = kb != null && kb.escapeKey.wasPressedThisFrame;
                bool kbConfirm = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame);

                if (aCancels_bConfirms)
                {
                    // South (A) cancels, East (B) confirms
                    if (kbCancel || south)
                    {
                        Debug.Log("[ConfirmationDialog] Device input (A south / Esc) - Cancel");
                        CloseDialog();
                        return;
                    }
                    if (kbConfirm || east)
                    {
                        Debug.Log("[ConfirmationDialog] Device input (B east / Enter/Space) - Confirm");
                        PerformConfirm();
                        return;
                    }
                }
                else
                {
                    // South (A) confirms, East (B) cancels
                    if (kbConfirm || south)
                    {
                        Debug.Log("[ConfirmationDialog] Device input (A south / Enter/Space) - Confirm");
                        PerformConfirm();
                        return;
                    }
                    if (kbCancel || east)
                    {
                        Debug.Log("[ConfirmationDialog] Device input (B east / Esc) - Cancel");
                        CloseDialog();
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Opens the confirmation dialog
    /// </summary>
    public void OpenDialog()
    {
        Debug.Log($"[ConfirmationDialog] 🚪 OpenDialog called on {gameObject.name}");
        
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
            isDialogOpen = true;
            dialogOpenTime = Time.unscaledTime; // Use unscaled time to work during pause
            s_openDialogs = Mathf.Max(0, s_openDialogs) + 1;
            
            Debug.Log($"[ConfirmationDialog] ✅ Dialog opened, isDialogOpen = {isDialogOpen}, ignoring input for {INPUT_IGNORE_DURATION}s");
            
            // Disable background interaction
            DisableBackgroundObjects(true);
            
            // Select default button for controller navigation (with delay for EventSystem)
            StartCoroutine(SelectDefaultButtonDelayed());
        }
        else
        {
            Debug.LogError($"[ConfirmationDialog] ❌ Dialog panel not assigned on {gameObject.name}!");
        }
    }
    
    /// <summary>
    /// Selects the default button after a short delay to ensure EventSystem is ready
    /// </summary>
    private System.Collections.IEnumerator SelectDefaultButtonDelayed()
    {
        // Wait one frame for UI to be fully active
        yield return null;
        
        if (defaultSelectedButton != null)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null); // Clear first
                yield return null; // Wait another frame
                EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
                Debug.Log($"[ConfirmationDialog] 🎮 Default button selected: {defaultSelectedButton.name}");
            }
            else
            {
                Debug.LogWarning($"[ConfirmationDialog] ⚠️ EventSystem is NULL! Controller navigation won't work.");
            }
        }
        else
        {
            Debug.LogWarning($"[ConfirmationDialog] ⚠️ Default selected button not assigned!");
        }
    }

    /// <summary>
    /// Closes the confirmation dialog without confirming
    /// </summary>
    public void CloseDialog()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
            isDialogOpen = false;
            if (s_openDialogs > 0) s_openDialogs--;
            lastCloseTime = Time.unscaledTime;

            // Re-enable background interaction after a short, safe delay (and after buttons are released)
            StartReenableAfterClose();
            
            Debug.Log($"[ConfirmationDialog] Dialog closed: {gameObject.name}");
        }
        
        // Trigger cancel event
        OnCancel?.Invoke();
    }

    /// <summary>
    /// Disables/enables background objects to prevent interaction during dialog
    /// </summary>
    private void DisableBackgroundObjects(bool disable)
    {
        if (objectsToDisableWhenOpen == null || objectsToDisableWhenOpen.Length == 0)
            return;

        foreach (var obj in objectsToDisableWhenOpen)
        {
            if (obj != null)
            {
                // Disable all Selectable components (Buttons, Toggles, etc.)
                var selectables = obj.GetComponentsInChildren<Selectable>(true);
                foreach (var selectable in selectables)
                {
                    selectable.interactable = !disable;
                }
                
                Debug.Log($"[ConfirmationDialog] Background object '{obj.name}' interaction: {!disable}");
            }
        }
    }

    /// <summary>
    /// Called when player presses A (Select/Cancel button)
    /// </summary>
    private void OnSelectPressed(InputAction.CallbackContext context)
    {
        Debug.Log($"[ConfirmationDialog] 🅰️ OnSelectPressed FIRED! isDialogOpen = {isDialogOpen}, Phase = {context.phase}");
        
        if (!isDialogOpen)
        {
            Debug.Log($"[ConfirmationDialog] ⚠️ Dialog not open, ignoring Select press");
            return;
        }

        // Ignore input immediately after opening (button still held from opening dialog)
        float timeSinceOpen = Time.unscaledTime - dialogOpenTime;
        if (timeSinceOpen < INPUT_IGNORE_DURATION)
        {
            Debug.Log($"[ConfirmationDialog] ⏳ Ignoring Select - too soon after opening ({timeSinceOpen:F2}s < {INPUT_IGNORE_DURATION}s)");
            return;
        }

        Debug.Log("[ConfirmationDialog] ✅ Select (A) pressed - Canceling dialog");
        
        // Prevent event from propagating to UI buttons
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        CloseDialog();
    }

    /// <summary>
    /// Called when player presses B (Confirm button)
    /// </summary>
    private void OnConfirmPressed(InputAction.CallbackContext context)
    {
        Debug.Log($"[ConfirmationDialog] 🅱️ OnConfirmPressed FIRED! isDialogOpen = {isDialogOpen}, Phase = {context.phase}");
        
        if (!isDialogOpen)
        {
            Debug.Log($"[ConfirmationDialog] ⚠️ Dialog not open, ignoring Confirm press");
            return;
        }

        // Ignore input immediately after opening (prevents accidental confirm)
        float timeSinceOpen = Time.unscaledTime - dialogOpenTime;
        if (timeSinceOpen < INPUT_IGNORE_DURATION)
        {
            Debug.Log($"[ConfirmationDialog] ⏳ Ignoring Confirm - too soon after opening ({timeSinceOpen:F2}s < {INPUT_IGNORE_DURATION}s)");
            return;
        }

        Debug.Log("[ConfirmationDialog] ✅ Confirm (B) pressed - Executing action");
        PerformConfirm();
    }

    /// <summary>
    /// Shared confirm flow used by B, Yes button, and device fallback
    /// </summary>
    private void PerformConfirm()
    {
        // Prevent event from propagating to UI buttons
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Close dialog first
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        isDialogOpen = false;
        if (s_openDialogs > 0) s_openDialogs--;
        lastCloseTime = Time.unscaledTime;

    // Re-enable background interaction after a safe delay
    StartReenableAfterClose();

        // Execute the confirmed action
        OnConfirm?.Invoke();
    }

    /// <summary>
    /// Starts or restarts the coroutine that re-enables background UI and restores selection
    /// after a short delay and after input buttons are released.
    /// </summary>
    private void StartReenableAfterClose()
    {
        if (reenableRoutine != null)
        {
            CoroutineRunner.Stop(reenableRoutine);
        }
        reenableRoutine = CoroutineRunner.Run(ReenableBackgroundAndSelectionAfterClose());
    }

    private System.Collections.IEnumerator ReenableBackgroundAndSelectionAfterClose()
    {
        // Ensure at least one frame passes with dialog hidden
        yield return null;

        float start = lastCloseTime;
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        // Wait until either duration elapsed OR relevant buttons are released
        while (Time.unscaledTime - start < inputIgnoreAfterClose)
        {
            bool anyPressed = false;
            if (kb != null)
            {
                anyPressed |= kb.escapeKey.isPressed || kb.enterKey.isPressed || kb.spaceKey.isPressed;
            }
            if (gp != null)
            {
                anyPressed |= gp.buttonSouth.isPressed || gp.buttonEast.isPressed;
            }

            if (!anyPressed)
            {
                // Buttons released: allow early exit
                break;
            }
            yield return null;
        }

        // Finally, re-enable background controls and restore selection
        DisableBackgroundObjects(false);

        if (buttonToSelectOnClose != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonToSelectOnClose.gameObject);
        }

        reenableRoutine = null;
    }

    /// <summary>
    /// Called when player clicks "No" button in UI
    /// </summary>
    public void OnNoButtonClicked()
    {
        Debug.Log("[ConfirmationDialog] No button clicked");
        CloseDialog();
    }

    /// <summary>
    /// Called when player clicks "Yes" button in UI
    /// </summary>
    public void OnYesButtonClicked()
    {
        Debug.Log("[ConfirmationDialog] Yes button clicked");
        PerformConfirm();
    }

    // Public getters
    public bool IsDialogOpen => isDialogOpen;
}
