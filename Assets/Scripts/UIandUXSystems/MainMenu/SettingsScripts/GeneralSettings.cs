/*
    Controls the items in the general settings and calls to the settings manager to edit values. This script also handles applying the settings and resetting them.

    written by Brandon Wahl
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private float vibration;
    private bool isVibrate;

    //All functions below sets values based on player choice
    public void SetSens(float sens)
    {
        SettingsManager.Instance.sensitivity = sens;
        sensTextValue.text = sens.ToString("0.0");
    }

    public void SetVibration(float vibrate)
    {
        SettingsManager.Instance.rumbleStrength = vibrate;
        vibrationTextValue.text = vibrate.ToString("0.0");
    }
    public void SetControllerVibrationOn(bool controllerVibrate)
    {
        isVibrate = controllerVibrate;
    }

    public void SetInvertY(bool invertYOn)
    {
        SettingsManager.Instance.invertY = invertYOn;
    }

    //Applies general settings
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

        PlayerPrefs.SetFloat("masterVibrateStrength", SettingsManager.Instance.rumbleStrength);

        if (controllerVibration.isOn)
        {
            PlayerPrefs.SetInt("masterVibrationOn", 1);
            SettingsManager.Instance.rumbleOn = true;
        }
        else
        {
            PlayerPrefs.SetInt("masterVibrationOn", 0);
            SettingsManager.Instance.rumbleOn = false;
        }

    }

    //Resets the settings
    public void ResetButton()
    {
        sensTextValue.text = defaultSens.ToString("0.0");
        SettingsManager.Instance.sensitivity = defaultSens;
        sensSlider.value = defaultSens;

        vibrationTextValue.text = defaultVibration.ToString("0.0");
        SettingsManager.Instance.rumbleStrength = defaultVibration;
        vibrationSlider.value = defaultVibration;
        SettingsManager.Instance.rumbleOn = true;

        SettingsManager.Instance.invertY = false;

        GeneralApply();
    }

}
