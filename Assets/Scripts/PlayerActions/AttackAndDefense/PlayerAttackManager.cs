/*
Written by Brandon Wahl

This script is the framework for eXsert's combo system. Here it juggles between the four hitboxes used and activates and deactivates them based on player input. 
It also checks for inactivity between inputs so the combo resets.


*/

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttackManager : MonoBehaviour { 

    [SerializeField] private int maxComboAmount = 5;
    [SerializeField] private float amountOfTimeBetweenAttacks = 1.5f;
    protected float lastAttackPressTime;

    private InputReader input;
    private ChangeStance changeStance;

    [SerializeField] private BoxCollider[] comboHitboxes;

    //The list is used to easily track which number of the combo the player is on
    private List<BoxCollider> currentComboAmount = new List<BoxCollider>();

    private void Start()
    {
        input = InputReader.Instance;
        lastAttackPressTime = Time.time;
        changeStance = GetComponent<ChangeStance>();
    }

    private void Update()
    {
        InactivityCheck();

        Attack();

    }

    private void Attack()
    {
        //First determines whether the heavy or light input is detected
        if (input.LightAttackTrigger)
        {
            lastAttackPressTime = Time.time;
            input.LightAttackTrigger = false;

            Debug.Log("Combo Amount: " + currentComboAmount.Count);

            //Then checks which stance the player is in to properly activated a hitbox
            if (changeStance.currentStance == 0)
            {
                currentComboAmount.Add(comboHitboxes[0]);
                comboHitboxes[0].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[0]));
            }
            else
            {
                currentComboAmount.Add(comboHitboxes[1]);
                comboHitboxes[1].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[1]));
            }

        }
        else if (input.HeavyAttackTrigger)
        {
            lastAttackPressTime = Time.time;
            input.HeavyAttackTrigger = false;

            Debug.Log("Combo Amount: " + currentComboAmount.Count);

            if (changeStance.currentStance == 0)
            {
                currentComboAmount.Add(comboHitboxes[2]);
                comboHitboxes[2].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[2]));
            }
            else
            {
                currentComboAmount.Add(comboHitboxes[3]);
                comboHitboxes[3].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[3]));
            }
        }

        /*If the player goes over the designated combo limit then it is reset back to 0. The "- 1" is added since combos technically starts at 0, but, for QOL, whoever is editing can
          input whatever limit they like without thinking of the technical details.
        */
        if (currentComboAmount.Count > maxComboAmount - 1) 
        {
            ResetCombo();
            Debug.Log("Combo Complete!");
        }
    }

    //If the player doesn't make an input within the designated amount of time, then it is reset
    private void InactivityCheck()
    {
        if(Time.time - lastAttackPressTime > amountOfTimeBetweenAttacks)
        {
            ResetCombo();
        }
    }

    //Clears the list which essentially clears the combo counter
    private void ResetCombo()
    {
        Debug.Log("Combo Reset");
        currentComboAmount.Clear();
    }

    //Turns off the hitbox
    private IEnumerator TurnOffHitboxes(BoxCollider box) 
    {
        yield return new WaitForSeconds(.2f);
        box.enabled = false;
    
    }

}
