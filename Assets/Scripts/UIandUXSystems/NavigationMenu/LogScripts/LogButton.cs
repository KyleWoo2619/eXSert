/*
    Handles the button logic for the dynamic log system.

    Written by Brandon Wahl
*/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Threading;

public class LogButton : MonoBehaviour, ISelectHandler
{
    private TMP_Text buttonText;
    private UnityAction onSelectAction;
    public Button button { get; private set; }
    private MenuEventSystemHandler logUI;

    private void Awake()
    {
        logUI = GameObject.FindGameObjectWithTag("LogUI").GetComponent<MenuEventSystemHandler>();
        logUI.Selectables.Add(this.button);
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
        GameObject.FindGameObjectWithTag("LogMenuOverview").SetActive(false);

    }
}
