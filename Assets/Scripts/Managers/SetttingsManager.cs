/*
    Manages the settings that are only available in game; acts as a middle man between the main menu settings and the functionality

    written by Brandon Wahl
*/

using UnityEngine;
using Singletons;

public class SettingsManager : Singleton<SettingsManager>
{
    [SerializeField] internal bool invertY;
    internal float sensitivity;
    [SerializeField] internal bool comboProgression;
    [SerializeField] internal bool rumbleOn;
    [SerializeField] internal float rumbleStrength;
    protected override void Awake()
    {
        base.Awake();
    }
}
