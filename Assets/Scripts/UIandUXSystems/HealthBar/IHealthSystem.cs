using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public interface IHealthSystem
{
    //Interface that allows any creature (player or enemy) to update their health
    void HealHP(float hp);

    void LoseHP(float damage);

    float currentHP { get; }

    float maxHP { get; } 
    
}
