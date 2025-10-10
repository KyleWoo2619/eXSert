using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int maxComboAmount = 3;
    [SerializeField] private float amountOfTimeBetweenAttacks = 1.5f;
    protected float lastAttackPressTime;
    private int currentComboAmount;
    private InputReader input;
    [SerializeField] BoxCollider[] playerAttackHitboxes;
    private ChangeStance changeStance;

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
        if (changeStance.currentStance == 0)
        {

            if (input.AttackTrigger)
            {
                currentComboAmount++;
                lastAttackPressTime = Time.time;
                input.AttackTrigger = false;


                Debug.Log("Combo Amount: " + currentComboAmount);
                Debug.Log(Time.time - lastAttackPressTime);

                switch (currentComboAmount)
                {
                    case 1:
                        Debug.Log("Attack 1");
                        playerAttackHitboxes[0].enabled = true;
                        StartCoroutine(TurnOffHitboxes(playerAttackHitboxes[0]));
                        break;

                    case 2:
                        Debug.Log("Attack 2");
                        playerAttackHitboxes[1].enabled = true;
                        StartCoroutine(TurnOffHitboxes(playerAttackHitboxes[1]));
                        break;

                    case 3:
                        Debug.Log("Attack 3");
                        playerAttackHitboxes[2].enabled = true;
                        StartCoroutine(TurnOffHitboxes(playerAttackHitboxes[2]));
                        break;
                }

            }
        }
        else if (changeStance.currentStance == 1)
        {
            {
                if (input.AttackTrigger)
                {
                    currentComboAmount++;
                    lastAttackPressTime = Time.time;
                    input.AttackTrigger = false;


                    Debug.Log("Combo Amount: " + currentComboAmount);
                    Debug.Log(Time.time - lastAttackPressTime);

                    switch (currentComboAmount)
                    {
                        case 1:
                            Debug.Log("Attack 1");
                            playerAttackHitboxes[0].enabled = true;
                            StartCoroutine(TurnOffHitboxes(playerAttackHitboxes[0]));
                            break;

                        case 2:
                            Debug.Log("Attack 2");
                            playerAttackHitboxes[1].enabled = true;
                            StartCoroutine(TurnOffHitboxes(playerAttackHitboxes[1]));
                            break;

                        case 3:
                            Debug.Log("Attack 3");
                            playerAttackHitboxes[3].enabled = true;
                            StartCoroutine(TurnOffHitboxes(playerAttackHitboxes[3]));
                            break;
                    }
                }
            }
        }

        if (currentComboAmount > maxComboAmount)
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
        if (currentComboAmount > 0 )
        {
            currentComboAmount = 0;
        }
    }

    private IEnumerator TurnOffHitboxes(BoxCollider box) 
    {
        yield return new WaitForSeconds(.2f);
        box.enabled = false;
    
    }

}
