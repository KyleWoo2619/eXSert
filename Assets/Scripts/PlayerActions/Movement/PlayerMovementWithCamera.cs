using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private bool shouldFaceMoveDirection = false;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;

    [Space, Header("Audio")]
    private AudioSource playSFX;
    [SerializeField] AudioClip dashAudio;
    [SerializeField] AudioClip jumpAudio;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playSFX = SoundManager.Instance.sfxSource;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        Debug.Log($"Move Input: {moveInput}");
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        playSFX.clip = jumpAudio;

        Debug.Log($"Jumping {context.performed} - Is Grounded: {controller.isGrounded}");
        if (context.performed && controller.isGrounded)
        {
            Debug.Log("We are supposed to jump");
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            playSFX.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
        controller.Move(moveDirection * speed * Time.deltaTime);

        if (shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
        }

        // deals with jumping
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
