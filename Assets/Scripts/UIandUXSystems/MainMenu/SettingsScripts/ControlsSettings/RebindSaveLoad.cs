/*
    Script provided by unity that will save the rebinds made to player prefs
*/

using UnityEngine;
using UnityEngine.InputSystem;

public class RebindSaveLoad : MonoBehaviour
{
    public InputActionAsset actions;
    
    //If true, it will save the load control scheme
    public bool loadControlScheme;

    public void OnEnable()
    {
        if (loadControlScheme)
        {
            var rebinds = PlayerPrefs.GetString("rebinds");
            if (!string.IsNullOrEmpty(rebinds))
                //Loads the rebinds if player prefs isnt null
                actions.LoadBindingOverridesFromJson(rebinds);
        }
    }

    public void OnDisable()
    {
        if (loadControlScheme)
        {
            //Saves bindings to player prefs
            var rebinds = actions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString("rebinds", rebinds);
        }
    }
}
