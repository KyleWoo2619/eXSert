using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputBootstrap : MonoBehaviour
{
    void Awake()
    {
        var pi = GetComponent<PlayerInput>();

        // Clone so this object has its own instance (no singleton side effects)
        pi.actions = Instantiate(pi.actions);

        // Disable everything, then enable only the map you want
        pi.actions.Disable();
        pi.SwitchCurrentActionMap("Gameplay");  // enables just Gameplay
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
