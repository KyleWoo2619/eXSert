/*
    Written by Brandon Wahl

    This script will handle swapping between the pause menu and navigation menu UIs
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class PauseAndNaviMenuSwapper : MonoBehaviour
{
    [SerializeField] private InputActionReference _swapMenuAction;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject navigationMenuUI;

    private void Awake()
    {
        if (pauseMenuUI == null)
        {
            Debug.LogWarning("Pause Menu UI is not assigned in the inspector.");
        }

        if (navigationMenuUI == null)
        {
            Debug.LogWarning("Navigation Menu UI is not assigned in the inspector.");
        }
    }

    private void Update()
    {
        if(_swapMenuAction != null && _swapMenuAction.action.triggered && pauseMenuUI != null && navigationMenuUI != null)
        {
            var pauseParent = pauseMenuUI.transform;
            var navigationParent = navigationMenuUI.transform;

            var pauseChild = pauseParent.GetChild(0);
            var navigationChild = navigationParent.GetChild(0);

            if (pauseChild.gameObject.activeSelf)
            {
                SwapToNavigationMenu();
            }
            else if (navigationChild.gameObject.activeSelf)
            {
                SwapToPauseMenu();
            }
        }
    }

    public void SwapToNavigationMenu()
    {
        if (pauseMenuUI != null && navigationMenuUI != null)
        {
            var pauseParent = pauseMenuUI.transform;
            pauseParent.GetChild(0).gameObject.SetActive(false);

            var navigationParent = navigationMenuUI.transform;
            navigationParent.GetChild(0).gameObject.SetActive(true);
        }
    }

    public void SwapToPauseMenu()
    {
        if (pauseMenuUI != null && navigationMenuUI != null)
        {
            var navigationParent = navigationMenuUI.transform;
            navigationParent.GetChild(0).gameObject.SetActive(false);

            var pauseParent = pauseMenuUI.transform;
            pauseParent.GetChild(0).gameObject.SetActive(true);
        }
    }
}