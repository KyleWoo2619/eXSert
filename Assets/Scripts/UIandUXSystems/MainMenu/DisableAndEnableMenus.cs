/**
    Written by Brandon Wahl

    This script handles enabling and disabling menus in the menu system. Because multiple different menus will occupy
    the same space on the screen, this script allows for easy switching between them.
**/
#if UNITY_EDITOR
using System.Runtime.InteropServices;
using Mono.Cecil;
using UnityEditor;
#endif


using UnityEngine;
using UnityEngine.InputSystem;

// Custom Attributes
public class SubSettingAttribute : PropertyAttribute { }
public class SettingsMenuAttribute : PropertyAttribute { }
public class NavigationBackButtonAttribute : PropertyAttribute { }
public class InGameMenuAttribute : PropertyAttribute { }
public class IsCanvasAttachedAttribute : PropertyAttribute { }
public class DisableAndEnableMenus : MonoBehaviour
{
    
    [Tooltip("SubSettingMenu refers to menus inside the settings (e.g. invertY or master volume)\n SettingsMenu refers to the general settings tabs like audio\n BackButton is for back buttons, duh.")] public enum MenuType { SubSettingMenu, SettingsMenu, SettingsBackButton, NavigationBackButton, IsCanvasAttached }
    [SerializeField] private MenuType menuType;
    [Tooltip("Leave blank if you don't intend to enable anything")][SerializeField] private GameObject enableThisGameobject = null;
    [Tooltip("Leave blank if you don't intend to disable anything")][SerializeField] private GameObject disableThisGameobject = null;
    [Tooltip("Assign the SubMenuManager (settings container) to this")][SerializeField] private SubMenuManager subMenuManager = null;
    
    [SubSetting]
    [Tooltip("Assign the SubMenu (e.g. invertY or master volume) you want to this")][SerializeField] private GameObject thisSubMenu = null;
    
    [SettingsMenu]
    [Tooltip("Assign the Settings Menu you want to this")][SerializeField] private GameObject thisSettingsMenu = null;

    [NavigationBackButton]
    [SerializeField] private FooterManager footer = null;
    [SerializeField] private bool isInGame;

    
    [SerializeField] private GameObject masterMenu;

    [IsCanvasAttached]
    [SerializeField] private GameObject pauseMenuUI;

    [IsCanvasAttached]
    [SerializeField] private GameObject navigationMenuUI;

    [IsCanvasAttached]
    [SerializeField] private InputActionReference _enterMenuAction;

    void Awake()
    {
        if (menuType == MenuType.NavigationBackButton)
        {
            if (footer == null)
            {
                // try to find a FooterManager on this GameObject before warning
                footer = GetComponent<FooterManager>();
                if (footer == null)
                {
                    Debug.LogWarning($"{nameof(DisableAndEnableMenus)}: footer is not assigned on '{name}'. Navigation back button may not function properly.");
                }
            }
        }
    }

    private void Update()
    {
       ActivateMenus();
    }

    //Enables or disables the assigned gameobjects based on the boolean selections
    public void OnClickSetActiveTrue()
    {
        if (menuType != MenuType.SettingsBackButton && subMenuManager == null)
        {
            Debug.LogWarning($"{nameof(DisableAndEnableMenus)}: subMenuManager is not assigned on '{name}'. Enabling target if present.");
            SafeSetActive(enableThisGameobject, true);
            return;
        }
        //If sub setting menu is selected this part of the function will run
        if (menuType == MenuType.SubSettingMenu)
        {
            // If the last activated sub menu is not empty, it becomes inactive
            if (subMenuManager.lastActivatedSubMenu != null)
            {
                subMenuManager.lastActivatedSubMenu.SetActive(false);
                
            }
            // The current sub menu is set to this one
            subMenuManager.lastActivatedSubMenu = this.thisSubMenu;
            
            //Enable the assigned gameobject
            SafeSetActive(enableThisGameobject, true);
        }
        //If settings menu is selected this part of the function will run
        else if (menuType == MenuType.SettingsMenu)
        {
            // If the last activated settings menu is not empty, it becomes inactive
            if (subMenuManager.lastActivatedSettingsMenu != null)
            {
                subMenuManager.lastActivatedSettingsMenu.SetActive(false);
            }

            // The current settings menu is set to this one
            subMenuManager.currentSettingsMenu = this.thisSettingsMenu;
            SafeSetActive(enableThisGameobject, true);

            // The last activated settings menu is set to this one
            subMenuManager.lastActivatedSettingsMenu = this.thisSettingsMenu;
        }

        subMenuManager.isOnSettingsMenu = false;
    }

