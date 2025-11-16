using System;
using UnityEngine;
using TMPro;
//Written By Brandon
public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] [Range(1, 10 )] private int amountOfHPEffected;
    [SerializeField] private TMP_Text healthText;

    private void Update()
    {
        var health = GameObject.FindWithTag("Player").GetComponent<PlayerHealthBarManager>();

        healthText.text = health.health.ToString();
    }

    //Tests script to show healthbar functionality
    public void TakeDamage()
    {
        var health = GameObject.FindWithTag("Player").GetComponent<PlayerHealthBarManager>();

        health.LoseHP(amountOfHPEffected);

        Debug.Log(health.health);
    }

    public void Heal()
    {
        var health = GameObject.FindWithTag("Player").GetComponent<PlayerHealthBarManager>();

        health.HealHP(amountOfHPEffected);

        Debug.Log(health.health);

    }
}
