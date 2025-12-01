using UnityEngine;

/// <summary>
/// Randomly flickers a light on and off with configurable timing.
/// Attach to a GameObject with a Light component.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    [SerializeField, Tooltip("Minimum time the light stays on (seconds)")]
    private float minOnTime = 0.1f;
    
    [SerializeField, Tooltip("Maximum time the light stays on (seconds)")]
    private float maxOnTime = 0.5f;
    
    [SerializeField, Tooltip("Minimum time the light stays off (seconds)")]
    private float minOffTime = 0.05f;
    
    [SerializeField, Tooltip("Maximum time the light stays off (seconds)")]
    private float maxOffTime = 0.3f;
    
    [Header("Intensity Variation (Optional)")]
    [SerializeField, Tooltip("Enable random intensity variations when light is on")]
    private bool varyIntensity = false;
    
    [SerializeField, Tooltip("Minimum intensity multiplier (0-1)")]
    [Range(0f, 1f)]
    private float minIntensityMultiplier = 0.5f;
    
    [SerializeField, Tooltip("Maximum intensity multiplier (0-1)")]
    [Range(0f, 1f)]
    private float maxIntensityMultiplier = 1f;
    
    [Header("Flicker Probability")]
    [SerializeField, Tooltip("Chance the light will flicker off (0-1). 1 = always flickers, 0 = never flickers")]
    [Range(0f, 1f)]
    private float flickerChance = 1f;
    
    [Header("Material Emission (Optional)")]
    [SerializeField, Tooltip("Renderer to control emission on (leave empty to auto-find on this object)")]
    private Renderer targetRenderer;
    
    [SerializeField, Tooltip("Material index to control emission on (if renderer has multiple materials)")]
    private int materialIndex = 0;
    
    [SerializeField, Tooltip("Enable/disable emission based on light state")]
    private bool controlEmission = true;
    
    private Light lightComponent;
    private float baseIntensity;
    private float nextFlickerTime;
    private bool isLightOn = true;
    private Material targetMaterial;
    private bool hasEmission = false;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private Color baseEmissionColor;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        baseIntensity = lightComponent.intensity;
        
        // Setup emission control
        if (controlEmission)
        {
            SetupEmissionControl();
        }
        
        // Schedule first flicker
        ScheduleNextFlicker();
    }

    private void SetupEmissionControl()
    {
        // Find renderer if not assigned
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null && targetRenderer.materials.Length > materialIndex)
        {
            // Get the material instance
            targetMaterial = targetRenderer.materials[materialIndex];
            
            // Check if material has emission
            if (targetMaterial.HasProperty(EmissionColor))
            {
                hasEmission = true;
                baseEmissionColor = targetMaterial.GetColor(EmissionColor);
                
                // Enable emission keyword if not already enabled
                if (baseEmissionColor != Color.black)
                {
                    targetMaterial.EnableKeyword("_EMISSION");
                }
            }
            else
            {
                Debug.LogWarning($"[LightFlicker] Material on {gameObject.name} does not have an _EmissionColor property.");
            }
        }
        else if (targetRenderer == null)
        {
            Debug.LogWarning($"[LightFlicker] No Renderer found on {gameObject.name}. Emission control disabled.");
        }
    }

    private void Update()
    {
        if (Time.time >= nextFlickerTime)
        {
            ToggleLight();
            ScheduleNextFlicker();
        }
    }

    private void ToggleLight()
    {
        // If light is currently on, decide whether to flicker it off
        if (isLightOn)
        {
            if (Random.value <= flickerChance)
            {
                // Turn off
                lightComponent.enabled = false;
                isLightOn = false;
                
                // Turn off emission
                if (controlEmission && hasEmission && targetMaterial != null)
                {
                    targetMaterial.SetColor(EmissionColor, Color.black);
                    targetMaterial.DisableKeyword("_EMISSION");
                }
            }
            else
            {
                // Keep on but maybe vary intensity
                if (varyIntensity)
                {
                    float multiplier = Random.Range(minIntensityMultiplier, maxIntensityMultiplier);
                    lightComponent.intensity = baseIntensity * multiplier;
                    
                    // Vary emission intensity too
                    if (controlEmission && hasEmission && targetMaterial != null)
                    {
                        targetMaterial.SetColor(EmissionColor, baseEmissionColor * multiplier);
                    }
                }
            }
        }
        else
        {
            // Turn back on
            lightComponent.enabled = true;
            isLightOn = true;
            
            float multiplier = 1f;
            
            if (varyIntensity)
            {
                multiplier = Random.Range(minIntensityMultiplier, maxIntensityMultiplier);
                lightComponent.intensity = baseIntensity * multiplier;
            }
            else
            {
                lightComponent.intensity = baseIntensity;
            }
            
            // Turn on emission
            if (controlEmission && hasEmission && targetMaterial != null)
            {
                targetMaterial.SetColor(EmissionColor, baseEmissionColor * multiplier);
                targetMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    private void ScheduleNextFlicker()
    {
        float delay;
        
        if (isLightOn)
        {
            // Light is on, schedule time until next potential off
            delay = Random.Range(minOnTime, maxOnTime);
        }
        else
        {
            // Light is off, schedule time until it turns back on
            delay = Random.Range(minOffTime, maxOffTime);
        }
        
        nextFlickerTime = Time.time + delay;
    }

    // Public methods for runtime control
    public void SetFlickerChance(float chance)
    {
        flickerChance = Mathf.Clamp01(chance);
    }

    public void EnableFlicker(bool enable)
    {
        enabled = enable;
        if (!enable)
        {
            lightComponent.enabled = true;
            lightComponent.intensity = baseIntensity;
            isLightOn = true;
            
            // Restore emission
            if (controlEmission && hasEmission && targetMaterial != null)
            {
                targetMaterial.SetColor(EmissionColor, baseEmissionColor);
                targetMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up material instance if we created one
        if (targetMaterial != null && targetRenderer != null)
        {
            // Note: Unity automatically cleans up material instances from renderer.materials
        }
    }

    private void OnValidate()
    {
        // Ensure max values are greater than min values
        if (maxOnTime < minOnTime)
            maxOnTime = minOnTime;
        
        if (maxOffTime < minOffTime)
            maxOffTime = minOffTime;
        
        if (maxIntensityMultiplier < minIntensityMultiplier)
            maxIntensityMultiplier = minIntensityMultiplier;
    }
}
