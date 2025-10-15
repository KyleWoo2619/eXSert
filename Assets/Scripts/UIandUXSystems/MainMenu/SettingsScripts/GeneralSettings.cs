using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Build;

public class GeneralSettings : MonoBehaviour, ISettings
{
    [SerializeField] private TMP_Text sensTextValue = null;
    [SerializeField] private Slider sensSlider = null;
    [SerializeField] private float defaultSens = 1.5f;
    [SerializeField] private Toggle controllerVibration;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private TMP_Text vibrationTextValue = null;
    [SerializeField] private Slider vibrationSlider = null;
    [SerializeField] private float defaultVibration = 0.5f;
    [SerializeField] private TMP_Dropdown languageDropdown;

    private float vibration;
    private bool isVibrate;

    public void SetSens(float sens)
    {
        SettingsManager.Instance.sensitivity = sens;
        sensTextValue.text = sens.ToString("0.0");
    }

    public void SetVibration(float vibrate)
    {
        vibrationTextValue.text = vibrate.ToString("0.0");
    }
    public void SetControllerVibrationOn(bool controllerVibrate)
    {

    }

    public void SetInvertY(bool invertYOn)
    {
        SettingsManager.Instance.invertY = invertYOn;
    }

    public void GeneralApply()
    {

        PlayerPrefs.SetFloat("masterSens", SettingsManager.Instance.sensitivity);
        
        if (invertYToggle.isOn)
        {
            PlayerPrefs.SetInt("masterInvertY", 1);
            SettingsManager.Instance.invertY = true;
        }
        else
        {
            PlayerPrefs.SetInt("masterInvertY", 0);
            SettingsManager.Instance.invertY = false;
        }

    }

    public void ResetButton()
    {
        sensTextValue.text = defaultSens.ToString("0.0");
        SettingsManager.Instance.sensitivity = defaultSens;
        sensSlider.value = defaultSens;
        GeneralApply();
    }

}
