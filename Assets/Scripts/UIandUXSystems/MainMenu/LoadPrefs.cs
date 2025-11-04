using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SocialPlatforms;
public class LoadPrefs : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool canUse = false;
    private AudioSettings sound;
    private GraphicsSettings graphics;
    private GeneralSettings general;
    

    [Header("Audio Settings")]
    [SerializeField] private TMP_Text masterVolumeTextValue = null;
    [SerializeField] Slider masterVolumeSlider = null;
    [SerializeField] private TMP_Text musicVolumeTextValue = null;
    [SerializeField] Slider musicVolumeSlider = null;
    [SerializeField] private TMP_Text sfxVolumeTextValue = null;
    [SerializeField] Slider sfxVolumeSlider = null;
    [SerializeField] private TMP_Text voiceVolumeTextValue = null;
    [SerializeField] Slider voiceVolumeSlider = null;

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Text brightnessTextValue = null;
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle motionBlurToggle;
    [SerializeField] private Toggle cameraShakeToggle;
    [SerializeField] private TMP_Dropdown fpsDropdown;

    [Header("General Settings")]
    [SerializeField] private TMP_Text sensTextValue = null;
    [SerializeField] private Slider sensSlider = null;
    [SerializeField] private Toggle invertY;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private TMP_Text vibrationTextValue = null;
    [SerializeField] private Slider vibrationSlider;

    private void Awake()
    {
        if (canUse)
        {
            if (PlayerPrefs.HasKey("masterVolume"))
            {
                float masterVolume = PlayerPrefs.GetFloat("masterVolume");

                if (masterVolumeTextValue) masterVolumeTextValue.text = masterVolume.ToString("0.0");
                if (masterVolumeSlider) masterVolumeSlider.value = masterVolume;
                if (SoundManager.Instance != null && SoundManager.Instance.masterSource)
                    SoundManager.Instance.masterSource.volume = masterVolume;

                float musicVolume = PlayerPrefs.GetFloat("musicVolume");

                if (musicVolumeTextValue) musicVolumeTextValue.text = musicVolume.ToString("0.0");
                if (musicVolumeSlider) musicVolumeSlider.value = musicVolume;
                if (SoundManager.Instance != null && SoundManager.Instance.musicSource)
                    SoundManager.Instance.musicSource.volume = musicVolume;

                float sfxVolume = PlayerPrefs.GetFloat("sfxVolume");

                if (sfxVolumeTextValue) sfxVolumeTextValue.text = sfxVolume.ToString("0.0");
                if (sfxVolumeSlider) sfxVolumeSlider.value = sfxVolume;
                if (SoundManager.Instance != null && SoundManager.Instance.sfxSource)
                    SoundManager.Instance.sfxSource.volume = sfxVolume;

                float voiceVolume = PlayerPrefs.GetFloat("voiceVolume");

                if (voiceVolumeTextValue) voiceVolumeTextValue.text = voiceVolume.ToString("0.0");
                if (voiceVolumeSlider) voiceVolumeSlider.value = voiceVolume;
                if (SoundManager.Instance != null && SoundManager.Instance.voiceSource)
                    SoundManager.Instance.voiceSource.volume = voiceVolume;
                
            } 
            else
            {
                if (sound != null) sound.ResetButton();
            }

            if (PlayerPrefs.HasKey("masterQuality"))
            {
                int localQuality = PlayerPrefs.GetInt("masterQuality");
                if (qualityDropdown) qualityDropdown.value = localQuality;
                QualitySettings.SetQualityLevel(localQuality);
            }
            else
            {
                if (graphics != null) graphics.ResetButton();
            }

            if (PlayerPrefs.HasKey("masterFPS"))
            {
                int localFPS = PlayerPrefs.GetInt("masterFPS");
                if (fpsDropdown) fpsDropdown.value = localFPS;
                Application.targetFrameRate = localFPS;
            }

            }

            if (PlayerPrefs.HasKey("masterMotionBlur"))
            {
                
            }

            if (PlayerPrefs.HasKey("masterBrightness"))
            {
                float localBrightness = PlayerPrefs.GetFloat("masterBrightness");
                if (brightnessTextValue) brightnessTextValue.text = localBrightness.ToString("0.0");
                if (brightnessSlider) brightnessSlider.value = localBrightness;
            }

            if (PlayerPrefs.HasKey("masterSens"))
            {
                float localSens = PlayerPrefs.GetFloat("masterSens");
                if (sensSlider) sensSlider.value = localSens;
            }
            else
            {
                if (general != null) general.ResetButton();
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
    }
