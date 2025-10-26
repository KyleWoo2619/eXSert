/*
    Scriptable objects for the hidden logs throughout the game.

    Written by Brandon Wahl
*/

using UnityEngine;
using UnityEditor;
using System;

[Serializable]
[ExecuteInEditMode]
[CreateAssetMenu(fileName = "NavigationLogSO", menuName = "NavigationMenu/Logs", order = 1)]
public class NavigationLogSO : ScriptableObject
{


    [field: SerializeField] public string logID { get; private set; }
    public string logName;
    public string locationFound;
    public string dateFound;

    [TextArea(3, 10)]
    public string logDescription;

    public bool isFound;

    //This ensures that the idName cannot be repeated
    private void OnValidate()
    {

#if UNITY_EDITOR
        string idName = this.name.Replace("Log", "");
        logID = "#00" + idName;
        EditorUtility.SetDirty(this);

#endif


    }

}


