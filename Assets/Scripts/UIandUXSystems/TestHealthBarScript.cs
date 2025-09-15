using UnityEngine;
//Written By Brandon
public class TestHealthBarScript : MonoBehaviour
{
    //Tests script to show healthbar functionality
    public void TakeDamage()
    {
        var health = GameObject.FindWithTag("Player").GetComponent<HealthBarManager>();

        health.LoseHP(1);

        Debug.Log(health.health);
    }

    public void Heal()
    {
        var health = GameObject.FindWithTag("Player").GetComponent<HealthBarManager>();

        health.HealHP(1);

        Debug.Log(health.health);

    }
}
