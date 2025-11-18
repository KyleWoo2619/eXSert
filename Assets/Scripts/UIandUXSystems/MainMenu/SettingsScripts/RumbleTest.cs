/**
    Written by Brandon Wahl
 
    Use this script to know how to use rumble.
**/

using UnityEngine;

public class RumbleTest : MonoBehaviour
{
    //This use specifically is used for rumble when pressing a menu button, but can be used anywhere
    public void onClickRumble()
    {
        // Simple rumble pulse: low frequency (you want to set this lower usually), high frequency, duration of rumble
        RumbleManager.Instance.RumblePulse(.25f, 1, .25f);
    }
}
