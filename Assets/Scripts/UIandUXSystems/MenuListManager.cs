using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuListManager : MonoBehaviour
{
    [SerializeField] internal List<GameObject> menusToManage;

    [SerializeField] private InputActionReference backButtonInputAction;
    [SerializeField] private GameObject firstMenuToOpen;
    [SerializeField] private GameObject canvas;

    // Tracks the last selected element before opening each menu (acts as a stack)
    private readonly List<Selectable> selectionHistory = new List<Selectable>();

    private void Start()
    {
        AddToMenuList(canvas); // Add this menu to the list on start
        if (firstMenuToOpen != null)
        {
            AddToMenuList(firstMenuToOpen);
        }
    }

    private void OnEnable()
    {
        if (backButtonInputAction != null && backButtonInputAction.action != null)
        {
            backButtonInputAction.action.performed += OnBackButtonPressed;
            backButtonInputAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (backButtonInputAction != null && backButtonInputAction.action != null)
        {
            backButtonInputAction.action.performed -= OnBackButtonPressed;
        }
    }

    private void OnBackButtonPressed(InputAction.CallbackContext context)
    {
        GoBackToPreviousMenu();
    }

    public void SetAsLastSibling(GameObject menuToMove)
    {
        if (menuToMove != firstMenuToOpen && menuToMove != canvas)
            menuToMove.transform.SetAsLastSibling();
    }

    public void AddToMenuList(GameObject menuToAdd)
    {
        if (menuToAdd == null)
        {
            return;
        }

        // Remember what was selected before opening this menu
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            Selectable previousSelection = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (previousSelection != null)
            {
                selectionHistory.Insert(0, previousSelection);
            }
        }

        if (menusToManage.Count > 0)
        {
            GameObject currentTop = menusToManage[0];
            if (currentTop != null)
            {
                bool sameParent = menuToAdd.transform.parent == currentTop.transform.parent;
                bool keepCurrentTop = currentTop == firstMenuToOpen || currentTop == canvas;
                if (sameParent && !keepCurrentTop)
                {
                    RemoveFirstItemInMenuList();
                }
            }
        }

        if (menusToManage.Contains(menuToAdd))
        {
            menusToManage.Remove(menuToAdd);
        }

        if (!menusToManage.Contains(menuToAdd))
        {
            menusToManage.Insert(0, menuToAdd);
            menuToAdd.SetActive(true);
            SetAsLastSibling(menusToManage[0]);
            
            // Select the first selectable in the new menu
            Selectable firstSelectable = menuToAdd.GetComponent<Selectable>();
            if (firstSelectable == null)
            {
                firstSelectable = menuToAdd.GetComponentInChildren<Selectable>();
            }
            
            if (firstSelectable != null)
            {
                firstSelectable.Select();
            }
        }

        Debug.Log("Menu added to list. Current menus in list: " + menusToManage.Count);
    }

    public void SelectFirstSelectOnBack(GameObject menuToAdd){
        MenuEventSystemHandler menuHandler = menuToAdd.GetComponent<MenuEventSystemHandler>();
        if(menuHandler != null)
        {
            if(menuHandler._firstSelected != null)
            {
                menuHandler._firstSelected.Select();
            }
            else
            {
                Debug.LogWarning("First Selected is not set for menu: " + menuToAdd.name);
            }
        }
        else
        {
            Debug.LogWarning("MenuEventSystemHandler component not found on menu: " + menuToAdd.name);
        }
    }

    public void CheckIfPreviousMenuSharesParent(GameObject menuToCheck, GameObject menuToOpen)
    {
        if(menuToCheck.transform.parent == menusToManage[0].transform.parent && menuToCheck != firstMenuToOpen && menuToCheck != canvas)
        {
            RemoveFirstItemInMenuList();
        }
    }

    public void GoBackToPreviousMenu()
    {
        if(menusToManage.Count <= 2)
            return;

        // Remove current top menu
        GameObject currentTop = menusToManage[0];
        if (currentTop != null)
        {
            currentTop.SetActive(false);
        }
        menusToManage.RemoveAt(0);

        // Select the first selectable in the new top menu
        if (menusToManage.Count > 0)
        {
            GameObject newTop = menusToManage[0];
            if (newTop != null)
            {
                SelectFirstSelectOnBack(newTop);
            }
        }
    }

    public void RemoveFirstItemInMenuList()
    {
        if (menusToManage.Count == 0) return;

        menusToManage[0].SetActive(false);
        menusToManage.RemoveAt(0);

        // Pop the last selection and reselect it if available
        Selectable target = null;
        if (selectionHistory.Count > 0)
        {
            target = selectionHistory[0];
            selectionHistory.RemoveAt(0);
        }

        // Fallback: select first selectable in the now-visible menu (if any)
        if (target == null && menusToManage.Count > 0)
        {
            target = menusToManage[0].GetComponent<Selectable>();
            if (target == null)
            {
                target = menusToManage[0].GetComponentInChildren<Selectable>();
            }
        }

        if (target != null)
        {
            target.Select();
        }
    }

    public void SwapBetweenMenus()
    {
        if(menusToManage.Count >= 5){
            RemoveFirstItemInMenuList();
        }
    }

    public void ClearMenuList()
    {
        foreach(GameObject menu in menusToManage)
        {
            menu.SetActive(false);
        }
        menusToManage.Clear();
        selectionHistory.Clear();
    }

}