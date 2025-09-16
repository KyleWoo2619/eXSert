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

    public string GetProfileId()
    {
        return this.profileId;
    }
    public void SetInteractable(bool interactable)
    {
        saveSlotButton.interactable = interactable;
    }
}
