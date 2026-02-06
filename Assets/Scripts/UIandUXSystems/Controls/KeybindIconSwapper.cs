using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class KeybindIconSwapper : MonoBehaviour
{
    public enum DeviceMode
    {
        Auto,
        KeyboardMouse,
        Gamepad
    }

    [Header("References")]
    [SerializeField] private KeybindIconSet iconSet;
    [SerializeField] private Image targetImage;

    [Header("Binding")]
    [SerializeField] private KeybindAction action;
    [SerializeField] private DeviceMode deviceMode = DeviceMode.Auto;

    [Header("Behavior")]
    [SerializeField] private bool hideWhenMissing = false;

    private string lastScheme = string.Empty;
    private bool isSubscribed;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += HandleActionChange;
        SubscribeToPlayerInput();
        RefreshIcon();
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= HandleActionChange;
        UnsubscribeFromPlayerInput();
    }

    private void Update()
    {
        if (deviceMode != DeviceMode.Auto)
            return;

        string scheme = GetCurrentScheme();
        if (!string.Equals(scheme, lastScheme))
        {
            lastScheme = scheme;
            RefreshIcon();
        }
    }

    private void HandleActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.BoundControlsChanged)
            return;

        RefreshIcon();
    }

    private void HandleControlsChanged(PlayerInput _)
    {
        if (deviceMode == DeviceMode.Auto)
            RefreshIcon();
    }

    private void SubscribeToPlayerInput()
    {
        if (isSubscribed)
            return;

        if (InputReader.PlayerInput != null)
        {
            InputReader.PlayerInput.onControlsChanged += HandleControlsChanged;
            isSubscribed = true;
        }
    }

    private void UnsubscribeFromPlayerInput()
    {
        if (!isSubscribed)
            return;

        if (InputReader.PlayerInput != null)
            InputReader.PlayerInput.onControlsChanged -= HandleControlsChanged;
        isSubscribed = false;
    }

    private string GetCurrentScheme()
    {
        if (InputReader.Instance != null)
            return InputReader.activeControlScheme ?? string.Empty;

        if (InputReader.PlayerInput != null)
            return InputReader.PlayerInput.currentControlScheme ?? string.Empty;

        return string.Empty;
    }

    private void RefreshIcon()
    {
        if (iconSet == null || targetImage == null)
            return;

        bool useGamepad = deviceMode == DeviceMode.Gamepad;
        if (deviceMode == DeviceMode.Auto)
            useGamepad = iconSet.IsGamepadScheme(GetCurrentScheme());

        if (iconSet.TryGetIcon(action, useGamepad, out Sprite icon, out _))
        {
            targetImage.sprite = icon;
            targetImage.enabled = true;
        }
        else if (hideWhenMissing)
        {
            targetImage.enabled = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (!Application.isPlaying)
            return;

        RefreshIcon();
    }
#endif
}
