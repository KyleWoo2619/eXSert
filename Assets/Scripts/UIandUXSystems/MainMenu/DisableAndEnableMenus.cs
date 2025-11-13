/**
    Written by Brandon Wahl

    This script handles enabling and disabling menus in the menu system. Because multiple different menus will occupy
    the same space on the screen, this script allows for easy switching between them.
**/



using UnityEngine;

public class DisableAndEnableMenus : MonoBehaviour
{
    [Tooltip("Leave blank if you don't intend to enable anything")][SerializeField] private GameObject enableThisGameobject = null;
    [Tooltip("Leave blank if you don't intend to disable anything")][SerializeField] private GameObject disableThisGameobject = null;
    [Tooltip("Select this if the menu this is assigned to is for a sub setting menu (e.g. invertY or master volume)")]public bool isASubSettingMenu;
    [Tooltip("Select this if the menu this is assigned to is for a settings menu")]public bool isASettingsMenu;
    [Tooltip("Select this if the menu this is assigned to is a back button")]public bool isABackButton;
    [Tooltip("Assign the SubMenuManager (settings container) to this")][SerializeField] private SubMenuManager subMenuManager = null;
    [Tooltip("Assign the SubMenu (e.g. invertY or master volume) you want to this")][SerializeField] private GameObject thisSubMenu = null;
    [Tooltip("Assign the Settings Menu you want to this")][SerializeField] private GameObject thisSettingsMenu = null;

    //Enables or disables the assigned gameobjects based on the boolean selections
    public void OnClickSetActiveTrue()
    {
        //If sub setting menu is selected this part of the function will run
        if (isASubSettingMenu)
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
        else if (isASettingsMenu)
        {
            // If the last activated settings menu is not empty, it becomes inactive
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
        if (isABackButton && !subMenuManager.isOnSettingsMenu)
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

}
