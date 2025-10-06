using UnityEngine;
using UnityEngine.InputSystem;

/*
Written by Kyle Woo
Manages the cursor visibility and lock state based on the current input scheme and action map.
*/

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public class CursorBySchemeAndMap : MonoBehaviour
{
    [SerializeField] private string uiActionMapName = "UI";
    [SerializeField] private string gameplayActionMapName = "Gameplay";
    [SerializeField] private string keyboardMouseSchemeName = "Keyboard&Mouse"; // match your scheme name
    [SerializeField] private CursorLockMode lockModeWhenHidden = CursorLockMode.Locked;

    private PlayerInput playerInput;
    private string lastMap;
    private string lastScheme;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        lastMap = playerInput.currentActionMap != null ? playerInput.currentActionMap.name : "";
        lastScheme = playerInput.currentControlScheme;
        ApplyCursorPolicy();
        
        // Update when devices change (keyboard ↔ gamepad swap)
        playerInput.onControlsChanged += _ => OnControlsOrMapPossiblyChanged();
    }

    void OnDestroy()
    {
        if (playerInput != null)
            playerInput.onControlsChanged -= _ => OnControlsOrMapPossiblyChanged();
    }

    void Update()
    {
        // Detect manual SwitchCurrentActionMap calls
        var mapName = playerInput.currentActionMap != null ? playerInput.currentActionMap.name : "";
        if (mapName != lastMap)
            OnControlsOrMapPossiblyChanged();
    }

    private void OnControlsOrMapPossiblyChanged()
    {
        lastMap = playerInput.currentActionMap != null ? playerInput.currentActionMap.name : "";
        lastScheme = playerInput.currentControlScheme;
        ApplyCursorPolicy();
    }

    private void ApplyCursorPolicy()
    {
        bool inUI = playerInput.currentActionMap != null &&
                    playerInput.currentActionMap.name == uiActionMapName;

        bool onKeyboardMouse = !string.IsNullOrEmpty(playerInput.currentControlScheme) &&
                               playerInput.currentControlScheme == keyboardMouseSchemeName;

        if (inUI && onKeyboardMouse)
            ShowCursor();
        else
            HideCursor();
    }

    // Public helpers if you want to call them elsewhere
    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = lockModeWhenHidden;
    }
}
