/**
    Written by Brandon Wahl

    This script manages the state of menus in the settings within the main menu system
**/

using UnityEngine;

public class SubMenuManager : MonoBehaviour
{
    [SerializeField] internal GameObject lastActivatedSubMenu = null;
    [SerializeField] internal GameObject lastActivatedSettingsMenu = null;
    [SerializeField] internal GameObject currentSettingsMenu = null;
    [SerializeField] internal GameObject mainMenu = null;
    [SerializeField] internal bool isOnSettingsMenu = false;

    //When the settings button is clicked, this function is called
    public void OnSettingsClick()
    {
        isOnSettingsMenu = true;
    }

}
