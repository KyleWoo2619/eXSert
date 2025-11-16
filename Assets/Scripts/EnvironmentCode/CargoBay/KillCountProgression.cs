using UnityEngine;
using UnityEngine.Events;

public class KillCountProgression : MonoBehaviour
{
    private int killCount = 0;
    public GameObject progBarrier_1;
    public GameObject progBarrier_2;
    public GameObject amblingZone_1;
    public GameObject amblingZone_2;
    public GameObject amblingZone_3;

    //increments kill count

    private void Start()
    {
        EnemyHealthManager.onDeathEvent += KillCountUpdate;
    }
    public void KillCountUpdate()
    {
        killCount++;
        Debug.Log("Current kill count: " + killCount);
        ProgressionEnabler();
    }
    //checks current progress point and kill count to determine if the next progress point has been reached and if it needs to be enabled.
    private void ProgressionEnabler()
    {
        if (progBarrier_1.activeInHierarchy == true && killCount >= 5)
        {
            progBarrier_1.SetActive(false);
            amblingZone_1.SetActive(false);
            amblingZone_2.SetActive(true);
        }
        if (progBarrier_2.activeInHierarchy == true && killCount >= 11)
        {
            progBarrier_2.SetActive(false);
            amblingZone_2.SetActive(false);
            amblingZone_3.SetActive(true);
        }
        else 
        {
            return;
        }
    }
}
