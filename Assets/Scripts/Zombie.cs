using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Zombie : EnemyAI
{
    private Transform psychoTransform;
    private EnemyHealth psychoHealth;
    private float psychoSearchTimer;
    private const float PSYCHO_SEARCH_INTERVAL = 2f;

    protected override void FindAllTargets()
    {
        base.FindAllTargets(); // Вызываем базовый поиск

        // Периодический поиск психа
        psychoSearchTimer += Time.deltaTime;
        if (psychoSearchTimer >= PSYCHO_SEARCH_INTERVAL)
        {
            FindPsycho();
            psychoSearchTimer = 0f;
        }
    }

    private void FindPsycho()
    {
        GameObject psychoObject = GameObject.FindGameObjectWithTag("Psycho");
        if (psychoObject != null)
        {
            psychoTransform = psychoObject.transform;
            psychoHealth = psychoObject.GetComponent<EnemyHealth>();
        }
    }

    protected override Transform GetSpecialTarget()
    {
        // Для зомби специальная цель - псих
        if (psychoHealth != null && psychoHealth.currentHealth > 0)
        {
            return psychoTransform;
        }
        return null;
    }

    protected override void AttackImplementation()
    {
        if (currentTarget != null)
        {
            transform.LookAt(currentTarget);

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            CompanionHealth companion = currentTarget.GetComponent<CompanionHealth>();
            if (companion != null)
            {
                companion.TakeDamage(attackDamage);
                return;
            }

            if (currentTarget == playerTransform && playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                return;
            }

            if (currentTarget == psychoTransform && psychoHealth != null)
            {
                psychoHealth.TakeDamage(attackDamage);
            }
        }
    }

    // Для обратной совместимости
    public void ForceChasePlayer(Transform playerTarget)
    {
        ForceChaseTarget(playerTarget);
    }
}