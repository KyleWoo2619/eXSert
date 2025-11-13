/*
    Manages the settings that are only available in game; acts as a middle man between the main menu settings and the functionality

    written by Brandon Wahl
*/

using UnityEngine;
using Singletons;
using System.Collections.Generic;

public class SettingsManager : Singleton<SettingsManager>
{
    [SerializeField] internal bool invertY;
    internal float sensitivity;
    [SerializeField] internal bool comboProgression;
    [SerializeField] internal float rumbleStrength;

    [SerializeField] private Sprite[] gamePadIconArray;

    [SerializeField] internal Dictionary<string, Sprite> gamePadIcons = new Dictionary<string, Sprite>();
    
    protected override void Awake()
    {
        base.Awake();

       gamePadIcons = new Dictionary<string, Sprite>()
       {
           {"buttonSouth", gamePadIconArray[0]},
           {"buttonNorth", gamePadIconArray[1]},
           {"buttonEast", gamePadIconArray[2]},
           {"buttonWest", gamePadIconArray[3]},
           {"leftShoulder", gamePadIconArray[4]},
           {"rightShoulder", gamePadIconArray[5]},
           {"leftTrigger", gamePadIconArray[6]},
           {"rightTrigger", gamePadIconArray[7]},
           {"dpadUp", gamePadIconArray[8]},
           {"dpadDown", gamePadIconArray[9]},
           {"dpadLeft", gamePadIconArray[10]},
            {"dpadRight", gamePadIconArray[11]},
           {"leftStick", gamePadIconArray[12]},
           {"rightStick", gamePadIconArray[13]}
       };
        
    }
}
