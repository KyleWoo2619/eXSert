using UnityEngine;
using Singletons;
using UnityEngine.InputSystem;
using System;

public class SettingsManager : Singleton<SettingsManager>
{
    [SerializeField] internal bool invertY;
    internal float sensitivity;
    [SerializeField] internal bool comboProgression;
    protected override void Awake()
    {
        base.Awake();
    }
}
