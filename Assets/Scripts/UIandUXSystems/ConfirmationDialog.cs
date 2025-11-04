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
            if (selectActionReference != null && selectActionReference.action != null)
            {
                if (selectActionReference.action.WasPerformedThisFrame())
                {
                    Debug.Log($"[ConfirmationDialog] ⚠️ Select detected in Update (backup detection) - Closing dialog");
                    CloseDialog();
                }
                
                // Debug: Check if action is being pressed but not firing
                if (selectActionReference.action.IsPressed())
                {
                    Debug.Log($"[ConfirmationDialog] 🔍 Select action is being pressed (value: {selectActionReference.action.ReadValue<float>()})");
                }
            }
            
            if (confirmActionReference != null && confirmActionReference.action != null)
            {
                // Debug: Check if confirm action is being pressed
                if (confirmActionReference.action.IsPressed())
                {
                    Debug.Log($"[ConfirmationDialog] 🔍 Confirm action is being pressed (value: {confirmActionReference.action.ReadValue<float>()})");
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
            
            Debug.Log($"[ConfirmationDialog] ✅ Dialog opened, isDialogOpen = {isDialogOpen}");
            
            // Disable background interaction
            DisableBackgroundObjects(true);
            
            // Select default button for controller navigation
            if (defaultSelectedButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
                Debug.Log($"[ConfirmationDialog] 🎮 Default button selected: {defaultSelectedButton.name}");
            }
            else
            {
                Debug.LogWarning($"[ConfirmationDialog] ⚠️ Default selected button is NULL or EventSystem is NULL!");
            }
        }
        else
        {
            Debug.LogError($"[ConfirmationDialog] ❌ Dialog panel not assigned on {gameObject.name}!");
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
            
            // Re-enable background interaction
            DisableBackgroundObjects(false);
            
            // Re-select button for controller navigation
            if (buttonToSelectOnClose != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(buttonToSelectOnClose.gameObject);
            }
            
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

        Debug.Log("[ConfirmationDialog] ✅ Confirm (B) pressed - Executing action");
        
        // Prevent event from propagating to UI buttons
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // Close dialog first
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        isDialogOpen = false;
        
        // Re-enable background interaction
        DisableBackgroundObjects(false);
        
        // Re-select button for controller navigation (optional for confirm, since action usually changes scene/state)
        if (buttonToSelectOnClose != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonToSelectOnClose.gameObject);
        }
        
        // Execute the confirmed action
        OnConfirm?.Invoke();
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
        
        // Close dialog first
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        isDialogOpen = false;
        
        // Re-enable background interaction
        DisableBackgroundObjects(false);
        
        // Re-select button for controller navigation (optional for confirm)
        if (buttonToSelectOnClose != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(buttonToSelectOnClose.gameObject);
        }
        
        // Execute the confirmed action
        OnConfirm?.Invoke();
    }

    // Public getters
    public bool IsDialogOpen => isDialogOpen;
}
