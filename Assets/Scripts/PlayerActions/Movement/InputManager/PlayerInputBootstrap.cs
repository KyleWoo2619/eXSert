using UnityEngine;
using UnityEngine.InputSystem;
using eXsert;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputBootstrap : MonoBehaviour
{
    void Awake()
    {
        var pi = GetComponent<PlayerInput>();
        var runtimeControls = default(PlayerControls);

        var sourceAsset = pi.actions;
        if (sourceAsset == null)
        {
            runtimeControls = new PlayerControls();
            sourceAsset = runtimeControls.asset;

            if (sourceAsset == null)
            {
                Debug.LogError("[PlayerInputBootstrap] PlayerInput has no assigned InputActionAsset and no fallback could be created.");
                return;
            }
        }

        // Clone so this object has its own instance (no singleton side effects)
        pi.actions = Instantiate(sourceAsset);
        runtimeControls?.Dispose();

        // Disable everything, then enable only the map you want
        pi.actions.Disable();
        pi.SwitchCurrentActionMap("Gameplay"); // Enables just Gameplay

        // Allow Unity to auto-switch between control schemes (keyboard/controller) on the fly
        pi.neverAutoSwitchControlSchemes = false;
    }

    public void EnterUI(PlayerInput pi)
    {
        pi.SwitchCurrentActionMap("UI");
    }

    public void ExitUI(PlayerInput pi)
    {
        pi.SwitchCurrentActionMap("Gameplay");
    }

}
