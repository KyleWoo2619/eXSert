using UnityEngine;
using Singletons;
using UnityEngine.InputSystem;
using System;

public class CameraSettingsManager : Singleton<CameraSettingsManager>
{
    internal bool invertY;
    internal float sensitivity;
    protected override void Awake()
    {
        base.Awake();
    }
}
