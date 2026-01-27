/*
    Written originally by Brandon Wahl

    This script oringinated from the DisableAndEnableMenus script to handle settings menu tab toggling functionality

    CoPilot split the logic from the original script into this new script to better separate concerns.

*/

using UnityEngine;

public class SettingsMenuToggle : MonoBehaviour
{
    [Tooltip("Assign the SubMenuManager (settings container) to this")] [SerializeField]
    private SubMenuManager subMenuManager = null;

    [Tooltip("Assign the Settings Menu you want to this")][SerializeField]
    private GameObject thisSettingsMenu = null;

    [Tooltip("Leave blank if you don't intend to enable anything")][SerializeField]
    private GameObject enableThisGameobject = null;

    /// <summary>
    /// Open the targeted settings menu tab: deactivate previously-open settings tab, set current, and enable target content.
    /// </summary>
    public void OpenSettingsMenu()
    {
        if (subMenuManager == null)
        {
            Debug.LogWarning($"{nameof(SettingsMenuToggle)}: subMenuManager is not assigned on '{name}'. Enabling target if present.");
            SafeSetActive(enableThisGameobject, true);
            return;
        }

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

        subMenuManager.isOnSettingsMenu = false;
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
}
