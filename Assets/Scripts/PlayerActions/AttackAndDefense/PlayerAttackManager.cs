using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttackManager : MonoBehaviour { 

    [SerializeField] private int maxComboAmount = 3;
    [SerializeField] private float amountOfTimeBetweenAttacks = 1.5f;
    protected float lastAttackPressTime;

    private InputReader input;
    private ChangeStance changeStance;

    [SerializeField] private BoxCollider[] comboHitboxes;
    private List<BoxCollider> currentComboAmount = new List<BoxCollider>();

    private void Start()
    {
        input = InputReader.Instance;
        lastAttackPressTime = Time.time;
        changeStance = GetComponent<ChangeStance>();
    }

    private void Update()
    {
        InactivityCheck();

        Attack();

    }

    private void Attack()
    {

        if (input.AttackTrigger)
        {
            lastAttackPressTime = Time.time;
            input.AttackTrigger = false;

            Debug.Log("Combo Amount: " + currentComboAmount.Count);
            
            //Light Attacks
            switch (currentComboAmount.Count)
            {

                case 0:
                    currentComboAmount.Add(comboHitboxes[0]);
                    comboHitboxes[0].enabled = true;
                    Debug.Log(comboHitboxes[0].GetComponent<HitboxDamageManager>().weaponName);
                    StartCoroutine(TurnOffHitboxes(comboHitboxes[0]));
                    break;

                case 1:
                    currentComboAmount.Add(comboHitboxes[1]);
                    comboHitboxes[1].enabled = true;
                    Debug.Log(comboHitboxes[1].GetComponent<HitboxDamageManager>().weaponName);
                    StartCoroutine(TurnOffHitboxes(comboHitboxes[1]));
                    break;

                case 2:
                    currentComboAmount.Add(comboHitboxes[2]);
                    comboHitboxes[2].enabled = true;
                    Debug.Log(comboHitboxes[2].GetComponent<HitboxDamageManager>().weaponName);
                    StartCoroutine(TurnOffHitboxes(comboHitboxes[2]));
                    break;

            }

        }

        if(currentComboAmount.Count > maxComboAmount - 1)
        {
            ResetCombo();
        }
    }

    private void InactivityCheck()
    {
        if(Time.time - lastAttackPressTime > amountOfTimeBetweenAttacks)
        {
            Debug.Log("Combo Reset");
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        currentComboAmount.Clear();
    }

    private IEnumerator TurnOffHitboxes(BoxCollider box) 
    {
        yield return new WaitForSeconds(.2f);
        box.enabled = false;
    
    }

}
