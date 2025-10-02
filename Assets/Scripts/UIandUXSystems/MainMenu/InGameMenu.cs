/*
Written by Brandon Wahl

Manages the in game menu and the unique actions performed

*/

using UnityEngine;

public class InGameMenu : MonoBehaviour
{

    //Saves the game when clicked
    public void SaveOnClick()
    {
        DataPersistenceManager.instance.SaveGame();
    }
}
