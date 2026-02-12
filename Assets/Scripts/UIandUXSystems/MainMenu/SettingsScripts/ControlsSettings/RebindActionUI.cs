/*
    Script provided by Unity that handles the core functionality of the rebinding settings. This script takes in the input asset assigned,
    and shows the respective name and button bind. By clicking on the button this script is attached to, it will allow the player to select
    a new key binding.
*/

using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

////TODO: localization support

////TODO: deal with composites that have parts bound in different control schemes

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A reusable component with a self-contained UI for rebinding a single action.
    /// </summary>
    public class RebindActionUI : MonoBehaviour
    {
        /// <summary>
        /// Reference to the action that is to be rebound.
        /// </summary>
        public InputActionReference actionReference
        {
            get => m_Action;
            set
            {
                m_Action = value;
                UpdateActionLabel();
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// ID (in string form) of the binding that is to be rebound on the action.
        /// </summary>
        /// <seealso cref="InputBinding.id"/>
        public string bindingId
        {
            get => m_BindingId;
            set
            {
                m_BindingId = value;
                UpdateBindingDisplay();
            }
        }

        public InputBinding.DisplayStringOptions displayStringOptions
        {
            get => m_DisplayStringOptions;
            set
            {
                m_DisplayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Text component that receives the name of the action. Optional.
        /// </summary>
        public TMPro.TextMeshProUGUI actionLabel
        {
            get => m_ActionLabel;
            set
            {
                m_ActionLabel = value;
                UpdateActionLabel();
            }
        }

        /// <summary>
        /// Text component that receives the display string of the binding. Can be <c>null</c> in which
        /// case the component entirely relies on <see cref="updateBindingUIEvent"/>.
        /// </summary>
        public TMPro.TextMeshProUGUI bindingText
        {
            get => m_BindingText;
            set
            {
                m_BindingText = value;
                UpdateBindingDisplay();
            }
        }

        /// <summary>
        /// Optional text component that receives a text prompt when waiting for a control to be actuated.
        /// </summary>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindOverlay"/>
        public TMPro.TextMeshProUGUI rebindPrompt
        {
            get => m_RebindText;
            set => m_RebindText = value;
        }

        /// <summary>
        /// Optional UI that is activated when an interactive rebind is started and deactivated when the rebind
        /// is finished. This is normally used to display an overlay over the current UI while the system is
        /// waiting for a control to be actuated.
        /// </summary>
        /// <remarks>
        /// If neither <see cref="rebindPrompt"/> nor <c>rebindOverlay</c> is set, the component will temporarily
        /// replaced the <see cref="bindingText"/> (if not <c>null</c>) with <c>"Waiting..."</c>.
        /// </remarks>
        /// <seealso cref="startRebindEvent"/>
        /// <seealso cref="rebindPrompt"/>
        public GameObject rebindOverlay
        {
            get => m_RebindOverlay;
            set => m_RebindOverlay = value;
        }

        /// <summary>
        /// Event that is triggered every time the UI updates to reflect the current binding.
        /// This can be used to tie custom visualizations to bindings.
        /// </summary>
        public UpdateBindingUIEvent updateBindingUIEvent
        {
            get
            {
                if (m_UpdateBindingUIEvent == null)
                    m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
                return m_UpdateBindingUIEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind is started on the action.
        /// </summary>
        public InteractiveRebindEvent startRebindEvent
        {
            get
            {
                if (m_RebindStartEvent == null)
                    m_RebindStartEvent = new InteractiveRebindEvent();
                return m_RebindStartEvent;
            }
        }

        /// <summary>
        /// Event that is triggered when an interactive rebind has been completed or canceled.
        /// </summary>
        public InteractiveRebindEvent stopRebindEvent
        {
            get
            {
                if (m_RebindStopEvent == null)
                    m_RebindStopEvent = new InteractiveRebindEvent();
                return m_RebindStopEvent;
            }
        }

        /// <summary>
        /// When an interactive rebind is in progress, this is the rebind operation controller.
        /// Otherwise, it is <c>null</c>.
        /// </summary>
        public InputActionRebindingExtensions.RebindingOperation ongoingRebind => m_RebindOperation;

        /// <summary>
        /// Return the action and binding index for the binding that is targeted by the component
        /// according to
        /// </summary>
        /// <param name="action"></param>
        /// <param name="bindingIndex"></param>
        /// <returns></returns>
        public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
        {
            bindingIndex = -1;

            action = m_Action?.action;
            if (action == null)
                return false;

            if (string.IsNullOrEmpty(m_BindingId))
                return false;

            // Look up binding index.
            var bindingId = new Guid(m_BindingId);
            bindingIndex = action.bindings.IndexOf(x => x.id == bindingId);
            if (bindingIndex == -1)
            {
                Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Trigger a refresh of the currently displayed binding.
        /// </summary>
        public void UpdateBindingDisplay()
        {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            // Check if binding text override is enabled
            if (overrideBindingText)
            {
                displayString = bindingTextString;
                Debug.Log($"[RebindActionUI] UpdateBindingDisplay: Using override text '{displayString}' on {gameObject.name}");
            }
            else
            {
                // Get display string from action.
                var action = m_Action?.action;
                if (action != null)
                {
                    var bindingIndex = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
                    if (bindingIndex != -1)
                    {
                        displayString = action.GetBindingDisplayString(bindingIndex, out deviceLayoutName, out controlPath, displayStringOptions);
                        Debug.Log($"[RebindActionUI] UpdateBindingDisplay: Action '{action.name}' binding updated to '{displayString}' on {gameObject.name}");
                    }
                }
            }

            // Set on label (if any).
            if (m_BindingText != null)
                m_BindingText.text = displayString;

            // Give listeners a chance to configure UI in response.
            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        /// <summary>
        /// Remove currently applied binding overrides.
        /// </summary>
       
    public void ResetToDefault()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
            {
                Debug.LogWarning($"[RebindActionUI] Failed to resolve action and binding for reset on {gameObject.name}");
                return;
            }

            Debug.Log($"[RebindActionUI] Resetting binding to default for action '{action.name}' on {gameObject.name}");

            ResetBinding(action, bindingIndex);

            if (action.bindings[bindingIndex].isComposite)
            {
                Debug.Log($"[RebindActionUI] Resetting composite binding and all its parts");
                // It's a composite. Remove overrides from part bindings.
                for (var i = bindingIndex + 1; i < action.bindings.Count && action.bindings[i].isPartOfComposite; ++i)
                    action.RemoveBindingOverride(i);
            }
            else
            {
                action.RemoveBindingOverride(bindingIndex);
            }

            UpdateBindingDisplay();
            Debug.Log($"[RebindActionUI] Reset complete. New path: {action.bindings[bindingIndex].effectivePath}");
        }

       private void ResetBinding(InputAction action, int bindingIndex)
        {
            InputBinding newBinding = action.bindings[bindingIndex];
            string oldOverridePath = newBinding.overridePath;

            action.RemoveBindingOverride(bindingIndex);
            int currentIndex = -1;

            foreach (InputAction otherAction in action.actionMap.actions)
            {
                currentIndex++;
                InputBinding currentBinding = action.actionMap.bindings[currentIndex];

                if (otherAction == action)
                {
                    if (newBinding.isPartOfComposite)
                    {
                        if (currentBinding.overridePath == newBinding.path)
                        {
                            otherAction.ApplyBindingOverride(currentIndex, oldOverridePath);
                        }
                    }

                    else
                    {
                        continue;
                    }
                }

                for (int i = 0; i < otherAction.bindings.Count; i++)
                {
                    InputBinding binding = otherAction.bindings[i];
                    if (binding.overridePath == newBinding.path)
                    {
                        otherAction.ApplyBindingOverride(i, oldOverridePath);
                    }
                }
            }
        }

        /// <summary>
        /// Initiate an interactive rebind that lets the player actuate a control to choose a new binding
        /// for the action.
        /// </summary>
        public void StartInteractiveRebind()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
            {
                Debug.LogWarning($"[RebindActionUI] Failed to resolve action and binding for rebind on {gameObject.name}");
                return;
            }

            Debug.Log($"[RebindActionUI] Starting interactive rebind for action '{action.name}' on {gameObject.name}");

            // If the binding is a composite, we need to rebind each part in turn.
            if (action.bindings[bindingIndex].isComposite)
            {
                Debug.Log($"[RebindActionUI] Binding is composite, rebinding all parts");
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
            }
            else
            {
                PerformInteractiveRebind(action, bindingIndex);
            }
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            m_RebindOperation?.Cancel(); // Will null out m_RebindOperation.

            var binding = action.bindings[bindingIndex];
            var bindingName = binding.isPartOfComposite ? $"{binding.name} (part of composite)" : "simple binding";
            Debug.Log($"[RebindActionUI] Performing rebind for '{action.name}' [{bindingName}] at index {bindingIndex}. Current path: {binding.effectivePath}, Override path: {binding.overridePath}");

            void CleanUp()
            {
                m_RebindOperation?.Dispose();
                m_RebindOperation = null;

                Debug.Log($"[RebindActionUI] CleanUp: Enabling action map '{action.actionMap.name}' and action '{action.name}'");
                action.actionMap.Enable();
                m_UIInputActionMap?.Enable();
                Debug.Log($"[RebindActionUI] CleanUp complete. Action enabled: {action.enabled}, ActionMap enabled: {action.actionMap.enabled}");
            }

            //disable the action before use
            action.Disable();

            // An "InvalidOperationException: Cannot rebind action x while it is enabled" will
            // be thrown if rebinding is attempted on an action that is enabled.
            //
            // On top of disabling the target action while rebinding, it is recommended to
            // disable any actions (or action maps) that could interact with the rebinding UI
            // or gameplay - it would be undesirable for rebinding to cause the player
            // character to jump.
            //
            // In this example, we explicitly disable both the UI input action map and
            // the action map containing the target action.
            action.actionMap.Disable();
            m_UIInputActionMap?.Disable();

            // Configure the rebind.
            m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
                .OnCancel(
                    operation =>
                    {
                        Debug.Log($"[RebindActionUI] Rebind CANCELED for action '{action.name}' on {gameObject.name}");
                        action.Enable();
                        m_RebindStopEvent?.Invoke(this, operation);
                        if (m_RebindOverlay != null)
                            m_RebindOverlay.SetActive(false);
                        UpdateBindingDisplay();
                        CleanUp();
                    })
                .OnComplete(
                    operation =>
                    {
                            var newBinding = action.bindings[bindingIndex];
                            Debug.Log($"[RebindActionUI] Rebind COMPLETE for action '{action.name}' on {gameObject.name}. New path: {newBinding.effectivePath}");
                            
                            // Hide rebind overlay
                            if (m_RebindOverlay != null)
                                m_RebindOverlay.SetActive(false);
                                
                            m_RebindStopEvent?.Invoke(this, operation);

                            if (CheckDuplicateBindings(action, bindingIndex, allCompositeParts))
                            {
                                Debug.LogWarning($"[RebindActionUI] Duplicate binding detected, clearing old duplicate binding");
                                ClearDuplicateBinding(action, bindingIndex);
                            }

                            // Update display to show new binding
                            UpdateBindingDisplay();
                            
                            // Log the final binding state
                            var finalBinding = action.bindings[bindingIndex];
                            Debug.Log($"[RebindActionUI] Final binding state - Path: {finalBinding.path}, OverridePath: {finalBinding.overridePath}, EffectivePath: {finalBinding.effectivePath}");
                            
                            // Re-enable action and action maps
                            action.Enable();
                            CleanUp();
                            
                            Debug.Log($"[RebindActionUI] Rebind finalized! Action '{action.name}' is now bound to '{newBinding.effectivePath}'");

                        // If there's more composite parts we should bind, initiate a rebind
                        // for the next part.
                        if (allCompositeParts)
                        {
                            var nextBindingIndex = bindingIndex + 1;
                            if (nextBindingIndex < action.bindings.Count && action.bindings[nextBindingIndex].isPartOfComposite)
                            {
                                Debug.Log($"[RebindActionUI] Moving to next composite part at index {nextBindingIndex}");
                                PerformInteractiveRebind(action, nextBindingIndex, true);
                            }
                            else
                            {
                                Debug.Log($"[RebindActionUI] All composite parts rebound successfully");
                            }
                        }
                    });

            // If it's a part binding, show the name of the part in the UI.
            var partName = default(string);
            if (action.bindings[bindingIndex].isPartOfComposite)
                partName = $"Binding '{action.bindings[bindingIndex].name}'. ";

            // Bring up rebind overlay, if we have one.
            m_RebindOverlay?.SetActive(true);
            if (m_RebindText != null)
            {
                var text = !string.IsNullOrEmpty(m_RebindOperation.expectedControlType)
                    ? $"{partName}Waiting for {m_RebindOperation.expectedControlType} input..."
                    : $"{partName}Waiting for input...";
                m_RebindText.text = text;
            }

            // If we have no rebind overlay and no callback but we have a binding text label,
            // temporarily set the binding text label to "<Waiting>".
            if (m_RebindOverlay == null && m_RebindText == null && m_RebindStartEvent == null && m_BindingText != null)
                m_BindingText.text = "<Waiting...>";

            // Give listeners a chance to act on the rebind starting.
            m_RebindStartEvent?.Invoke(this, m_RebindOperation);

            m_RebindOperation.Start();
        }

        private bool CheckDuplicateBindings(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            InputBinding newBinding = action.bindings[bindingIndex];
            string newPath = newBinding.effectivePath;
            
            Debug.Log($"[RebindActionUI] Checking for duplicates: new path = '{newPath}', binding index = {bindingIndex}");

            // Check all bindings in the same action map for duplicates
            for (int i = 0; i < action.bindings.Count; i++)
            {
                // Skip checking against itself
                if (i == bindingIndex)
                    continue;

                InputBinding otherBinding = action.bindings[i];
                string otherPath = otherBinding.effectivePath;

                // Skip empty paths
                if (string.IsNullOrEmpty(newPath) || string.IsNullOrEmpty(otherPath))
                    continue;

                if (otherPath == newPath)
                {
                    Debug.LogWarning($"[RebindActionUI] Duplicate binding found within same action! Path '{newPath}' is already bound to binding at index {i}");
                    return true;
                }
            }

            // Also check bindings in other actions within the same action map
            if (action.actionMap != null)
            {
                int actionMapBindingIndex = -1;
                for (int i = 0; i < action.actionMap.bindings.Count; i++)
                {
                    if (action.actionMap.bindings[i].id == newBinding.id)
                    {
                        actionMapBindingIndex = i;
                        break;
                    }
                }

                for (int i = 0; i < action.actionMap.bindings.Count; i++)
                {
                    // Skip current binding
                    if (i == actionMapBindingIndex)
                        continue;

                    InputBinding otherBinding = action.actionMap.bindings[i];
                    string otherPath = otherBinding.effectivePath;

                    // Skip empty paths
                    if (string.IsNullOrEmpty(newPath) || string.IsNullOrEmpty(otherPath))
                        continue;

                    if (otherPath == newPath)
                    {
                        Debug.LogWarning($"[RebindActionUI] Duplicate binding found! Path '{newPath}' is already bound in action '{otherBinding.action}'");
                        return true;
                    }
                }
            }

            return false;
        }

        private void ClearDuplicateBinding(InputAction action, int bindingIndex)
        {
            InputBinding newBinding = action.bindings[bindingIndex];
            string newPath = newBinding.effectivePath;

            if (string.IsNullOrEmpty(newPath))
                return;

            // Find and clear bindings in other actions that use the same path
            if (action.actionMap != null)
            {
                for (int i = 0; i < action.actionMap.bindings.Count; i++)
                {
                    InputBinding otherBinding = action.actionMap.bindings[i];
                    
                    // Skip the binding we just changed
                    if (otherBinding.id == newBinding.id)
                        continue;

                    string otherPath = otherBinding.effectivePath;
                    
                    if (otherPath == newPath)
                    {
                        // Find the action that owns this binding
                        var otherAction = action.actionMap.FindAction(otherBinding.action);
                        if (otherAction != null)
                        {
                            // Find the binding index in that action
                            int otherBindingIndex = otherAction.bindings.IndexOf(x => x.id == otherBinding.id);
                            if (otherBindingIndex >= 0)
                            {
                                // Apply an empty binding override to show "-" in the UI
                                otherAction.ApplyBindingOverride(otherBindingIndex, "");
                                Debug.Log($"[RebindActionUI] Cleared duplicate binding from action '{otherAction.name}' at index {otherBindingIndex}");
                                
                                // Force update all RebindActionUI components that display this action
                                UpdateRebindUIForAction(otherAction);
                            }
                        }
                    }
                }
            }
        }
        private void UpdateRebindUIForAction(InputAction targetAction)
        {
            if (s_RebindActionUIs == null)
                return;

            for (int i = 0; i < s_RebindActionUIs.Count; i++)
            {
                var ui = s_RebindActionUIs[i];
                var referencedAction = ui.actionReference?.action;
                
                if (referencedAction == targetAction)
                {
                    // Disable and re-enable the action to force binding re-evaluation
                    bool wasEnabled = referencedAction.enabled;
                    referencedAction.Disable();
                    
                    ui.UpdateBindingDisplay();
                    Debug.Log($"[RebindActionUI] Updated binding display for UI component on {ui.gameObject.name}");
                    
                    if (wasEnabled)
                        referencedAction.Enable();
                }
            }
        }

        protected void OnEnable()
        {
            if (s_RebindActionUIs == null)
                s_RebindActionUIs = new List<RebindActionUI>();
            s_RebindActionUIs.Add(this);
            if (s_RebindActionUIs.Count == 1)
                InputSystem.onActionChange += OnActionChange;
            if (m_DefaultInputActions != null && m_UIInputActionMap == null)
                m_UIInputActionMap = m_DefaultInputActions.FindActionMap("UI");
            // Refresh the binding display when component is enabled
            // This ensures saved bindings show the correct text when the scene loads
            UpdateActionLabel();
            UpdateBindingDisplay();
            Debug.Log($"[RebindActionUI] OnEnable - Refreshed binding display for {gameObject.name}");

            // Hook up button click listeners
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(StartInteractiveRebind);
                Debug.Log($"[RebindActionUI] Button click listener attached to {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[RebindActionUI] No Button component found on {gameObject.name}. Button clicks will not trigger rebinding.");
            }
        }

        protected void OnDisable()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;

            // Remove button click listener
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(StartInteractiveRebind);
            }

            s_RebindActionUIs.Remove(this);
            if (s_RebindActionUIs.Count == 0)
            {
                s_RebindActionUIs = null;
                InputSystem.onActionChange -= OnActionChange;
            }
        }

        // When the action system re-resolves bindings, we want to update our UI in response. While this will
        // also trigger from changes we made ourselves, it ensures that we react to changes made elsewhere. If
        // the user changes keyboard layout, for example, we will get a BoundControlsChanged notification and
        // will update our UI to reflect the current keyboard layout.
        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            for (var i = 0; i < s_RebindActionUIs.Count; ++i)
            {
                var component = s_RebindActionUIs[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }

        [Tooltip("Reference to action that is to be rebound from the UI.")]
        [SerializeField]
        private InputActionReference m_Action;

        [SerializeField]
        private string m_BindingId;

        [SerializeField]
        private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [Tooltip("Text label that will receive the name of the action. Optional. Set to None to have the "
            + "rebind UI not show a label for the action.")]
        [SerializeField]
        private TMPro.TextMeshProUGUI m_ActionLabel;

        [Tooltip("Text label that will receive the current, formatted binding string.")]
        [SerializeField]
        private TMPro.TextMeshProUGUI m_BindingText;

        [Tooltip("Image component that displays the binding icon (controlled by GamepadIconsExample or similar icon handlers).")]
        [SerializeField]
        public Image m_BindingImage = null;

        [Tooltip("Optional UI that will be shown while a rebind is in progress.")]
        [SerializeField]
        private GameObject m_RebindOverlay;

        [Tooltip("Optional text label that will be updated with prompt for user input.")]
        [SerializeField]
        private TMPro.TextMeshProUGUI m_RebindText;

        [Tooltip("Optional bool field which allows you to override the action label with custom text")]
        public bool m_OverrideActionLabel;

        [Tooltip("What text should be displayed for the action label?")]
        [SerializeField]
        private string m_ActionLabelString;
        /// <summary>
        /// Whether to override the binding text with a custom string.
        /// </summary>
        public bool overrideBindingText
        {
            get => m_OverrideBindingText;
            set
            {
                m_OverrideBindingText = value;
                UpdateBindingDisplay();
                Debug.Log($"[RebindActionUI] Override binding text changed to {value} on {gameObject.name}");
            }
        }

        /// <summary>
        /// The custom text to display for the binding when override is enabled.
        /// </summary>
        public string bindingTextString
        {
            get => m_BindingTextString;
            set
            {
                m_BindingTextString = value;
                UpdateBindingDisplay();
                Debug.Log($"[RebindActionUI] Binding text string changed to '{value}' on {gameObject.name}");
            }
        }

        [Tooltip("Optional bool field which allows you to override the binding text with custom text")]
        [SerializeField]
        private bool m_OverrideBindingText;

        [Tooltip("What text should be displayed for the binding?")]
        [SerializeField]
        private string m_BindingTextString;
        

        [Tooltip("Optional reference to default input actions containing the UI action map. The UI action map is "
            + "disabled when rebinding is in progress.")]
        [SerializeField]
        private InputActionAsset m_DefaultInputActions;
        private InputActionMap m_UIInputActionMap;

        [Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying "
            + "bindings in custom ways, e.g. using images instead of text.")]
        [SerializeField]
        private UpdateBindingUIEvent m_UpdateBindingUIEvent;

        [Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, "
            + "to implement custom UI behavior while a rebind is in progress. It can also be used to further "
            + "customize the rebind.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStartEvent;

        [Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
        [SerializeField]
        private InteractiveRebindEvent m_RebindStopEvent;

        private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

        private static List<RebindActionUI> s_RebindActionUIs;

        // We want the label for the action name to update in edit mode, too, so
        // we kick that off from here.
        #if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }

        #endif

        private void UpdateActionLabel()
        {
            if (m_ActionLabel != null)
            {
                var action = m_Action?.action;

                if (m_OverrideActionLabel)
                {
                    m_ActionLabel.text = m_ActionLabelString;
                }
                else
                {
                    m_ActionLabel.text = action != null ? action.name : string.Empty;
                    m_ActionLabelString = String.Empty;
                }
            }
        }

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
        {
        }

        [Serializable]
        public class InteractiveRebindEvent : UnityEvent<RebindActionUI, InputActionRebindingExtensions.RebindingOperation>
        {
        }
    }
}
