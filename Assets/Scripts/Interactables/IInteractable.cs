using UnityEngine;
public interface IInteractable
{
    public string interactId { get; set; }
    public AnimationClip interactAnimation { get; set; }
    public bool showHitbox { get; set; }
    public bool isPlayerNearby { get; set; }

    public void DeactivateInteractable(MonoBehaviour interactable);
    public void OnInteractButtonPressed();
}
