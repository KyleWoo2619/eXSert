using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/*
Written by Kyle Woo
Manages the cursor visibility and lock state based on the current input scheme and action map.
*/

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
public class CursorBySchemeAndMap : MonoBehaviour
{
    [SerializeField] private string[] uiActionMapNames = new[] { "UI", "Menu" };
    [SerializeField] private string loadingActionMapName = "Loading";
    [SerializeField] private string[] keyboardMouseSchemeNames = new[] { "Keyboard&Mouse", "KeyboardMouse" };
    [SerializeField] private string[] forceShowCursorScenes = new[] { "MainMenu" };
    [SerializeField] private CursorLockMode lockModeWhenHidden = CursorLockMode.Locked;

    private static bool forceHidden;
    private static readonly List<CursorBySchemeAndMap> Instances = new();

    private PlayerInput playerInput;
    private string lastMap;
    private string lastScheme;

    public static void SetForceHidden(bool hidden)
    {
        forceHidden = hidden;
        foreach (var instance in Instances)
        {
            if (instance != null)
                instance.ApplyCursorPolicy();
        }
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        CacheState();
        ApplyCursorPolicy();

        if (playerInput != null)
            playerInput.onControlsChanged += HandleControlsChanged;
    }

    private void OnDestroy()
    {
        if (playerInput != null)
            playerInput.onControlsChanged -= HandleControlsChanged;
        Instances.Remove(this);
    }

    private void OnEnable()
    {
        if (!Instances.Contains(this))
            Instances.Add(this);
    }

    private void OnDisable()
    {
        Instances.Remove(this);
    }

    private void Update()
    {
        // Detect manual SwitchCurrentActionMap calls
        var mapName = playerInput != null && playerInput.currentActionMap != null ? playerInput.currentActionMap.name : string.Empty;
        if (!string.Equals(mapName, lastMap, System.StringComparison.Ordinal))
            OnControlsOrMapPossiblyChanged();
    }

    private void HandleControlsChanged(PlayerInput _)
    {
        OnControlsOrMapPossiblyChanged();
    }

    private void OnControlsOrMapPossiblyChanged()
    {
        CacheState();
        ApplyCursorPolicy();
    }

    private void CacheState()
    {
        lastMap = playerInput != null && playerInput.currentActionMap != null ? playerInput.currentActionMap.name : string.Empty;
        lastScheme = playerInput != null ? playerInput.currentControlScheme : string.Empty;
    }

    private void ApplyCursorPolicy()
    {
        if (forceHidden)
        {
            HideCursor();
            return;
        }

        if (IsSceneForcedVisible())
        {
            ShowCursor();
            return;
        }

        bool inLoading = !string.IsNullOrEmpty(loadingActionMapName)
            && string.Equals(lastMap, loadingActionMapName, System.StringComparison.OrdinalIgnoreCase);

        if (inLoading)
        {
            HideCursor();
            return;
        }

        bool inUI = IsMatch(lastMap, uiActionMapNames);
        bool onKeyboardMouse = IsMatch(lastScheme, keyboardMouseSchemeNames);

        if (inUI && onKeyboardMouse)
            ShowCursor();
        else
            HideCursor();
    }

    private static bool IsMatch(string value, string[] candidates)
    {
        if (string.IsNullOrEmpty(value) || candidates == null)
            return false;

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate))
                continue;
            if (string.Equals(value, candidate, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool IsSceneForcedVisible()
    {
        if (forceShowCursorScenes == null || forceShowCursorScenes.Length == 0)
            return false;

        string sceneName = SceneManager.GetActiveScene().name;
        return IsMatch(sceneName, forceShowCursorScenes);
    }

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
