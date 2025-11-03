/*
    Controls the settings that involve graphics

    written by Brandon Wahl
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphicsSettings : MonoBehaviour, ISettings
{
    [SerializeField] private TMP_Text brightnessTextValue = null;
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private float defaultBrightness = 1f;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Toggle cameraShakeToggle;
    [SerializeField] private Toggle motionBlurToggle;
    [SerializeField] private int frameRate = 60;

    private int qualityLevel;
    private int fpsLevel;
    private bool isMotionBlur;
    private float brightnessLevel;

    //Alls functions below change values based on player choice
    public void SetBrightness(float brightness)
    {
        Screen.brightness = brightness;
        brightnessTextValue.text = brightness.ToString("0.0");

    }

    public void SetMotionBlur(bool motionBlur)
    {
        isMotionBlur = motionBlur;
    }

    public void SetQuality(int qualityIndex)
    {
        qualityLevel = qualityIndex;
    }

    public void SetFPS(int framerate)
    {
        QualitySettings.vSyncCount = 0;

        switch (framerate)
        {
            case 0:
                Application.targetFrameRate = 30;
                break;
            case 1:
                Application.targetFrameRate = 60;
                break;
            case 2:
                Application.targetFrameRate = -1;
                break;
        }
    }

    //Applies graphic settings
    public void GraphicsApply()
    {
        PlayerPrefs.SetFloat("masterBrightness", brightnessLevel);

        PlayerPrefs.SetInt("masterQuality", qualityLevel);
        QualitySettings.SetQualityLevel(qualityLevel);

        PlayerPrefs.SetInt("masterFPS", fpsLevel);
        Application.targetFrameRate = fpsLevel;

        PlayerPrefs.SetInt("masterMotionBlur", (isMotionBlur ? 1 : 0));
    }

    //Resets graphics settings
    public void ResetButton()
    {

        brightnessSlider.value = defaultBrightness;
        brightnessTextValue.text = defaultBrightness.ToString("0.0");

        fpsDropdown.value = 1;
        Application.targetFrameRate = 30;

        qualityDropdown.value = 1;
        QualitySettings.SetQualityLevel(1);

        fullScreenToggle.isOn = false;
        Screen.fullScreen = false;

        motionBlurToggle.isOn = false;

        GraphicsApply();

    }
}
