/*
Written by Brandon Wahl

This script is the framework for eXsert's combo system. Here it juggles between the four hitboxes used and activates and deactivates them based on player input. 
It also checks for inactivity between inputs so the combo resets.


*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackManager : MonoBehaviour
{
    // will's stuff
    [Header("Input")]
    [SerializeField] private InputActionReference _lightAttackAction;
    [SerializeField] private InputActionReference _heavyAttackAction;

    [Space, Header("Attack Objects")]
    [SerializeField] PlayerAttack lightSingle;
    [SerializeField] PlayerAttack lightAOE;
    [SerializeField] PlayerAttack heavySingle;
    [SerializeField] PlayerAttack heavyAOE;

    [Space, Header("Animator")]
    [SerializeField] Animator animator;

    [Space, Header("Audio")]
    private AudioSource playSFX;
    [SerializeField] AudioClip[] playerAttackClip;


    PlayerAttack currentAttack;

    [Space]
    // brandon's stuff
    [SerializeField] private float amountOfTimeBetweenAttacks = 1.5f;

    private float lastAttackPressTime;

    [Space, Header("Combo Colliders")]
    [SerializeField] private BoxCollider[] comboHitboxes;

    //The list is used to easily track which number of the combo the player is on
    private List<BoxCollider> currentComboAmount = new List<BoxCollider>();

    private void Start()
    {
        if (_lightAttackAction.action == null)
            Debug.LogError("Light Attack Action is NULL! Assign the Light Attack Action to the Player Input component.");

        if (_heavyAttackAction.action == null)
            Debug.LogError("Heavy Attack Action is NULL! Assign the Heavy Attack Action to the Player Input component.");


        // ensures all hitboxes are off at start
        foreach (BoxCollider box in comboHitboxes)
        {
            box.enabled = false;
        }

        lastAttackPressTime = Time.time;
        
        playSFX = SoundManager.Instance.sfxSource;
    }

    private void Update()
    {
        if (_lightAttackAction.action.triggered && !InputReader.inputBusy)
            OnLightAttack();
        else
            animator.ResetTrigger("lightTrigger");


        if (!_lightAttackAction.action.triggered && _heavyAttackAction.action.triggered && !InputReader.inputBusy)
            OnHeavyAttack();
        else
            animator.ResetTrigger("heavyTrigger");


        if (currentComboAmount.Count > 0)
            InactivityCheck();
    }

    public void OnLightAttack()
    {
        PerformLightAttack();
    }

    public void OnHeavyAttack()
    {
        PerformHeavyAttack();
    }

    private void PerformLightAttack()
    {
        Attack(true);
    }

    private void PerformHeavyAttack()
    {
        Attack(false);
    }

    private void Attack(bool light)
    {
        
        playSFX.clip = playerAttackClip[Random.Range(0, playerAttackClip.Length)];

        if (animator != null)
        {
            animator.SetBool("stance", CombatManager.singleTargetMode);
            if (light)
                animator.SetTrigger("lightTrigger");
            else
                animator.SetTrigger("heavyTrigger");
        }


        //First determines whether the heavy or light input is detected
        if (light)
        {
            

            //Then checks which stance the player is in to properly activated a hitbox
            if (CombatManager.singleTargetMode)
            {
                currentComboAmount.Add(comboHitboxes[0]);
                comboHitboxes[0].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[0]));

                currentAttack = lightSingle;
            }
            else
            {
                currentComboAmount.Add(comboHitboxes[1]);
                comboHitboxes[1].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[1]));

                currentAttack = lightAOE;
            }
        }

        else
        {

            if (CombatManager.singleTargetMode)
            {
                currentComboAmount.Add(comboHitboxes[2]);
                comboHitboxes[2].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[2]));

                currentAttack = heavySingle;
            }
            else
            {
                currentComboAmount.Add(comboHitboxes[3]);
                comboHitboxes[3].enabled = true;
                StartCoroutine(TurnOffHitboxes(comboHitboxes[3]));

                currentAttack = heavyAOE;
            }
        }
        Debug.Log("Combo Amount: " + currentComboAmount.Count);

        Debug.Log($"Performed Attack: {currentAttack.attackName}");

        lastAttackPressTime = Time.time;

        playSFX.Play();

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
