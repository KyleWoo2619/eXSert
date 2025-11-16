using UnityEngine;

public class ComboProgressionDisplay : MonoBehaviour
{
    [SerializeField] private GameObject comboProgressionUI;
    void Start()
    {
        if(comboProgressionUI == null)
        {
            Debug.LogWarning("The combo progression display is currently empty");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!SettingsManager.Instance.comboProgression)
        {
            comboProgressionUI.SetActive(false);
        }
        else
        {
            comboProgressionUI.SetActive(true);
        }
    }
}
