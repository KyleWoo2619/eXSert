using UnityEngine;

public class InGameMenu : MonoBehaviour
{
    public void SaveOnClick()
    {
        DataPersistenceManager.instance.SaveGame();
    }
}
