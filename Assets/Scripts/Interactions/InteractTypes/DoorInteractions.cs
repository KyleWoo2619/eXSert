/*
    Written by Brandon Wahl

    Place this script on any door GameObject to allow player interaction with it.
*/
using UnityEngine;

public class DoorInteractions : InteractionManager
{
    
    protected override void Interact()
    {
        var doorHandler = this.GetComponent<DoorHandler>();
        if (doorHandler != null)
        {
            doorHandler.Interact();
        }
    }

}
