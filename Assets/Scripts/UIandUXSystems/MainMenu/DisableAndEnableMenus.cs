using Unity.VisualScripting;
using UnityEngine;

public class DisableAndEnableMenus : MonoBehaviour
{
    [Tooltip("Leave blank if you don't intend to enable anything")][SerializeField] private GameObject enableThisGameobject = null;
    [Tooltip("Leave blank if you don't intend to disable anything")][SerializeField] private GameObject disableThisGameobject = null;
    public bool isASubSettingMenu;
    public bool isASettingsMenu;
    public bool isABackButton;
    [SerializeField] private SubMenuManager subMenuManager = null;
    [SerializeField] private GameObject thisSubMenu = null;
    [SerializeField] private GameObject thisSettingsMenu = null;

    public void OnClickSetActiveTrue()
    {
        if (isASubSettingMenu)
        {
            if (subMenuManager.lastActivatedSubMenu != null)
            {
                subMenuManager.lastActivatedSubMenu.SetActive(false);
                
            }

            subMenuManager.lastActivatedSubMenu = this.thisSubMenu;
            
            enableThisGameobject.SetActive(true);
            Debug.Log(this.name);
        }
        else if (isASettingsMenu)
        {
            subMenuManager.currentSettingsMenu = this.thisSettingsMenu;
            enableThisGameobject.SetActive(true);

            if (subMenuManager.lastActivatedSettingsMenu != null)
            {
                subMenuManager.lastActivatedSettingsMenu.SetActive(false);
            }

            subMenuManager.lastActivatedSettingsMenu = this.thisSettingsMenu;
        }

        subMenuManager.isOnSettingsMenu = false;
    }

    public void OnClickSetActiveFalse()
    {
        if (isABackButton && !subMenuManager.isOnSettingsMenu)
        {
            disableThisGameobject = subMenuManager.currentSettingsMenu;
            disableThisGameobject.SetActive(false);
            subMenuManager.isOnSettingsMenu = true;
        }
        else
        {
            disableThisGameobject = subMenuManager.gameObject;
            disableThisGameobject.SetActive(false);

            enableThisGameobject = subMenuManager.mainMenu;
            enableThisGameobject.SetActive(true);

            subMenuManager.isOnSettingsMenu = false;
        }

    }

}
