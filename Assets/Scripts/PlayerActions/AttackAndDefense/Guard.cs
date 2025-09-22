/*
Written by Brandon Wahl

Manages the player's ability to guard

*/

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class Guard : MonoBehaviour
{
    private InputReader input;
    [SerializeField] private bool canGuard;
    private PlayerMovement movement;
    private float originalSpeed;

    void Start()
    {
        input = InputReader.Instance;
        movement = GetComponent<PlayerMovement>();
        originalSpeed = movement.speed;
        canGuard = true;
    }

    // Update is called once per frame
    void Update()
    {
        OnGuardHold();
    }

    //If the player is able to guard they will be in the guard state until they let go of the button
    void OnGuardHold()
    {
        if (canGuard)
        {
            if (input.GetGuard())
            {

                Debug.Log("Is Guarding");
                movement.speed = originalSpeed / 2;

            } else
            {
                movement.speed = originalSpeed; 
            }

        }

    }

    //Cooldown so players can't infinitely guard
    //private IEnumerator GuardCoolDown()
    //{
    //    yield return new WaitForSeconds(3);
    //    canGuard = true;
    //}
}
