/*
Written by Brandon Wahl

Manages the player's ability to guard


* 
* Edited by Will T.
* 
* made it more streamlined by removing camera functionality to be handled in camera script
* additionally added parry functionality with CombatManager
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class Guard : MonoBehaviour
{
    /*
     * MOVE CAMERA FUNCTIONALITY TO CAMERA SCRIPT, REFERENCING CombatManager.isGuarding FOR STATE
     * 
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera moveCam;    // Your normal Cinemachine Camera
    [SerializeField] private CinemachineCamera guardCam;   // Your guard Cinemachine Camera
    *

    private CinemachineOrbitalFollow moveOrb;
    private CinemachineOrbitalFollow guardOrb;
    *
    
    private void InitializeCinemachineCameras()
    {
        if (moveCam != null && guardCam != null)
        {
            // Get orbital follow components
            moveOrb = moveCam.GetComponent<CinemachineOrbitalFollow>();
            guardOrb = guardCam.GetComponent<CinemachineOrbitalFollow>();
            
            // Set priorities - moveCam active by default
            moveCam.Priority = 20;   // Active camera
            guardCam.Priority = 0;   // Inactive camera
            
            Debug.Log("Cinemachine cameras initialized - MoveCam active, GuardCam inactive");
            
            // Validation
            if (moveOrb == null)
                Debug.LogWarning("MoveCam is missing CinemachineOrbitalFollow component!");
            if (guardOrb == null)
                Debug.LogWarning("GuardCam is missing CinemachineOrbitalFollow component!");
        }
        else
        {
            Debug.LogError("Please assign MoveCam and GuardCam Cinemachine Cameras in Inspector!");
        }
    }
    */

    [SerializeField] InputActionReference _guardActionReference;

    private void OnEnable()
    {
        if (_guardActionReference == null || _guardActionReference.action == null)
            Debug.LogError("Guard Input Action Reference is not set in the inspector. Player won't be able to guard.");

        else
            _guardActionReference.action.Enable();
    }

    private void Update()
    {
        if(_guardActionReference != null && _guardActionReference.action != null){
            // Check if the guard button was pressed this frame and enter guard mode
            if (_guardActionReference.action.WasPressedThisFrame() && !InputReader.inputBusy)
            {
                CombatManager.EnterGuard();
            }

            // Check if the guard button was released this frame and exit guard modes
            if (_guardActionReference.action.WasReleasedThisFrame() && CombatManager.isGuarding)
            {
                CombatManager.ExitGuard();
            }
        }
    }

    private void OnDisable()
    {
        if (_guardActionReference != null && _guardActionReference.action != null)
            _guardActionReference.action.Disable();
    }

    /*
    private void SwitchToGuardCamera()
    {
        if (moveCam != null && guardCam != null && moveOrb != null && guardOrb != null)
        {
            // Copy current orbit from move to guard so orientation is preserved
            guardOrb.HorizontalAxis.Value = moveOrb.HorizontalAxis.Value;
            guardOrb.VerticalAxis.Value = moveOrb.VerticalAxis.Value;
            guardOrb.RadialAxis.Value = moveOrb.RadialAxis.Value;

            // Switch priorities
            guardCam.Priority = 20;  // Active
            moveCam.Priority = 0;    // Inactive
            
            Debug.Log("Switched to Guard Camera with preserved orientation");
        }
        else
        {
            Debug.LogWarning("Cinemachine cameras or orbital components not properly assigned!");
        }
    }

    private void SwitchToMoveCamera()
    {
        if (moveCam != null && guardCam != null && moveOrb != null && guardOrb != null)
        {
            // Copy orbit back (optional - maintains orientation when exiting guard)
            moveOrb.HorizontalAxis.Value = guardOrb.HorizontalAxis.Value;
            moveOrb.VerticalAxis.Value = guardOrb.VerticalAxis.Value;
            moveOrb.RadialAxis.Value = guardOrb.RadialAxis.Value;

            // Switch priorities
            moveCam.Priority = 20;   // Active
            guardCam.Priority = 0;   // Inactive
            
            // Debug.Log("Switched to Move Camera with preserved orientation");
        }
        else
        {
            Debug.LogWarning("Cinemachine cameras or orbital components not properly assigned!");
        }
    }
    */
}
