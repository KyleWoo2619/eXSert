/**
    Written by Brandon Wahl

    This script manages the state of menus in the settings within the main menu system
**/

using UnityEngine;

public class SubMenuManager : MonoBehaviour
{
    //DONT TOUCH THESE VARIABLES! They will auto populate when menus are enabled/disabled
    [SerializeField] internal GameObject lastActivatedSubMenu = null;
    [SerializeField] internal GameObject lastActivatedSettingsMenu = null;
    [SerializeField] internal GameObject currentSettingsMenu = null;

    //Assign main menu container to this
    [SerializeField] internal GameObject mainMenu = null;

    //Will change if the player is on the settings menu or not
    [SerializeField] internal bool isOnSettingsMenu = false;

    //When the settings button is clicked, this function is called
    public void OnSettingsClick()
    {
        isOnSettingsMenu = true;
    }

}
