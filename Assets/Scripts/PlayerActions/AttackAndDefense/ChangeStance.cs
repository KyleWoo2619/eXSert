/*
Written by Brandon Wahl

Allows the player to switch between states, or stances, which changes attack styles

Editted by Will T


*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
public class ChangeStance : MonoBehaviour
{
    [SerializeField] InputActionReference _changeStanceAction;
    [SerializeField] float stanceCooldownTime = 1f;

    [Space, Header("Audio")]
    private AudioSource playSFX;
    [SerializeField] AudioClip changeStanceAudio;

    private void Start()
    {
        playSFX = SoundManager.Instance.sfxSource;
    }

    private void OnEnable()
    {
        if (_changeStanceAction == null || _changeStanceAction.action == null)
            Debug.LogError("Change Stance Input Action Reference is not set in the inspector. Player won't be able to change stances.");

        // if the action is valid, enable it and register the performed event
        else
        {
            if(changeStanceAudio != null)
            {
                playSFX = SoundManager.Instance.sfxSource;
                playSFX.clip = changeStanceAudio;

                playSFX.Play();
            }

            _changeStanceAction.action.Enable();
            _changeStanceAction.action.performed += OnStanceChange;
        }
    }

    private void OnDisable()
    {
        if (_changeStanceAction != null && _changeStanceAction.action != null)
        {
            _changeStanceAction.action.performed -= OnStanceChange; 
            _changeStanceAction.action.Disable();
        }
    }

    private void OnStanceChange(InputAction.CallbackContext context)
    {
        // checks to see if the player can change their stance
        if (!InputReader.inputBusy)
        {
            InputReader.inputBusy = true;
            CombatManager.ChangeStance();

            StartCoroutine(StanceChangeCoolDown());
        }
    }

    //Cooldown so players can't infinitely change their stance
    private IEnumerator StanceChangeCoolDown()
    {
        yield return new WaitForSeconds(stanceCooldownTime);
        InputReader.inputBusy = false;
    }
}
