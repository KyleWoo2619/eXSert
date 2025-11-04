/*
    Controls the items in the general settings and calls to the settings manager to edit values. This script also handles applying the settings and resetting them.

    written by Brandon Wahl
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GeneralSettings : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [SerializeField] private Slider sensSlider = null;
    [SerializeField] private float defaultSens = 1.5f;

    [Header("Vibration Settings")]
    [SerializeField] private Slider vibrationSlider = null;
    [SerializeField] private float defaultVibration = 0.5f;

    [Header("On/Off Text")]
    [SerializeField] private TMP_Text invertYText = null;
    [SerializeField] private TMP_Text comboProgressionText = null;
    private bool isInvertYOn = false;
    private bool isComboProgressionOn;
    private float vibration;

    //All functions below sets values based on player choice
    public void SetSens(float sens)
    {
        SettingsManager.Instance.sensitivity = sens;
        PlayerPrefs.SetFloat("masterSens", SettingsManager.Instance.sensitivity);
    }

    public void SetVibration(float vibrate)
    {
        SettingsManager.Instance.rumbleStrength = vibrate;
        PlayerPrefs.SetFloat("masterVibrateStrength", SettingsManager.Instance.rumbleStrength);
    }

    public void SetComboProgressionDisplay(bool displayOn)
    {
        SettingsManager.Instance.comboProgression = displayOn;

        Debug.Log("Combo Progression: " + !isComboProgressionOn);

        if (displayOn)
        {
            isComboProgressionOn = true;
            comboProgressionText.text = "On";
        }
        else
        {
            isComboProgressionOn = false;
            comboProgressionText.text = "Off";
        }

        if (isComboProgressionOn)
        {
            PlayerPrefs.SetInt("masterCombo", 1);
            SettingsManager.Instance.comboProgression = true;
        }
        else
        {
            PlayerPrefs.SetInt("masterCombo", 0);
            SettingsManager.Instance.comboProgression = false;
        }
    }

    public void SetInvertY(bool invertYOn)
    {
        SettingsManager.Instance.invertY = invertYOn;
        Debug.Log("Invert Y: " + !isInvertYOn);

        if (invertYOn)
        {
            isInvertYOn = true;
            invertYText.text = "On";
        }
        else
        {
            isInvertYOn = false;
            invertYText.text = "Off";
        }

        
    }

    public void ApplySettings(string settingName)
    {
        if(settingName == "sens")
        {
            
        }
    }

    //Resets the settings
    public void ResetButton()
    {
        SettingsManager.Instance.sensitivity = defaultSens;
        sensSlider.value = defaultSens;

        SettingsManager.Instance.rumbleStrength = defaultVibration;
        vibrationSlider.value = defaultVibration;
        SettingsManager.Instance.invertY = false;
    }

}
