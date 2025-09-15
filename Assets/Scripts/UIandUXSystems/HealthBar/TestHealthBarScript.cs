using System;
using UnityEngine;
//Written By Brandon
public class TestHealthBarScript : MonoBehaviour
{
    [SerializeField] [Range(1, 100)] private int amountOfHPEffected;


    //Tests script to show healthbar functionality
    public void TakeDamage()
    {
        var health = GameObject.FindWithTag("Player").GetComponent<HealthBarManager>();

        health.LoseHP(amountOfHPEffected);

        Debug.Log(health.health);
    }

    public void Heal()
    {
        var health = GameObject.FindWithTag("Player").GetComponent<HealthBarManager>();

        health.HealHP(amountOfHPEffected);

        Debug.Log(health.health);

    }
}
