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
    private GameObject logMenuOverview;
    private GameObject indivdualLogMenu;
    public Button button { get; private set; }

    //Grabs the two log menus and assigns them
    void Awake()
    {
        logMenuOverview = GameObject.FindGameObjectWithTag("LogMenuOverview");
        indivdualLogMenu = GameObject.FindGameObjectWithTag("IndividualLogMenu");
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
        logMenuOverview.gameObject.SetActive(false);
        indivdualLogMenu.gameObject.SetActive(true);
    }
}
