/**
    Written by Brandon Wahl

    This script handles enabling and disabling menus in the menu system. Because multiple different menus will occupy
    the same space on the screen, this script allows for easy switching between them.
**/
#if UNITY_EDITOR
using UnityEditor;
#endif


using UnityEngine;

// Custom Attributes
public class SubSettingAttribute : PropertyAttribute { }
public class SettingsMenuAttribute : PropertyAttribute { }


public class DisableAndEnableMenus : MonoBehaviour
{
    
    [Tooltip("SubSettingMenu refers to menus inside the settings (e.g. invertY or master volume)\n SettingsMenu refers to the general settings tabs like audio\n BackButton is for back buttons, duh.")] public enum MenuType { SubSettingMenu, SettingsMenu, BackButton }
    [SerializeField] private MenuType menuType;
    [Tooltip("Leave blank if you don't intend to enable anything")][SerializeField] private GameObject enableThisGameobject = null;
    [Tooltip("Leave blank if you don't intend to disable anything")][SerializeField] private GameObject disableThisGameobject = null;
    [Tooltip("Assign the SubMenuManager (settings container) to this")][SerializeField] private SubMenuManager subMenuManager = null;
    
    [SubSetting]
    [Tooltip("Assign the SubMenu (e.g. invertY or master volume) you want to this")][SerializeField] private GameObject thisSubMenu = null;
    
    [SettingsMenu]
    [Tooltip("Assign the Settings Menu you want to this")][SerializeField] private GameObject thisSettingsMenu = null;

    //Enables or disables the assigned gameobjects based on the boolean selections
    public void OnClickSetActiveTrue()
    {
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
            enableThisGameobject.SetActive(true);
            Debug.Log(this.name);
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
            enableThisGameobject.SetActive(true);

            // The last activated settings menu is set to this one
            subMenuManager.lastActivatedSettingsMenu = this.thisSettingsMenu;
        }

        subMenuManager.isOnSettingsMenu = false;
    }

    // Disables or enables the assigned gameobjects based on the boolean selections; mainly dealt with back button functionality
    public void OnClickSetActiveFalse()
    {
        //If back button and the player is not on settings menu this part of the function will run
        if (menuType == MenuType.BackButton && !subMenuManager.isOnSettingsMenu)
        {
            // Disable the current settings menu and mark that the player is now on the settings menu
            disableThisGameobject = subMenuManager.currentSettingsMenu;
            disableThisGameobject.SetActive(false);
            subMenuManager.isOnSettingsMenu = true;
        }
        else
        {
            // Disable the settings menu and enable the main menu
            disableThisGameobject = subMenuManager.gameObject;
            disableThisGameobject.SetActive(false);

            enableThisGameobject = subMenuManager.mainMenu;
            enableThisGameobject.SetActive(true);

            subMenuManager.isOnSettingsMenu = false;
        }

    }

// Custom Property Drawers

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SubSettingAttribute))]
public class SubSettingDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as DisableAndEnableMenus;
        if (parent != null)
        {
            var menuTypeField = property.serializedObject.FindProperty("menuType");
            if (menuTypeField != null && menuTypeField.enumValueIndex == 0) // 0 = SubSettingMenu
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as DisableAndEnableMenus;
        if (parent != null)
        {
            var menuTypeField = property.serializedObject.FindProperty("menuType");
            if (menuTypeField != null && menuTypeField.enumValueIndex == 0) // 0 = SubSettingMenu
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
        }
        return 0;
    }
}

[CustomPropertyDrawer(typeof(SettingsMenuAttribute))]
public class SettingsMenuDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as DisableAndEnableMenus;
        if (parent != null)
        {
            var menuTypeField = property.serializedObject.FindProperty("menuType");
            if (menuTypeField != null && menuTypeField.enumValueIndex == 1) // 1 = SettingsMenu
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as DisableAndEnableMenus;
        if (parent != null)
        {
            var menuTypeField = property.serializedObject.FindProperty("menuType");
            if (menuTypeField != null && menuTypeField.enumValueIndex == 1) // 1 = SettingsMenu
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
        }
        return 0;
    }
}

#endif

}