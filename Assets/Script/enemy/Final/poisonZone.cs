using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class poisonZone : MonoBehaviour
{
    public int poisonDamage = 1;
    public float damageInterval = 1f;

    private Coroutine poisonCoroutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player playerScript = other.GetComponent<player>();
            if (playerScript != null && poisonCoroutine == null)
            {
                poisonCoroutine = StartCoroutine(ApplyPoisonDamage(playerScript));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (poisonCoroutine != null)
            {
                StopCoroutine(poisonCoroutine);
                poisonCoroutine = null;
            }
        }
    }

    private IEnumerator ApplyPoisonDamage(player playerScript)
    {
        while (true)
        {
            if (playerScript != null && playerScript.alive && !playerScript.isInvulnerable)
            {
                playerScript.currenthealth -= poisonDamage;
                playerScript.UpdateHealth();
                playerScript.StartCoroutine(playerScript.TemporaryInvulnerability());
                if (playerScript.regenCoroutine != null)
                {
                    playerScript.StopCoroutine(playerScript.regenCoroutine);
                    playerScript.regenCoroutine = null;
                }
                Debug.Log("☠️ Poison zone damage: " + poisonDamage);
            }

            yield return new WaitForSeconds(damageInterval);
        }
    }
}
