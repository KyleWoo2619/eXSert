/*
Written by Brandon Wahl

Changes text on the save slots depending on if there is data assigned to that save slot

*/

using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SaveSlots : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private string profileId = "";

    [Header("Content")]
    [SerializeField] private GameObject noDataContent;
    [SerializeField] private GameObject hasDataContent;

    [SerializeField] private TextMeshProUGUI healthCountText;

    private Button saveSlotButton;

    private void Awake()
    {
        saveSlotButton = this.GetComponent<Button>();
    }

    //Depending on if the data is null or not, it will show their respective texts
    public void SetData(GameData data)
    {
        if(data == null)
        {
            noDataContent.SetActive(true);
            hasDataContent.SetActive(false);
        }
        else
        {
            noDataContent.SetActive(false);
            hasDataContent.SetActive(true);

            healthCountText.text = "Health: " + data.health.ToString();
        }
    }

    //Gathers the individual profileId being used
    public string GetProfileId()
    {
        return this.profileId;
    }

    //Sets interactability of save slots
    public void SetInteractable(bool interactable)
    {
        saveSlotButton.interactable = interactable;
    }
}