    // Disables or enables the assigned gameobjects based on the boolean selections; mainly dealt with back button functionality
    public void OnClickSetActiveFalse()
    {
        // Guard: if this isn't the settings back flow and subMenuManager is missing, bail out
        if (menuType != MenuType.SettingsBackButton && subMenuManager == null)
        {
            Debug.LogWarning($"{nameof(DisableAndEnableMenus)}: subMenuManager is not assigned on '{name}'. Cannot change settings/main menu.");
            return;
        }
        //If back button and the player is not on settings menu this part of the function will run
        if (menuType == MenuType.SettingsBackButton && !isInGame)
        {
            if (subMenuManager == null)
            {
                Debug.LogWarning($"{nameof(DisableAndEnableMenus)}: subMenuManager is not assigned on '{name}'. Nothing to go back to.");
                return;
            }

            // Disable the current settings menu and mark that the player is now on the settings menu
            disableThisGameobject = subMenuManager.currentSettingsMenu;
            SafeSetActive(disableThisGameobject, false);
            subMenuManager.isOnSettingsMenu = true;
        }
        if (menuType == MenuType.SettingsBackButton && isInGame)
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
            }
            else
            {
                Debug.LogWarning($"{nameof(DisableAndEnableMenus)}: pauseMenuUI is not assigned on '{name}'. Cannot return to pause menu.");
            }
        }
        else
        {
            disableThisGameobject = subMenuManager.gameObject;
            SafeSetActive(disableThisGameobject, false);

            Debug.Log("Enabled Main Menu UI");
            enableThisGameobject = subMenuManager.mainMenu;
            SafeSetActive(enableThisGameobject, true);
        
            if (subMenuManager != null)
                subMenuManager.isOnSettingsMenu = false;
        }

        if (menuType == MenuType.NavigationBackButton)
        {
            HandleNavigationBack();
        }
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

    private void ActivateMenus()
    {
        if(_enterMenuAction != null && _enterMenuAction.action.triggered){
            if(navigationMenuUI.activeSelf ||pauseMenuUI.activeSelf)
            {
                var child = masterMenu.transform.GetChild(0);
                SafeSetActive(child.gameObject, true);
            }
        }
    }

    // Centralized navigation-back handling for the footer to avoid duplicated code
    public void HandleNavigationBack()
    {
        
        if (footer == null)
        {
            Debug.LogWarning($"{nameof(DisableAndEnableMenus)}: footer is not assigned on '{name}'. Cannot perform navigation back.");
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

        if(IsActive(pauseMenuUI))
        {
            var child = masterMenu.transform.GetChild(0);
            SafeSetActive(child.gameObject, false);
            return;
        }

        if(IsActive(footer.mainNavigationMenuHolderUI))
        {
            var child = masterMenu.transform.GetChild(0);
            SafeSetActive(child.gameObject, false);
            return;
        }

        // if nothing matched, ensure main navigation is visible
        SafeSetActive(footer.mainNavigationMenuHolderUI, true);
        SafeSetActive(footer.overlayUI, false);
    }

// Custom Property Drawers

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SubSettingAttribute))]
public class SubSettingDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (DrawerHelpers.ShouldShowForMenuType(property, 0)) // 0 = SubSettingMenu
            EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return DrawerHelpers.GetHeightForMenuType(property, label, 0);
    }
}

[CustomPropertyDrawer(typeof(SettingsMenuAttribute))]
public class SettingsMenuDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (DrawerHelpers.ShouldShowForMenuType(property, 1)) // 1 = SettingsMenu
            EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return DrawerHelpers.GetHeightForMenuType(property, label, 1);
    }
}

[CustomPropertyDrawer(typeof(NavigationBackButtonAttribute))]
public class NavigationBackButtonDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (DrawerHelpers.ShouldShowForMenuType(property, 3)) // 3 = NavigationBackButton
            EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return DrawerHelpers.GetHeightForMenuType(property, label, 3);
    }
}

[CustomPropertyDrawer(typeof(InGameMenuAttribute))]
public class InGameMenuDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var isInGameField = property.serializedObject.FindProperty("isInGame");
        if (isInGameField != null && isInGameField.boolValue)
            EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var isInGameField = property.serializedObject.FindProperty("isInGame");
        if (isInGameField != null && isInGameField.boolValue)
            return EditorGUI.GetPropertyHeight(property, label, true);
        return 0;
    }
}

[CustomPropertyDrawer(typeof(IsCanvasAttachedAttribute))]
public class IsCanvasAttachedDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (DrawerHelpers.ShouldShowForMenuType(property, 4)) // 4 = IsCanvasAttached
            EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return DrawerHelpers.GetHeightForMenuType(property, label, 4);
    }
}


// Helper utilities to reduce duplicate drawer logic
internal static class DrawerHelpers
{
    public static bool ShouldShowForMenuType(SerializedProperty property, int expectedEnumIndex)
    {
        if (property == null) return false;
        var menuTypeField = property.serializedObject.FindProperty("menuType");
        return menuTypeField != null && menuTypeField.enumValueIndex == expectedEnumIndex;
    }

    public static float GetHeightForMenuType(SerializedProperty property, GUIContent label, int expectedEnumIndex)
    {
        return ShouldShowForMenuType(property, expectedEnumIndex) ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
    }
}


#endif

}