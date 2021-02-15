using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    //This script is for the battle system that this game uses
    //The following variables are public for easy setting in the inspector
    //They can be set to private if you can pull them into the inspector
    //public camShakeScript CSS;
    public GameManager gameMan;
    //This is used to check if the battle has been finished
    private bool isBattleOngoing;

    //In: two unit gameObjects the attacker and the receiver
    //Out: this plays the animations for the battle
    //Desc: this function calls all the functions for the battle 
    public IEnumerator attack(GameObject unit, GameObject enemy)
    {
        isBattleOngoing = true;
        float elapsedTime = 0;
        Vector3 startingPos = unit.transform.position;
        Vector3 endingPos = enemy.transform.position;

        while (elapsedTime < .25f)
        {
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        while (isBattleOngoing)
        {
            //StartCoroutine(CSS.camShake(.2f, unit.GetComponent<UnitScript>().attackDamage, getDirection(unit, enemy)));
            if (unit.GetComponent<Unit>().attackRange == enemy.GetComponent<Unit>().attackRange && enemy.GetComponent<Unit>().currentHealthPoints - unit.GetComponent<Unit>().attackDamage > 0)
            {
                //StartCoroutine(unit.GetComponent<Unit>().displayDamageEnum(enemy.GetComponent<Unit>().attackDamage));
                //StartCoroutine(enemy.GetComponent<Unit>().displayDamageEnum(unit.GetComponent<Unit>().attackDamage));
            }

            else
            {
                //StartCoroutine(enemy.GetComponent<Unit>().displayDamageEnum(unit.GetComponent<Unit>().attackDamage));
            }

            //unit.GetComponent<UnitScript>().displayDamage(enemy.GetComponent<UnitScript>().attackDamage);
            //enemy.GetComponent<UnitScript>().displayDamage(unit.GetComponent<UnitScript>().attackDamage);

            if (unit.GetComponent<Unit>().teamNum != enemy.GetComponent<Unit>().teamNum)
            {
                battle(unit, enemy);
            }else
            {
                support(unit, enemy);
            }
            yield return new WaitForEndOfFrame();
        }

    }

    private void battle(GameObject initiator, GameObject recipient)
    {
        isBattleOngoing = true;
        var initiatorUnit = initiator.GetComponent<Unit>();
        var recipientUnit = recipient.GetComponent<Unit>();
        bool rangedAttack = false;
        if (Mathf.Abs(initiatorUnit.x - recipientUnit.x) + Mathf.Abs(initiatorUnit.y - recipientUnit.y) > 1)
        {
            print("Ranged attack");
            rangedAttack = true;
        }

        int initiatorAtt = initiatorUnit.attackDamage;
        bool initiatorCrit = false;
        //check for crit
        if(Random.Range(1,100) <= initiatorUnit.chanceToCrit - recipientUnit.critAvoid)
        {
            print("CRITICAL HIT; attacker");
            initiatorAtt *= 2;
            initiatorCrit = true;
        }

        //If the two units have the same attackRange then they can trade
        if (!rangedAttack && (recipientUnit.attackRange.x == 1))
        {
            //GameObject tempParticle = Instantiate(recipientUnit.GetComponent<Unit>().damagedParticle, recipient.transform.position, recipient.transform.rotation);
            //Destroy(tempParticle, 2f);
            recipientUnit.takeDamage(initiatorAtt, initiatorCrit);
            if (recipientUnit.checkIfDead())
            {
                //Set to null then remove, if the gameObject is destroyed before its removed it will not check properly
                //This leads to the game not actually ending because the check to see if any units remains happens before the object
                //is removed from the parent, so we need to parent to null before we destroy the gameObject.
                recipient.transform.parent = null;
                recipientUnit.unitDie();
                isBattleOngoing = false;
                gameMan.checkIfUnitsRemain(initiator, recipient);
                return;
            }

            int recipientAtt = Mathf.FloorToInt(recipientUnit.attackDamage * recipientUnit.crackbackModifier);
            bool recipientCrit = false;
            if (Random.Range(1, 100) <= recipientUnit.chanceToCrit - initiatorUnit.critAvoid)
            {
                print("CRITICAL HIT; defender");
                recipientAtt *= 2;
                recipientCrit = true;
            }
            if (recipientAtt > 0)
            {
                initiatorUnit.takeDamage(recipientAtt, recipientCrit);
            }

            if (initiatorUnit.checkIfDead())
            {
                initiator.transform.parent = null;
                initiatorUnit.unitDie();
                isBattleOngoing = false;
                gameMan.checkIfUnitsRemain(initiator, recipient);
                return;

            }
        }
        //if the units don't have the same attack range, like a swordsman vs an archer; the recipient cannot strike back
        else
        {
            //GameObject tempParticle = Instantiate(recipientUnit.GetComponent<Unit>().damagedParticle, recipient.transform.position, recipient.transform.rotation);
            //Destroy(tempParticle, 2f);

            recipientUnit.takeDamage(initiatorAtt, initiatorCrit);
            if (recipientUnit.checkIfDead())
            {
                recipient.transform.parent = null;
                recipientUnit.unitDie();
                isBattleOngoing = false;
                gameMan.checkIfUnitsRemain(initiator, recipient);
                return;
            }
        }
        isBattleOngoing = false;
    }

    private void support(GameObject initiator, GameObject recipient)
    {
        print("Supporting a unit!");

        isBattleOngoing = true;
        var initiatorUnit = initiator.GetComponent<Unit>();
        var recipientUnit = recipient.GetComponent<Unit>();

        int initiatorSupport = Random.Range(2*initiatorUnit.powerAmount, 4*initiatorUnit.powerAmount);
        bool healCrit = false;
        //check for crit
        if (Random.Range(1, 100) <= initiatorUnit.chanceToCrit)
        {
            print("CRITICAL SUPPORT");
            initiatorSupport *= 2;
            healCrit = true;
        }

        recipientUnit.heal(initiatorSupport, healCrit);

        isBattleOngoing = false;
        return;
    }
}
