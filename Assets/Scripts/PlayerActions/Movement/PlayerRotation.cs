/*
Written by Brandon Wahl

This script is designed to handle player rotation through the "playerControls" action map

*/
using UnityEngine;
using UnityEngine.Windows;

public class PlayerRotation : MonoBehaviour
{
    [Tooltip("Mouse Sensitivity")][SerializeField][Range(.1f, 3)] private float mouseSens;
    [Tooltip("Range of looking up or down")][SerializeField][Range(10, 100)] private float upDownRange;
    private float verticalRotation;

    [Header("Camera Settings")]
    [SerializeField] bool invertYAxis = false;

    private InputReader input;

    private Camera mainCamera;

    private void Start()
    {
        input = InputReader.Instance;
    }

    void Awake()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Rotation();
    }

    //Gathers the LookInput.y output and rotates the player and camera according. This function also gives the player the functionality to invert the Y axis
    public void Rotation()
    {
        float mouseYInput = invertYAxis ? -input.LookInput.y : input.LookInput.y;

        float mouseXRotation = input.LookInput.x * mouseSens;
        transform.Rotate(0, mouseXRotation, 0);

        verticalRotation -= mouseYInput * mouseSens;

        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }


}
