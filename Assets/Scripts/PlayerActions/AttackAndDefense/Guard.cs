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

    void Start()
    {
        input = InputReader.Instance;
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
            }
        }
    }

    //Cooldown so players can't infinitely guard
    private IEnumerator GuardCoolDown()
    {
        yield return new WaitForSeconds(1);
        canGuard = true;
    }
}
