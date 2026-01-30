using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuListManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> menusToManage;

    [SerializeField] private InputActionReference backButtonInputAction;

    // Tracks the last selected element before opening each menu (acts as a stack)
    private readonly List<Selectable> selectionHistory = new List<Selectable>();

    public void AddToMenuList(GameObject menuToAdd)
    {
        // Remember what was selected before opening this menu
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            Selectable previousSelection = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (previousSelection != null)
            {
                selectionHistory.Insert(0, previousSelection);
            }
        }

        if (!menusToManage.Contains(menuToAdd))
        {
            menusToManage.Insert(0, menuToAdd);
            menuToAdd.SetActive(true);
        }

        Debug.Log("Menu added to list. Current menus in list: " + menusToManage.Count);
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

        // Fallback: select first selectable in the new top menu (if any)
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
        if(menusToManage.Count > 2)
        {
            menusToManage[1].SetActive(false);
            menusToManage.RemoveAt(1);
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

    private void Update()
    {
        if(backButtonInputAction != null && backButtonInputAction.action != null && backButtonInputAction.action.triggered)
        {
            RemoveFirstItemInMenuList();
        }
    }
}