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
        diaryUI = GameObject.FindGameObjectWithTag("DiaryUI").GetComponent<MenuEventSystemHandler>();
        diaryUI.Selectables.Add(this.button);
    }

    //Components get assigned moment of initlization
    public void InitializeButton(string logName, UnityAction selectAction)
    {
        this.button = this.GetComponent<Button>();
        this.buttonText = this.GetComponentInChildren<TMP_Text>();

        this.buttonText.text = logName;
        this.onSelectAction = selectAction;

    }

    public void OnSelect(BaseEventData eventData)
    {
        onSelectAction();
    }

    //Hides Menus
    public void hideMenuOnClick()
    {
        GameObject.FindGameObjectWithTag("DiaryMenuOverview").SetActive(false);

    }
}