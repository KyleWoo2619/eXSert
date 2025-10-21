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

    [Space, Header("Animator")]
    [SerializeField] Animator animator;

    [Space, Header("Audio")]
    private AudioSource playSFX;
    [SerializeField] AudioClip[] playerAttackClip;


    PlayerAttack currentAttack;

    private void Start()
    {
        if (_lightAttackAction.action == null)
            Debug.LogError("Light Attack Action is NULL! Assign the Light Attack Action to the Player Input component.");

        if (_heavyAttackAction.action == null)
            Debug.LogError("Heavy Attack Action is NULL! Assign the Heavy Attack Action to the Player Input component.");
        
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

    }

    public void OnLightAttack()
    {
        Debug.Log("Light Attack Input Detected");

        if (InputReader.inputBusy) return;

        PerformLightAttack();
    }

    public void OnHeavyAttack()
    {
        Debug.Log("Heavy Attack Input Detected");

        if (InputReader.inputBusy) return;

        PerformHeavyAttack();
    }

    private void PerformLightAttack()
    {
        InitiateAttack(true);
    }

    private void PerformHeavyAttack()
    {
        InitiateAttack(false);
    }

    private void InitiateAttack(bool lightAttack)
    {
        /*
         *              ,O,
         *             ,OOO,
         *       'oooooOOOOOooooo'
         *         `OOOOOOOOOOO`
         *           `OOOOOOO`
         *           OOOO'OOOO
         *          OOO'   'OOO
         *         O'         'O
         *         
         * CHANGE this to reference the sound from the attack scriptable object later
         */
        if (playerAttackClip.Length > 0)
            playSFX.clip = playerAttackClip[Random.Range(0, playerAttackClip.Length)];

        if (animator != null)
        {
            animator.SetBool("stance", CombatManager.singleTargetMode);
            if (lightAttack)
                animator.SetTrigger("lightTrigger");
            else
                animator.SetTrigger("heavyTrigger");
        }


        //First determines whether the heavy or light input is detected
        if (lightAttack)
        {

            //Then checks which stance the player is in to properly activated a hitbox
            if (CombatManager.singleTargetMode)
                currentAttack = ComboManager.Attack(AttackType.LightSingle);
            else
                currentAttack = ComboManager.Attack(AttackType.LightAOE);
        }

        else
        {

            if (CombatManager.singleTargetMode)
                currentAttack = ComboManager.Attack(AttackType.HeavySingle);
            else
                currentAttack = ComboManager.Attack(AttackType.HeavyAOE);
        }

        // Calls the coroutine to handle the attack timing and hitbox activation depending on the attack chosen by ComboManager
        StartCoroutine(PerformAttack(currentAttack));

        Debug.Log("Combo Amount: " + ComboManager.comboCount);

        Debug.Log($"Performed Attack: {currentAttack.attackName}");

    }

    /*
     * Coroutine to handle the timing of the attack, including start lag and end lag.
     * It also manages the enabling and disabling of hitboxes and starts the timer for combo reset.
     * Disables player input during the attack animation.
     */
    public IEnumerator PerformAttack(PlayerAttack attack)
    {
        PlayerAttack executedAttack = attack;
        InputReader.inputBusy = true;

        // Start the attack animation

        yield return new WaitForSeconds(executedAttack.startLag);

        // Here you would typically enable the hitbox and apply damage to enemies within range
        Debug.Log($"Executing Attack: {executedAttack.attackName} with Damage: {executedAttack.damage}");

        // End the attack animation
        yield return new WaitForSeconds(executedAttack.endLag);
        InputReader.inputBusy = false;

        StartCoroutine(ComboManager.WaitForInputReset());
        yield return null;
    }
}
