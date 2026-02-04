using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(BoxCollider))]
public abstract class InteractionManager : MonoBehaviour, IInteractable
{
    // IInteractable implementation
    public string interactId { get => _interactId; set => _interactId = value; }
    public AnimationClip interactAnimation { get => _interactAnimation; set => _interactAnimation = value; }
    public bool showHitbox { get => _showHitbox; set => _showHitbox = value; }
    public bool isPlayerNearby { get; set; }

    [Header("Debugging")]
    [SerializeField] private bool _showHitbox;

    [Space(10)]
    [Header("Interaction Animation and ID")]
    [SerializeField] private AnimationClip _interactAnimation;
    [SerializeField] private string _interactId;
    
    [Space(10)]
    [Header("Input Action Reference")]
    [SerializeField] internal InputActionReference _interactInputAction;

    protected virtual void Awake()
    {
        this.GetComponent<BoxCollider>().isTrigger = true;

        interactId = _interactId.Trim().ToLowerInvariant();

        

        if(InteractionUI.Instance._interactText != null)
            InteractionUI.Instance._interactText.gameObject.SetActive(false);
        if(InteractionUI.Instance._interactIcon != null)
            InteractionUI.Instance._interactIcon.gameObject.SetActive(false);
    }
    public void OnInteractButtonPressed()
    {
        if (isPlayerNearby)
        {
            if(_interactInputAction != null && _interactInputAction.action != null && _interactInputAction.action.triggered)
            {
                Interact();
                InteractionUI.Instance._interactEffect?.Play();
            }
        }
    }

    private void Update()
    {
        OnInteractButtonPressed();
    }

    protected abstract void Interact();
    public bool IsUsingKeyboard()
    {
        var scheme = InputReader.Instance != null ? InputReader.Instance.activeControlScheme ?? string.Empty : string.Empty;
        return scheme.IndexOf("keyboard", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public void DeactivateInteractable(MonoBehaviour interactable)
    {
        if (interactable == null)
        {
            return;
        }

        // Disable interaction on the provided interactable object, not the manager itself.
        var collider = interactable.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        interactable.gameObject.SetActive(false);

        if(InteractionUI.Instance._interactIcon != null)
            InteractionUI.Instance._interactIcon.gameObject.SetActive(false);

        if(InteractionUI.Instance._interactText != null)
            InteractionUI.Instance._interactText.gameObject.SetActive(false);
    }
    
    public void SwapBasedOnInputMethod()
    {
        if (_interactInputAction == null || _interactInputAction.action == null || _interactInputAction.action.controls.Count == 0)
        {
            // Fallback if input action is not properly configured
            if(InteractionUI.Instance._interactText != null)
            {
                InteractionUI.Instance._interactText.text = "Press to interact";
                InteractionUI.Instance._interactText.gameObject.SetActive(true);
            }
            if(InteractionUI.Instance._interactIcon != null)
                InteractionUI.Instance._interactIcon.gameObject.SetActive(false);
            return;
        }

        if (IsUsingKeyboard())
        {
            if(InteractionUI.Instance._interactText != null)
            {
                InteractionUI.Instance._interactText.text = $"Press {(_interactInputAction.action.controls[0].name).ToUpperInvariant()} to interact";
                InteractionUI.Instance._interactText.gameObject.SetActive(true);
            }
            if(InteractionUI.Instance._interactIcon != null)
                InteractionUI.Instance._interactIcon.gameObject.SetActive(false);
        }
        else
        {
            string gamePadButtonName = _interactInputAction.action.controls[0].name;
            if(InteractionUI.Instance._interactIcon != null)
                InteractionUI.Instance._interactIcon.gameObject.SetActive(true);

            foreach(var iconEntry in SettingsManager.Instance.gamePadIcons)
            {
                if(iconEntry.Key == gamePadButtonName)
                {
                    if(InteractionUI.Instance._interactIcon != null)
                        InteractionUI.Instance._interactIcon.sprite = iconEntry.Value;
                    break;
                }
            }
            if(InteractionUI.Instance._interactText != null)
            {
                InteractionUI.Instance._interactText.text = "Press \n\n to interact";
                InteractionUI.Instance._interactText.gameObject.SetActive(true);
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {

            isPlayerNearby = true;

            SwapBasedOnInputMethod();

            if(InteractionUI.Instance != null)
            {
                if(InteractionUI.Instance._interactText != null)
                {
                    InteractionUI.Instance._interactText.gameObject.SetActive(true);
                    if(InteractionUI.Instance._interactText.transform.parent != null)
                        InteractionUI.Instance._interactText.transform.parent.gameObject.SetActive(true);
                }
                if(InteractionUI.Instance._interactIcon != null && !IsUsingKeyboard())
                {
                    InteractionUI.Instance._interactIcon.gameObject.SetActive(true);
                    if(InteractionUI.Instance._interactIcon.transform.parent != null)
                        InteractionUI.Instance._interactIcon.transform.parent.gameObject.SetActive(true);
                }
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if(InteractionUI.Instance._interactText != null)
                InteractionUI.Instance._interactText.gameObject.SetActive(false);

            if(InteractionUI.Instance._interactIcon != null)
                InteractionUI.Instance._interactIcon.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        if(_showHitbox)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            BoxCollider box = GetComponent<BoxCollider>();
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
