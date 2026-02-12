using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class LoadPrefs : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool canUse = false;
    private AudioSettings sound;
    private GraphicsSettings graphics;
    private GeneralSettings general;
    

    [Header("Audio Settings")]
    [SerializeField] Slider masterVolumeSlider = null;
    [SerializeField] Slider musicVolumeSlider = null;
    [SerializeField] Slider sfxVolumeSlider = null;
    [SerializeField] Slider voiceVolumeSlider = null;

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Text displayModeText = null;
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private float fallbackDefaultBrightness = 0.75f;
    [SerializeField] private TMP_Text resolutionTextValue = null;
    [SerializeField] private TMP_Text motionBlurOnOffText = null;
    [SerializeField] private TMP_Text cameraShakeOnOffText = null;
    [SerializeField] private TMP_Text fpsTextValue = null;

    [Header("General Settings")]
    [SerializeField] private Slider sensSlider = null;
    [SerializeField] private TMP_Text invertYTextValue = null;
    [SerializeField] private TMP_Text comboTextValue = null;
    [SerializeField] private Slider vibrationSlider= null;

    [SerializeField] private GameObject settingsManager;

    private void Awake()
    {
        sound = settingsManager.GetComponent<AudioSettings>();
        graphics = settingsManager.GetComponent<GraphicsSettings>();
        general = settingsManager.GetComponent<GeneralSettings>();

        if (canUse)
        {
            LoadAudioSettings();
            LoadGeneralSettings();
            LoadGraphicsSettings();
        }
        else
        {
            if (sound != null) sound.ResetButton();
            if (graphics != null) graphics.ResetButton();
            if (general != null) general.ResetButton();
        }
    }


    public void LoadAudioSettings()
    {
        if (PlayerPrefs.HasKey("masterVolume"))
        {
            float masterVolume = PlayerPrefs.GetFloat("masterVolume");

            if (masterVolumeSlider) masterVolumeSlider.value = masterVolume;
            if (SoundManager.Instance != null && SoundManager.Instance.masterSource)
                SoundManager.Instance.masterSource.volume = masterVolume;
        }

        if (PlayerPrefs.HasKey("sfxVolume"))
        {
            float sfxVolume = PlayerPrefs.GetFloat("sfxVolume");

            if (sfxVolumeSlider) sfxVolumeSlider.value = sfxVolume;
            if (SoundManager.Instance != null && SoundManager.Instance.sfxSource)
                SoundManager.Instance.sfxSource.volume = sfxVolume;
        }

        if (PlayerPrefs.HasKey("voiceVolume"))
        {
            float voiceVolume = PlayerPrefs.GetFloat("voiceVolume");

            if (voiceVolumeSlider) voiceVolumeSlider.value = voiceVolume;
            if (SoundManager.Instance != null && SoundManager.Instance.voiceSource)
                SoundManager.Instance.voiceSource.volume = voiceVolume;
        }

        if (PlayerPrefs.HasKey("musicVolume"))
        {
            float musicVolume = PlayerPrefs.GetFloat("musicVolume");

            if (musicVolumeSlider) musicVolumeSlider.value = musicVolume;
            if (SoundManager.Instance != null && SoundManager.Instance.musicSource)
                SoundManager.Instance.musicSource.volume = musicVolume;
        }
    }

    public void LoadGeneralSettings()
    {
        if (PlayerPrefs.HasKey("masterVibrateStrength"))
        {
            float localVibration = PlayerPrefs.GetFloat("masterVibrateStrength");
            if (vibrationSlider) vibrationSlider.value = localVibration;
        }

        if (PlayerPrefs.HasKey("masterCombo"))
        {
            int localCombo = PlayerPrefs.GetInt("masterCombo");
            if (localCombo == 1)
            {
                if (general != null)
                    SettingsManager.Instance.comboProgression = true;
            }
            else
            {
                if (general != null)
                    SettingsManager.Instance.comboProgression = false;
            }
        }

        if (PlayerPrefs.HasKey("masterSens"))
        {
            float localSens = PlayerPrefs.GetFloat("masterSens");
            if (sensSlider) sensSlider.value = localSens;
        }

        if (PlayerPrefs.HasKey("masterInvertY"))
        {
            int localInvert = PlayerPrefs.GetInt("masterInvertY");

            if (localInvert == 1)
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.invertY = true;
            }
            else
            {
                if (SettingsManager.Instance != null)
                    SettingsManager.Instance.invertY = false;
            }
        }
    }

    public void LoadGraphicsSettings()
    {
        if (PlayerPrefs.HasKey("masterFullscreen"))
        {
            int fullscreenInt = PlayerPrefs.GetInt("masterFullscreen");

            if (fullscreenInt == 0)
            {
                if (graphics != null)
                {
                    graphics.SetDisplayMode(0); // Fullscreen
                }
            }
            else if (fullscreenInt == 1)
            {
                if (graphics != null)
                {
                    graphics.SetDisplayMode(1); // Windowed
                }
            }
            else
            {
                if (graphics != null)
                {
                    graphics.SetDisplayMode(2); // Borderless
                }
            }
        }

        if (PlayerPrefs.HasKey("masterResolution"))
        {
            int resolutionInt = PlayerPrefs.GetInt("masterResolution");

            if (resolutionInt == 0)
            {
                if (graphics != null)
                {
                    graphics.SetResolution("1920x1080"); // 1920x1080
                }
            }
            else
            {
                if (graphics != null)
                {
                    graphics.SetResolution("2560x1440"); // 2560x1440
                }
            }
        }

        if (PlayerPrefs.HasKey("masterCameraShake"))
        {
            int cameraShakeInt = PlayerPrefs.GetInt("masterCameraShake");
            bool isCameraShake = cameraShakeInt == 1;

            if (graphics != null)
            {
                graphics.SetCameraShake(isCameraShake);
            }
        }

        if (PlayerPrefs.HasKey("masterFPS"))
        {
            int localFPS = PlayerPrefs.GetInt("masterFPS");
            Application.targetFrameRate = localFPS;
        }

        if (PlayerPrefs.HasKey("masterMotionBlur"))
        {
            int motionBlurInt = PlayerPrefs.GetInt("masterMotionBlur");
            bool isMotionBlur = motionBlurInt == 1;

            if (graphics != null)
            {
                graphics.SetMotionBlur(isMotionBlur);
            }
        }

        if (PlayerPrefs.HasKey("masterBrightness"))
        {
            float localBrightness = PlayerPrefs.GetFloat("masterBrightness");
            if (brightnessSlider) brightnessSlider.value = localBrightness;

            float defaultBrightness = fallbackDefaultBrightness;
            if (graphics != null)
            {
                defaultBrightness = graphics.DefaultBrightness;
                graphics.SetBrightness(localBrightness);
            }
            else
            {
                BrightnessOverlayController.Instance?.ApplyBrightness(localBrightness, defaultBrightness);
            }
        }
    }
}
