using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Singletons;

/// <summary>
/// Adjusts a persistent URP Volume's Lift/Gamma/Gain overrides to simulate brightness changes.
/// Attach this to the shared Global Volume so every scene can call <see cref="ApplyBrightness"/>.
/// </summary>
[RequireComponent(typeof(Volume))]
[DisallowMultipleComponent]
public class BrightnessOverlayController : Singleton<BrightnessOverlayController>
{
    [Header("Volume References")]
    [SerializeField]
    [Tooltip("Explicit volume reference (auto-assigned when left empty).")]
    private Volume targetVolume;

    private VolumeProfile runtimeProfile;
    private LiftGammaGain liftGammaGain;

    [SerializeField]
    [Tooltip("Emit debug information whenever brightness is applied.")]
    private bool logChanges;

    [Header("Gamma Curve")]
    [SerializeField]
    [Tooltip("Gamma value when the slider is at its minimum.")]
    private float minGamma = 0.5f;

    [SerializeField]
    [Tooltip("Gamma value when the slider is at its maximum.")]
    private float maxGamma = 1.2f;

    [Header("Slider Bounds")]
    [SerializeField]
    [Tooltip("Highest brightness slider value (used for normalization).")]
    private float maxSliderValue = 3f;
    [SerializeField]
    [Tooltip("Lowest brightness slider value (used for normalization).")]
    private float minSliderValue = 0f;

    protected override void Awake()
    {
        base.Awake();

        EnsureVolumeReferences();
    }

    /// <summary>
    /// Applies brightness adjustments by scaling the Lift/Gamma/Gain override.
    /// </summary>
    public void ApplyBrightness(float brightness, float defaultBrightness)
    {
        if (!EnsureVolumeReferences())
            return;

        if (Mathf.Approximately(defaultBrightness, 0f))
            defaultBrightness = 0.01f;

        float sliderMin = minSliderValue;
        float sliderMax = Mathf.Max(maxSliderValue, sliderMin + 0.0001f);

        float normalizedBrightness = Mathf.InverseLerp(
            sliderMin,
            sliderMax,
            Mathf.Clamp(brightness, sliderMin, sliderMax));

        float gammaValue = Mathf.Lerp(minGamma, maxGamma, normalizedBrightness);

        liftGammaGain.gamma.value = new Vector4(gammaValue, gammaValue, gammaValue, 0f);
        liftGammaGain.active = true;

        if (logChanges)
        {
            Debug.Log(
                $"[BrightnessOverlayController] Brightness:{brightness:F3} -> Gamma:{gammaValue:F3}");
        }
    }

    private void EnsureOverridesEnabled()
    {
        if (liftGammaGain == null)
            return;

        liftGammaGain.gamma.overrideState = true;
    }

    private bool EnsureVolumeReferences()
    {
        targetVolume ??= GetComponent<Volume>();
        if (targetVolume == null)
        {
            Debug.LogError(
                "[BrightnessOverlayController] Missing Volume reference. Brightness adjustments disabled.");
            return false;
        }

        runtimeProfile = targetVolume.profile ?? targetVolume.sharedProfile ?? runtimeProfile;
        if (runtimeProfile == null)
        {
            Debug.LogError(
                "[BrightnessOverlayController] Volume profile missing. Assign a profile to the Global Volume.");
            return false;
        }

        if (liftGammaGain == null && !runtimeProfile.TryGet(out liftGammaGain))
        {
            Debug.LogError(
                "[BrightnessOverlayController] Volume profile lacks a LiftGammaGain override. Add one to enable brightness control.");
            return false;
        }

        EnsureOverridesEnabled();
        return true;
    }

}


