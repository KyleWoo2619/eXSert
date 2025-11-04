using UnityEngine;

public class SubMenuManager : MonoBehaviour
{
    [SerializeField] internal GameObject lastActivatedSubMenu = null;
    [SerializeField] internal GameObject currentSettingsMenu = null;
    [SerializeField] internal GameObject mainMenu = null;
    [SerializeField] internal bool isOnSettingsMenu = false;

    public void OnSettingsClick()
    {
        isOnSettingsMenu = true;
    }

}
