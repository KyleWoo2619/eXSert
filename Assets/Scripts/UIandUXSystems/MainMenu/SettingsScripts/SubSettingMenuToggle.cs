/*
    Written originally by Brandon Wahl

    This script oringinated from the DisableAndEnableMenus script to handle sub-settings menu tab toggling functionality

    CoPilot split the logic from the original script into this new script to better separate concerns.

*/

using UnityEngine;

public class SubSettingMenuToggle : MonoBehaviour
{
    [Tooltip("Assign the SubMenuManager (settings container) to this")] [SerializeField]
    private SubMenuManager subMenuManager = null;

    [Tooltip("Assign the SubMenu (e.g. invertY or master volume) you want to this")]
    [SerializeField]
    private GameObject thisSubMenu = null;

    [Tooltip("Leave blank if you don't intend to enable anything")]
    [SerializeField]
    private GameObject enableThisGameobject = null;

    /// <summary>
    /// Open the targeted sub-setting menu: deactivate previously-open sub menu, set this one as last, and enable target content.
    /// </summary>
    public void OpenSubSetting()
    {
        if (subMenuManager == null)
        {
            Debug.LogWarning($"{nameof(SubSettingMenuToggle)}: subMenuManager is not assigned on '{name}'. Enabling target if present.");
            SafeSetActive(enableThisGameobject, true);
            return;
        }

        // If the last activated sub menu is not empty, it becomes inactive
        if (subMenuManager.lastActivatedSubMenu != null)
        {
            subMenuManager.lastActivatedSubMenu.SetActive(false);
        }

        // The current sub menu is set to this one
        subMenuManager.lastActivatedSubMenu = this.thisSubMenu;

        // Enable the assigned gameobject
        SafeSetActive(enableThisGameobject, true);

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
