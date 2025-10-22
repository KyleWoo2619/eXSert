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


    public void InitializeButton(string logName, UnityAction selectAction)
    {
        logMenuOverview = GameObject.FindGameObjectWithTag("LogMenuOverview");
        indivdualLogMenu = GameObject.FindGameObjectWithTag("IndividualLogMenu");

        this.button = this.GetComponent<Button>();
        this.buttonText = this.GetComponentInChildren<TMP_Text>();

        this.buttonText.text = logName;
        this.onSelectAction = selectAction;
    }

    public void OnSelect(BaseEventData eventData)
    {
        onSelectAction();
    }

    public void hideMenuOnClick()
    {
        logMenuOverview.gameObject.SetActive(false);
        indivdualLogMenu.gameObject.SetActive(true);
    }
}
