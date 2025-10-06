/*
Written by Brandon Wahl

Manages the player's ability to guard

*/

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class Guard : MonoBehaviour
{
    private InputReader input;
    [SerializeField] private bool canGuard;
    private PlayerMovement movement;
    private float originalSpeed;
    
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera moveCam;    // Your normal Cinemachine Camera
    [SerializeField] private CinemachineCamera guardCam;   // Your guard Cinemachine Camera

    private CinemachineOrbitalFollow moveOrb;
    private CinemachineOrbitalFollow guardOrb;

    void Start()
    {
        input = InputReader.Instance;
        movement = GetComponent<PlayerMovement>();
        originalSpeed = movement.speed;
        canGuard = true;
        
        // Initialize Cinemachine cameras
        InitializeCinemachineCameras();
    }
    
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

    // Update is called once per frame
    void Update()
    {
        OnGuardHold();
    }

    //If the player is able to guard they will be in the guard state until they let go of the button
    public void OnGuardHold()
    {
        if (canGuard && input != null)
        {
            if (input.GuardTrigger)
            {
                // Enter Guard Mode
                movement.speed = originalSpeed * 0.5f;
                SwitchToGuardCamera();
            }
            else
            {
                // Exit Guard Mode
                movement.speed = originalSpeed;
                SwitchToMoveCamera();
            }
        }
    }

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

    //Cooldown so players can't infinitely guard
    //private IEnumerator GuardCoolDown()
    //{
    //    yield return new WaitForSeconds(3);
    //    canGuard = true;
    //}
}
