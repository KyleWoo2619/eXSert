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

                masterVolumeTextValue.text = masterVolume.ToString("0.0");
                masterVolumeSlider.value = masterVolume;
                SoundManager.Instance.masterSource.volume = masterVolume;

                float musicVolume = PlayerPrefs.GetFloat("musicVolume");

                musicVolumeTextValue.text = musicVolume.ToString("0.0");
                musicVolumeSlider.value = musicVolume;
                SoundManager.Instance.musicSource.volume = musicVolume;

                float sfxVolume = PlayerPrefs.GetFloat("sfxVolume");

                sfxVolumeTextValue.text = sfxVolume.ToString("0.0");
                sfxVolumeSlider.value = sfxVolume;
                SoundManager.Instance.sfxSource.volume = sfxVolume;

                float voiceVolume = PlayerPrefs.GetFloat("voiceVolume");

                voiceVolumeTextValue.text = voiceVolume.ToString("0.0");
                voiceVolumeSlider.value = voiceVolume;
                SoundManager.Instance.voiceSource.volume = voiceVolume;
                
            } 
            else
            {
                sound.ResetButton();
            }

            if (PlayerPrefs.HasKey("masterQuality"))
            {
                int localQuality = PlayerPrefs.GetInt("masterQuality");
                qualityDropdown.value = localQuality;
                QualitySettings.SetQualityLevel(localQuality);
            }
            else
            {
                graphics.ResetButton();
            }

            if (PlayerPrefs.HasKey("masterFPS"))
            {
                int localFPS = PlayerPrefs.GetInt("masterFPS");
                fpsDropdown.value = localFPS;
                Application.targetFrameRate = localFPS;
            }

            }

            if (PlayerPrefs.HasKey("masterMotionBlur"))
            {
                
            }

            if (PlayerPrefs.HasKey("masterBrightness"))
            {
                float localBrightness = PlayerPrefs.GetFloat("masterBrightness");
                brightnessTextValue.text = localBrightness.ToString("0.0");
                brightnessSlider.value = localBrightness;
            }

            if (PlayerPrefs.HasKey("masterSens"))
            {
                float localSens = PlayerPrefs.GetFloat("masterSens");
                sensTextValue.text = localSens.ToString("0.0");
                sensSlider.value = localSens;
            }
            else
            {
                general.ResetButton();
            }

            if (PlayerPrefs.HasKey("masterInvertY"))
            {
                int localInvert = PlayerPrefs.GetInt("masterInvertY");

                if (localInvert == 1)
                {
                    CameraSettingsManager.Instance.invertY = true;
                }
                else
                {
                    CameraSettingsManager.Instance.invertY = false;
                }
            }
        }
    }
