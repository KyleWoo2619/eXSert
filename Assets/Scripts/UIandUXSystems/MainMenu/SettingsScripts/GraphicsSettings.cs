/*
    Controls the settings that involve graphics

    written by Brandon Wahl
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.Build;

public class GraphicsSettings : MonoBehaviour
{
    [Header("Brightness Settings")]
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private float defaultBrightness = 1f;
    private float brightnessLevel;

    [Header("Display Mode Settings")]
    [SerializeField] private TMP_Text displayModeText;
    private bool isFullscreen;

    [Header("FPS Mode Settings")]
    [SerializeField] private int frameRate = 60;
    [SerializeField] private TMP_Text fpsText;
    private int fpsLevel;

    [Header("Resolution Mode Settings")]
    [SerializeField] private TMP_Text resolutionText;

    [Header("Camera Shake Settings")]
    [SerializeField] private TMP_Text cameraShakeText;
    private bool isCameraShake;

    [Header("Motion Blur Settings")]
    [SerializeField] private TMP_Text motionBlurText;
    private bool isMotionBlur;
    

    //Alls functions below change values based on player choice
    public void SetBrightness(float brightness)
    {
        Screen.brightness = brightness;
    }

    public void SetDisplayMode(string displayMode)
    {
        if (displayMode == "fullscreen")
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            displayModeText.text = "Fullscreen";
            isFullscreen = true;
        }
        else if (displayMode == "windowed")
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            displayModeText.text = "Windowed";
            isFullscreen = false;
        }
        else
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            displayModeText.text = "Borderless";
            isFullscreen = false;
        }
    }

    public void SetMotionBlur(bool motionBlur)
    {
        isMotionBlur = motionBlur;

        if (motionBlur)
        {
            motionBlurText.text = "On";
            Debug.Log("Motion Blur:" + isMotionBlur);
        }
        else
        {
            motionBlurText.text = "Off";
            Debug.Log("Motion Blur:" + isMotionBlur);
        }
    }

    public void SetResolution(string resolution)
    {
        if (resolution == "1920x1080")
        {
            resolutionText.text = "1920x1080";
            Screen.SetResolution(1920, 1080, isFullscreen);
        }
        else
        {
            resolutionText.text = "2560x1440";
            Screen.SetResolution(2560, 1440, isFullscreen);
        }
    }

    public void SetCameraShake(bool cameraShake)
    {
        isCameraShake = cameraShake;

        if (cameraShake)
        {
            cameraShakeText.text = "On";
            Debug.Log("Motion Blur:" + isCameraShake);
        }
        else
        {
            cameraShakeText.text = "Off";
            Debug.Log("Motion Blur:" + isCameraShake);
        }
    }

    public void SetFPS(int framerate)
    {
        QualitySettings.vSyncCount = 0;

        if (framerate == 60)
        {
            fpsText.text = "60";
            Application.targetFrameRate = 60;
        }
        else if (framerate == 30)
        {
            fpsText.text = "30";
            Application.targetFrameRate = 30;
        }
        else
        {
            fpsText.text = "Unlimited";
            Application.targetFrameRate = -1;
        }
    }

    //Applies graphic settings
    public void GraphicsApply()
    {
        PlayerPrefs.SetFloat("masterBrightness", brightnessLevel);

        PlayerPrefs.SetInt("masterFPS", fpsLevel);
        Application.targetFrameRate = fpsLevel;

        PlayerPrefs.SetInt("masterMotionBlur", (isMotionBlur ? 1 : 0));
    }

    //Resets graphics settings
    public void ResetButton()
    {

        brightnessSlider.value = defaultBrightness;

        Application.targetFrameRate = 30;

        GraphicsApply();

    }
}
