/*
    Handles the button logic for the dynamic log system.

    Written by Brandon Wahl
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class LogButton : MonoBehaviour, ISelectHandler
{
    private TMP_Text buttonText;
    private UnityAction onSelectAction;
    public Button button { get; private set; }
    private MenuEventSystemHandler logUI;

    private void Awake()
    {

        this.button = this.GetComponent<Button>();
        
        GameObject logUIObject = GameObject.FindGameObjectWithTag("LogUI");
        if (logUIObject != null)
        {
            logUI = logUIObject.GetComponent<MenuEventSystemHandler>();
            if (logUI != null)
            {
                logUI.Selectables.Add(this.button);
            }
            else
            {
                Debug.LogError("MenuEventSystemHandler component not found on LogUI GameObject");
            }
        }
        else
        {
            Debug.LogError("GameObject with tag 'LogUI' not found");
        }
    }

    //Components get assigned moment of initlization
    public void InitializeButton(string logName, UnityAction selectAction)
    {
        // Ensure button is assigned (in case InitializeButton is called before Awake)
        if (this.button == null)
        {
            this.button = this.GetComponent<Button>();
        }
        
        this.buttonText = this.GetComponentInChildren<TMP_Text>();

        if (this.buttonText != null)
        {
            this.buttonText.text = logName;
        }
        
        this.onSelectAction = selectAction;
        
        // Add onClick listener so action triggers on click, not just select
        if (this.button != null && selectAction != null)
        {
            this.button.onClick.AddListener(() =>
            {
                // Ensure EventSystem selection updates for mouse clicks
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null)
                {
                    es.SetSelectedGameObject(this.gameObject);
                }

                selectAction();
            });
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (onSelectAction != null)
        {
            onSelectAction();
        }
    }

    public void FindAddMenusToList()
    {
        GameObject NavigationMenuObject = GameObject.FindGameObjectWithTag("NavigationMenu");

        GameObject individualLogMenuObject = GameObject.FindGameObjectWithTag("IndividualLogMenu");

        if(NavigationMenuObject != null)
        {
            var MenuToManage = NavigationMenuObject.GetComponent<MenuListManager>();
            if(individualLogMenuObject != null)
            {
                Transform child = individualLogMenuObject.transform.GetChild(0);
                MenuToManage.AddToMenuList(child.gameObject);
            }
        }
        else
        {
            Debug.LogError("GameObject with tag 'NavigationMenu' not found");
        }
        
    }

    //Hides Menus
    public void AddOverlay()
    {

        GameObject overlayParent = GameObject.FindGameObjectWithTag("Overlay");
        if (overlayParent != null)
        {
            Transform child = overlayParent.transform.GetChild(0);
            child.gameObject.SetActive(true);
        } else {
            Debug.LogError("GameObject with tag 'Overlay' not found");
        }
    }
}
