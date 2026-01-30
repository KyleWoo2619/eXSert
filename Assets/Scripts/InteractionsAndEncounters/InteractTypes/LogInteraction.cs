/*
    Written by Brandon Wahl

    Place this script where you want a log entry to be interacted with and collected into the player's inventory.
*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using UnityEditor.PackageManager;

[RequireComponent(typeof(BoxCollider))]
public class LogInteraction : InteractionManager
{
    [SerializeField] private ScriptableObject logData;

    private void OnEnable()
    {
        EventsManager.Instance.logEvents.onLogStateChange += OnLogStateChange;
    }

    private void OnDisable()
    {
        EventsManager.Instance.logEvents.onLogStateChange -= OnLogStateChange;
    }

    private void OnLogStateChange(Logs log)
    {
        if (log.info.logID.Equals(this.interactId))
        {
            Debug.Log("Log with id " + this.interactId + " updated to state: Is Found " + log.info.isFound);
        }
    }

    protected override void Interact()
    {
        var logSO = logData as NavigationLogSO;
        logSO.isFound = true;

        EventsManager.Instance.logEvents.FoundLog(this.interactId);
        DeactivateInteractable(this);
    }

}
