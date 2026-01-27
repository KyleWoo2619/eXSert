using UnityEngine;

public class MenuUIIconSwapper : MonoBehaviour
{
    [SerializeField] private GameObject[] gamepadUI;
    [SerializeField] private GameObject[] keyboardMouseUI;

    void Update()
    {
        if(InputReader.Instance.activeControlScheme == "Gamepad")
        {
           foreach(GameObject uiElement in gamepadUI)
           {
               uiElement.SetActive(true);
           }
        }
        else
        {
           foreach(GameObject uiElement in keyboardMouseUI)
           {
               uiElement.SetActive(true);
           }
        }
    }
}
