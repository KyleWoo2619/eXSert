using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GraphicsSettings : MonoBehaviour, ISettings
{
    [SerializeField] private TMP_Text brightnessTextValue = null;
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private float defaultBrightness = 1f;
    public TMP_Dropdown resolutionDropdown;
    private Resolution[] resolutions;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Toggle cameraShakeToggle;
    [SerializeField] private Toggle motionBlurToggle;
    [SerializeField] private int frameRate = 60;

    private int qualityLevel;
    private int fpsLevel;
    private bool isFullScreen;
    private bool isMotionBlur;
    private float brightnessLevel;

    private void Start()
    {
         resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

    }

    public void SetBrightness(float brightness)
    {
        Screen.brightness = brightness;
        brightnessTextValue.text = brightness.ToString("0.0");

    }

    public void SetMotionBlur(bool motionBlur)
    {
        isMotionBlur = motionBlur;
    }

    public void SetFullScreen(bool isFullscreen)
    {
        isFullScreen = isFullscreen;
    }

    public void SetQuality(int qualityIndex)
    {
        qualityLevel = qualityIndex;
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
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

    public void GraphicsApply()
    {
        PlayerPrefs.SetFloat("masterBrightness", brightnessLevel);

        PlayerPrefs.SetInt("masterQuality", qualityLevel);
        QualitySettings.SetQualityLevel(qualityLevel);

        PlayerPrefs.SetInt("masterFullScreen", (isFullScreen ? 1 : 0));
        Screen.fullScreen = isFullScreen;

        PlayerPrefs.SetInt("masterFPS", fpsLevel);
        Application.targetFrameRate = fpsLevel;

        PlayerPrefs.SetInt("masterMotionBlur", (isMotionBlur ? 1 : 0));

        //StartCoroutine(ConfirmationBox());
    }

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

        Resolution currentResolution = Screen.currentResolution;
        Screen.SetResolution(currentResolution.width, currentResolution.height, Screen.fullScreen);
        resolutionDropdown.value = resolutions.Length;
        GraphicsApply();
        
    }
}
