using UnityEngine;
using TMPro;

public class FooterManager : MonoBehaviour
{
    [SerializeField] private TMP_Text footerText;
    [SerializeField] internal GameObject logHolderUI;
    [SerializeField] internal GameObject diaryHolderUI;
    [SerializeField] internal GameObject mainNavigationMenuHolderUI;
    [SerializeField] internal GameObject IndividualLogUI;
    [SerializeField] internal GameObject IndividualDiaryUI;
    [SerializeField] internal GameObject overlayUI;

    public void CheckForFooterUpdate()
    {
        if (logHolderUI.activeSelf)
        {
            footerText.text = "Read Log Entries";
        }
        else if (diaryHolderUI.activeSelf)
        {
            footerText.text = "Read Diary Entries";
        }
        else if (mainNavigationMenuHolderUI.activeSelf)
        {
            footerText.text = "Explore Menu Options";
        }
        else if (IndividualLogUI.activeSelf)
        {
            footerText.text = "Return to Logs";
        }
        else if (IndividualDiaryUI.activeSelf)
        {
            footerText.text = "Return to Diary";
        }
    }

    private void Update()
    {
        CheckForFooterUpdate();
    }

}
