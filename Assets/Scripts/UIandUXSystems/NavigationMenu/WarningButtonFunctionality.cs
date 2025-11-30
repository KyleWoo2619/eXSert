/*
    Written by Brandon Wahl

    This script will handle the functionality of the warning buttons in the pause menu
*/


using UnityEngine;

public class WarningButtonFunctionality : MonoBehaviour
{
    [SerializeField] private GameObject checkpointText;
    [SerializeField] private GameObject quitText;
    [SerializeField] private GameObject returnToMenuText;

    public void WhichFunctionToCarryOut()
    {
        if(checkpointText.activeSelf)
        {
            CheckpointFunction();
        }
        else if(quitText.activeSelf)
        {
            QuitFunction();
        }
        else if(returnToMenuText.activeSelf)
        {
            ReturnToMenuFunction();
        }
    }

    private void CheckpointFunction()
    {
        Debug.Log("Loading Last Checkpoint...");
        // Checkpoint Logic
    }

    private void QuitFunction()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    private void ReturnToMenuFunction()
    {
        Debug.Log("Returning to Main Menu...");
        // Scene Loading Logic
    }
}
