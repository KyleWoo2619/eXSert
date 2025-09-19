/*
Written by Brandon Wahl

Allows the player to switch between states, or stances, which changes attack styles

*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
public class ChangeStance : MonoBehaviour
{
    private InputReader input;
    [SerializeField] [Range(0, 1)] private int currentStance;
    [SerializeField] bool canChangeStance = true;

    void Start()
    {
        input = InputReader.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        ChangePlayerStance();
        Debug.Log(currentStance);
    }

    //Gathers input from the player and changes which stance is currently equipped
    private void ChangePlayerStance()
    {
        var outOfBounds = stance.Count - 1;


        if (canChangeStance)
        {
            if (input.ChangeStanceTrigger)
            {
                if (currentStance == outOfBounds)
                {
                    currentStance--;
                } else
                {
                    currentStance++;
                }

                Debug.Log("Current stance: " + stance[currentStance]);
                canChangeStance = false;
                StartCoroutine(StanceChangeCoolDown());
            }

        }
    }

    //Cooldown so players can't infinitely change their stance
    private IEnumerator StanceChangeCoolDown()
    {

        yield return new WaitForSeconds(1f);
        canChangeStance = true;
    }

    //Manages what stances the player is able to use
    private Dictionary<int, string> stance = new Dictionary<int, string>()
    {
        {0, "Single Attack"},
        {1, "Area of Effect"}
    };
}
