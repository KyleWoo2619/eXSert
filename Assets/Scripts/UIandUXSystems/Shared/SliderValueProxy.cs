using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Mirrors a working slider's value onto a visual-only slider and exposes optional controller input.
/// Attach this to the interactive slider (the one receiving pointer/controller events).
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderValueProxy : MonoBehaviour
{
    [Header("Visual Slider (Optional)")]
    [SerializeField, Tooltip("Slider that visually mirrors the real slider's value.")]
    private Slider visualSlider;
    [SerializeField, Tooltip("When enabled, the visual slider is forced non-interactable.")]
    private bool disableVisualInteraction = true;

    [Header("Controller Input")]
    [SerializeField, Tooltip("Input action triggered by RB / Right Bumper (or any 'increase' binding).")]
    private InputActionReference increaseAction;
    [SerializeField, Tooltip("Input action triggered by LB / Left Bumper (or any 'decrease' binding).")]
    private InputActionReference decreaseAction;
    [SerializeField, Range(0.01f, 1f), Tooltip("Step size as a percentage of the slider's range when using controller input.")]
    private float controllerStepNormalized = 0.1f;

    private Slider sourceSlider;

    private void Awake()
    {
        sourceSlider = GetComponent<Slider>();
        if (visualSlider != null)
        {
            if (disableVisualInteraction)
                visualSlider.interactable = false;
            visualSlider.minValue = sourceSlider.minValue;
            visualSlider.maxValue = sourceSlider.maxValue;
            visualSlider.wholeNumbers = sourceSlider.wholeNumbers;
            visualSlider.value = sourceSlider.value;
        }
    }

    private void OnEnable()
    {
        if (sourceSlider != null)
            sourceSlider.onValueChanged.AddListener(SyncVisualSlider);

        SubscribeInput(increaseAction, OnIncreasePerformed);
        SubscribeInput(decreaseAction, OnDecreasePerformed);
    }

    private void OnDisable()
    {
        if (sourceSlider != null)
            sourceSlider.onValueChanged.RemoveListener(SyncVisualSlider);

        UnsubscribeInput(increaseAction, OnIncreasePerformed);
        UnsubscribeInput(decreaseAction, OnDecreasePerformed);
    }

    private void SyncVisualSlider(float value)
    {
        if (visualSlider != null)
            visualSlider.value = value;
    }

    private void SubscribeInput(InputActionReference actionReference, Action<InputAction.CallbackContext> handler)
    {
        if (actionReference == null || actionReference.action == null)
            return;

        actionReference.action.performed += handler;
        if (!actionReference.action.enabled)
            actionReference.action.Enable();
    }

    private void UnsubscribeInput(InputActionReference actionReference, Action<InputAction.CallbackContext> handler)
    {
        if (actionReference == null || actionReference.action == null)
            return;

        actionReference.action.performed -= handler;
    }

    private void OnIncreasePerformed(InputAction.CallbackContext context)
    {
        AdjustSlider(1f);
    }

    private void OnDecreasePerformed(InputAction.CallbackContext context)
    {
        AdjustSlider(-1f);
    }

    private void AdjustSlider(float direction)
    {
        if (sourceSlider == null)
            return;

        float range = sourceSlider.maxValue - sourceSlider.minValue;
        float stepSize = range * controllerStepNormalized;

        if (sourceSlider.wholeNumbers)
            stepSize = Mathf.Max(1f, Mathf.Round(stepSize));

        float newValue = Mathf.Clamp(sourceSlider.value + (stepSize * direction), sourceSlider.minValue, sourceSlider.maxValue);
        sourceSlider.value = sourceSlider.wholeNumbers ? Mathf.Round(newValue) : newValue;
    }
}
