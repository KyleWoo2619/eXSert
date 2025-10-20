using System.Collections;
using UnityEngine;
public class ConfirmationBoxPrompt : MonoBehaviour
{
    [SerializeField] private GameObject confirmationPrompt = null;
    public IEnumerator ConfirmationBox()
    {
        confirmationPrompt.SetActive(true);
        yield return new WaitForSeconds(2);
        confirmationPrompt.SetActive(false);
    }
    
}
