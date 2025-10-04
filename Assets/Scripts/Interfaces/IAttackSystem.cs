/*
Written by Brandon Wahl

This interface will be assigned to indivdual hitboxes so their damage amount and name can be tracked.


*/

using UnityEngine;

public interface IAttackSystem 
{
    string weaponName { get; }
    float damageAmount { get; }
}
