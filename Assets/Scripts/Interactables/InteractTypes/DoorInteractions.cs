/*
    Written by Brandon Wahl

    Specialized unlockable interaction for doors.
    Place this script on any GameObject that will allow a certain door to open.
    It could be on a console, a button, or even the door itself.
    Make sure to assign the DoorHandler component of the door you want to interact with in the inspector.
*/
using UnityEngine;

public class DoorInteractions : UnlockableInteraction
{
    [Tooltip("Place the gameObject with the DoorHandler component here, it may be on a different object or the same object as this script.")]
    [SerializeField] private DoorHandler doorHandler;
    [SerializeField] private DoorHandler doorhandler2;
    [SerializeField] private DoorHandler doorhandler3;
    [SerializeField] protected DoorHandler doorhandler4;

    protected override void ExecuteInteraction()
    {
        if (doorHandler != null)
        {
            doorHandler.Interact();

            if (doorhandler2 != null)
            {
                doorhandler2.Interact();

                if (doorhandler3 != null)
                {
                    doorhandler3.Interact();

                    if (doorhandler4 != null)
                    {
                        doorhandler4.Interact();
                    }

                    else
                    {
                        Debug.Log($"DoorHandler4 not assigned on {gameObject.name}");
                    }
                }

                else
                {
                    Debug.Log($"DoorHandler3 not assigned on {gameObject.name}");
                }
            }
            else
            {
                Debug.Log($"DoorHandler2 not assigned on {gameObject.name}");
            }
        }
        else
        {
            Debug.LogError($"DoorHandler not assigned on {gameObject.name}");
        }
        
    }
}
