using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// Attach this to any menu root that should react to UI/Cancel (Esc/B).
/// It listens for the EventSystem's Cancel event and invokes the assigned event.
/// Note: MenuListManager also handles back button input via InputActionReference.
public sealed class MenuCancelToBack : MonoBehaviour, ICancelHandler
{
    [Tooltip("Called when UI/Cancel (Esc/B) is pressed while this menu is active.")]
    public UnityEvent onCancelEvent;

    public void OnCancel(BaseEventData eventData)
    {
        onCancelEvent?.Invoke();
    }
}
