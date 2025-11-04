using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks player combat actions (light attack, heavy attack, stance changes)
/// and reports them to the ObjectiveManager for tutorial progression.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerActionTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Player's InputReader component")]
    private InputReader inputReader;

    private PlayerInput playerInput;
    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private InputAction changeStanceAction;

    private void Start()
    {
        // Try to find InputReader if not assigned
        if (inputReader == null)
        {
            inputReader = InputReader.Instance;
            if (inputReader == null)
            {
                Debug.LogError("[PlayerActionTracker] No InputReader found!");
                return;
            }
        }

        // Get PlayerInput from InputReader
        playerInput = inputReader._playerInput;
        
        if (playerInput != null)
        {
            // Get input actions
            lightAttackAction = playerInput.actions["LightAttack"];
            heavyAttackAction = playerInput.actions["HeavyAttack"];
            changeStanceAction = playerInput.actions["ChangeStance"];

            // Subscribe to input action events
            if (lightAttackAction != null)
                lightAttackAction.performed += OnLightAttackPerformed;
            
            if (heavyAttackAction != null)
                heavyAttackAction.performed += OnHeavyAttackPerformed;
            
            if (changeStanceAction != null)
                changeStanceAction.performed += OnStanceChangePerformed;

            Debug.Log("[PlayerActionTracker] Subscribed to input actions");
        }
        else
        {
            Debug.LogError("[PlayerActionTracker] PlayerInput not found in InputReader!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from input action events
        if (lightAttackAction != null)
            lightAttackAction.performed -= OnLightAttackPerformed;
        
        if (heavyAttackAction != null)
            heavyAttackAction.performed -= OnHeavyAttackPerformed;
        
        if (changeStanceAction != null)
            changeStanceAction.performed -= OnStanceChangePerformed;
    }

    // ========== Input Event Handlers ==========

    private void OnLightAttackPerformed(InputAction.CallbackContext context)
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.RegisterLightAttack();
        }
    }

    private void OnHeavyAttackPerformed(InputAction.CallbackContext context)
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.RegisterHeavyAttack();
        }
    }

    private void OnStanceChangePerformed(InputAction.CallbackContext context)
    {
        if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.RegisterStanceChange();
        }
    }
}
