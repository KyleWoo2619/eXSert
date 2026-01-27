/*
    Written by Brandon

    This script controls the functionality of the Diary Button
*/

using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

public class DiaryButton : MonoBehaviour, ISelectHandler
{
    private TMP_Text buttonText;
    private UnityAction onSelectAction;
    public Button button { get; private set; }
    private MenuEventSystemHandler diaryUI;


    private void Awake()
    {

        this.button = this.GetComponent<Button>();
        
        GameObject diaryUIObject = GameObject.FindGameObjectWithTag("DiaryUI");
        if (diaryUIObject != null)
        {
            diaryUI = diaryUIObject.GetComponent<MenuEventSystemHandler>();
            if (diaryUI != null)
            {
                diaryUI.Selectables.Add(this.button);
            }
            else
            {
                Debug.LogError("MenuEventSystemHandler component not found on DiaryUI GameObject");
            }
        }
        else
        {
            Debug.LogError("GameObject with tag 'DiaryUI' not found");
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
            this.button.onClick.AddListener(selectAction);
        }
    }

    public void FindAddMenusToList()
    {
        GameObject NavigationMenuObject = GameObject.FindGameObjectWithTag("NavigationMenu");

        GameObject individualDiaryMenuObject = GameObject.FindGameObjectWithTag("IndividualDiaryMenu");

        if(NavigationMenuObject != null)
        {
            var MenuToManage = NavigationMenuObject.GetComponent<MenuListManager>();
            if(individualDiaryMenuObject != null)
            {
                Transform child = individualDiaryMenuObject.transform.GetChild(0);
                MenuToManage.AddToMenuList(child.gameObject);
            }
        }
        else
        {
            Debug.LogError("GameObject with tag 'NavigationMenu' not found");
        }
    }
        

    public void OnSelect(BaseEventData eventData)
    {
        onSelectAction();
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