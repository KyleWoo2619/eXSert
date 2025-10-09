using eXsert;
using Unity.Cinemachine;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Guard Mode - Over Shoulder Settings")]
    [SerializeField] private float guardRadius = 2.5f;     // Close over-shoulder distance
    [SerializeField] private float guardHeightOffset = 0.5f; // Added height offset for shoulder view
    [SerializeField] private float zoomLerpSpeed = 8f;     // Transition speed
    
    private CinemachineCamera cmCamera;
    private CinemachineOrbitalFollow orbital;

    // Store original Three Ring settings to restore them
    private float originalRadius;
    private Vector3 originalTargetOffset;
    
    // Current state
    private bool wasGuarding = false;
    private bool isTransitioning = false;

    private void Start()
    {        
        cmCamera = GetComponent<CinemachineCamera>();
        orbital = cmCamera?.GetComponent<CinemachineOrbitalFollow>();

        if (orbital == null)
        {
            Debug.LogError("ThirdPersonCameraController: CinemachineOrbitalFollow not found on this Camera.");
            enabled = false;
            return;
        }

        // Store the original Three Ring settings from Inspector
        StoreOriginalSettings();
    }

    private void Update()
    {
        if (orbital == null) return;
        
        if (CombatManager.isGuarding != wasGuarding)
        {
            wasGuarding = CombatManager.isGuarding;
            
            if (CombatManager.isGuarding)
            {
                Debug.Log("Camera: Entering Guard Mode - Over Shoulder View");
                EnterGuardMode();
            }
            else
            {
                Debug.Log("Camera: Exiting Guard Mode - Restoring Three Ring");
                ExitGuardMode();
            }
        }

        // Only update camera position if we're transitioning or in guard mode
        if (isTransitioning || CombatManager.isGuarding)
        {
            UpdateCameraTransition();
        }
    }

    private void StoreOriginalSettings()
    {
        // Store the original Three Ring setup from your Inspector settings
        originalRadius = orbital.Radius;
        originalTargetOffset = orbital.TargetOffset;
        
        Debug.Log($"Stored original settings - Radius: {originalRadius}, Offset: {originalTargetOffset}");
        
        // Debug: Check properties we can access
        Debug.Log($"Target Offset: {orbital.TargetOffset}");
        
        // Try to check if this has ring settings
        // In Cinemachine, Three Ring might use different properties
    }

    private void EnterGuardMode()
    {
        isTransitioning = true;
        // We'll smoothly transition to guard settings in UpdateCameraTransition()
    }

    private void ExitGuardMode()
    {
        isTransitioning = true;
        // We'll smoothly transition back to original Three Ring settings
    }

    private void UpdateCameraTransition()
    {
        if (wasGuarding) // Transitioning TO guard mode
        {
            // Lerp to guard settings
            float newRadius = Mathf.Lerp(orbital.Radius, guardRadius, Time.deltaTime * zoomLerpSpeed);
            Vector3 targetGuardOffset = new Vector3(originalTargetOffset.x, originalTargetOffset.y + guardHeightOffset, originalTargetOffset.z);
            Vector3 newOffset = Vector3.Lerp(orbital.TargetOffset, targetGuardOffset, Time.deltaTime * zoomLerpSpeed);

            orbital.Radius = newRadius;
            orbital.TargetOffset = newOffset;

            // Check if transition is complete
            if (Mathf.Abs(orbital.Radius - guardRadius) < 0.01f)
            {
                orbital.Radius = guardRadius;
                orbital.TargetOffset = targetGuardOffset;
                isTransitioning = false;
                Debug.Log("Guard transition complete");
            }
        }
        else // Transitioning BACK to normal Three Ring
        {
            // Lerp back to original Three Ring settings
            float newRadius = Mathf.Lerp(orbital.Radius, originalRadius, Time.deltaTime * zoomLerpSpeed);
            Vector3 newOffset = Vector3.Lerp(orbital.TargetOffset, originalTargetOffset, Time.deltaTime * zoomLerpSpeed);

            orbital.Radius = newRadius;
            orbital.TargetOffset = newOffset;

            // Check if transition is complete
            if (Mathf.Abs(orbital.Radius - originalRadius) < 0.01f)
            {
                // Restore exact original settings
                orbital.Radius = originalRadius;
                orbital.TargetOffset = originalTargetOffset;
                isTransitioning = false;
                Debug.Log("Three Ring restoration complete");
            }
        }
    }

    // Public method to reset to original settings if needed
    public void ResetToOriginalSettings()
    {
        if (orbital != null)
        {
            orbital.Radius = originalRadius;
            orbital.TargetOffset = originalTargetOffset;
            isTransitioning = false;
            wasGuarding = false;
            Debug.Log("Camera reset to original Three Ring settings");
        }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (orbital != null)
        {
            // Blue = Normal Three Ring, Red = Guard Mode, Yellow = Transitioning
            if (isTransitioning)
                Gizmos.color = Color.yellow;
            else if (CombatManager.isGuarding)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.blue;
                
            Gizmos.DrawWireSphere(transform.position, orbital.Radius);
        }
    }
}
