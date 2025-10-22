using UnityEngine;
using System;
using UnityEditor;

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

    private void OnValidate()
    {

        #if UNITY_EDITOR
        string idName = this.name.Replace("Log", "");
        logID = "#00" + idName;
        EditorUtility.SetDirty(this);

        #endif
            
        
    }

}
