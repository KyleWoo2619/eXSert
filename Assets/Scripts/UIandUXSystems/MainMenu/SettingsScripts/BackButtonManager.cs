/*
    Written originally by Brandon Wahl

    This script oringinated from the DisableAndEnableMenus script to handle the back button functionality

    CoPilot split the logic from the original script into this new script to better separate concerns.

*/
#if UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

public class InGameMenuAttribute : PropertyAttribute { }

public class BackButtonManager : MonoBehaviour
{
    [Tooltip("Assign the SubMenuManager (settings container) to this")] [SerializeField]
    private SubMenuManager subMenuManager = null;

    [SerializeField] private bool isInGame = false;

    [InGameMenu]
    [SerializeField] private GameObject pauseMenuUI = null;

    [InGameMenu]
    [SerializeField] private GameObject masterMenu = null;

    [Tooltip("Optional explicit enable/disable targets")]
    [SerializeField] private GameObject enableThisGameobject = null;
    [SerializeField] private GameObject disableThisGameobject = null;

    [Header("Footer (optional) - used for navigation back behavior")]
    [SerializeField] private FooterManager footer = null;

    [SerializeField] private InputActionReference _backAction;

    /// <summary>
    /// Primary back handler which mimics the previous DisableAndEnableMenus back logic.
    /// It handles both settings-back flows and navigation-back flows when a FooterManager is assigned.
    /// </summary>
    public void OnBackPressed()
    {
        // First try to handle settings-back (returns true if handled)
        if (HandleSettingsBack())
            return;

        // If settings-back wasn't applicable, fall back to navigation back behavior
        HandleNavigationBack();
    }

    private void Update()
    {
        if (_backAction != null && _backAction.action != null && _backAction.action.triggered)
        {
            OnBackPressed();
        }
    }

    /// <summary>
    /// Attempts to perform the settings back flow (either in-game or main-menu flow).
    /// Returns true if a settings back action was performed, false if not applicable.
    /// </summary>
    public bool HandleSettingsBack()
    {
        if (subMenuManager == null)
        {
            Debug.LogWarning($"{nameof(BackButtonManager)}: subMenuManager is not assigned on '{name}'. Cannot change settings/main menu.");
            return false;
        }

        // Back from a sub-settings view to the settings menu (non in-game)
        if (!isInGame)
        {
            // First: disable the current settings submenu if any
            disableThisGameobject = subMenuManager.currentSettingsMenu;
            SafeSetActive(disableThisGameobject, false);
            subMenuManager.isOnSettingsMenu = true;

            // Then: close the whole settings menu and re-open main menu
            disableThisGameobject = subMenuManager.settingsMenu;
            SafeSetActive(disableThisGameobject, false);

            Debug.Log("Enabled Main Menu UI");
            enableThisGameobject = subMenuManager.mainMenu;
            SafeSetActive(enableThisGameobject, true);

            if (subMenuManager != null)
                subMenuManager.isOnSettingsMenu = false;

            return true;
        }

        // In-game: close settings and re-open pause menu
        if (isInGame)
        {
            if (pauseMenuUI != null)
            {
                Debug.Log("Disabled Settings Menu UI");
                var parent = subMenuManager.gameObject.transform;
                disableThisGameobject = parent.gameObject;
                SafeSetActive(disableThisGameobject, false);

                Debug.Log("Enabled Pause Menu UI");
                var parentPause = pauseMenuUI.gameObject.transform;
                enableThisGameobject = parentPause.GetChild(0).gameObject;
                SafeSetActive(enableThisGameobject, true);

                return true;
            }
            else
            {
                Debug.LogWarning($"{nameof(BackButtonManager)}: pauseMenuUI is not assigned on '{name}'. Cannot return to pause menu.");
            }
        }

        return false;
    }

    // Reuse the navigation back logic from the original file so the footer behavior remains the same
    private void HandleNavigationBack()
    {
        if (footer == null)
        {
            Debug.LogWarning($"{nameof(BackButtonManager)}: footer is not assigned on '{name}'. Cannot perform navigation back.");
            return;
        }

        if (IsActive(footer.logHolderUI) && !IsActive(footer.IndividualLogUI))
        {
            SafeSetActive(footer.logHolderUI, false);
            SafeSetActive(footer.mainNavigationMenuHolderUI, true);
            return;
        }

        if (IsActive(footer.diaryHolderUI) && !IsActive(footer.IndividualDiaryUI))
        {
            SafeSetActive(footer.diaryHolderUI, false);
            SafeSetActive(footer.mainNavigationMenuHolderUI, true);
            return;
        }

        if (IsActive(footer.IndividualDiaryUI))
        {
            SafeSetActive(footer.IndividualDiaryUI, false);
            SafeSetActive(footer.overlayUI, false);
            return;
        }

        if (IsActive(footer.IndividualLogUI))
        {
            SafeSetActive(footer.IndividualLogUI, false);
            SafeSetActive(footer.overlayUI, false);
            return;
        }

        if (IsActive(pauseMenuUI))
        {
            var child = masterMenu.transform.GetChild(0);
            SafeSetActive(child.gameObject, false);
            return;
        }

        if (IsActive(footer.mainNavigationMenuHolderUI))
        {
            var child = masterMenu.transform.GetChild(0);
            SafeSetActive(child.gameObject, false);
            return;
        }

        // if nothing matched, ensure main navigation is visible
        SafeSetActive(footer.mainNavigationMenuHolderUI, true);
        SafeSetActive(footer.overlayUI, false);
    }

    // Safely sets the active state of a GameObject, checking for null and current state
    private void SafeSetActive(GameObject go, bool state)
    {
        if (go == null)
            return;
        if (go.activeSelf == state)
            return;
        go.SetActive(state);
    }

    // Returns true if the GameObject is non-null and currently active
    private bool IsActive(GameObject go)
    {
        return go != null && go.activeSelf;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(InGameMenuAttribute))]

public class InGameMenuDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var isInGameProp = property.serializedObject.FindProperty("isInGame");

        EditorGUI.BeginProperty(position, label, property);

        if (isInGameProp != null && isInGameProp.propertyType == SerializedPropertyType.Boolean && isInGameProp.boolValue)
        {
            // When isInGame is true, draw the full property as normal
            EditorGUI.PropertyField(position, property, label);
        }
        else
        {
            // When hidden, draw a subtle disabled single-line label so inspector doesn't jump sizes
            using (new UnityEditor.EditorGUI.DisabledScope(true))
            {
                var rect = new Rect(position.x, position.y, position.width, UnityEditor.EditorGUIUtility.singleLineHeight);
                UnityEditor.EditorGUI.LabelField(rect, label.text + " (hidden)");
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var isInGameProp = property.serializedObject.FindProperty("isInGame");
        if (isInGameProp != null && isInGameProp.propertyType == SerializedPropertyType.Boolean && isInGameProp.boolValue)
        {
            // Normal property height
            return base.GetPropertyHeight(property, label);
        }

        // When hidden, reserve single-line height to avoid big layout jumps
        return UnityEditor.EditorGUIUtility.singleLineHeight + UnityEditor.EditorGUIUtility.standardVerticalSpacing;
    }
}

#endif


