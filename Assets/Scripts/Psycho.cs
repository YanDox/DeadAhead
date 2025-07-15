using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Psycho : EnemyAI
{
    [Header("Psycho Specific Settings")]
    public LayerMask zombieLayer; // Слой зомби
    private List<Zombie> zombiesInRange = new List<Zombie>();

    protected override void FindAllTargets()
    {
        base.FindAllTargets(); // Базовый поиск компаньонов и игрока

        // Поиск зомби
        zombiesInRange.Clear();
        int numZombies = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            hitColliders,
            zombieLayer
        );

        for (int i = 0; i < numZombies; i++)
        {
            Zombie zombie = hitColliders[i].GetComponent<Zombie>();
            if (zombie != null) zombiesInRange.Add(zombie);
        }
    }

    protected override Transform GetSpecialTarget()
    {
        // Для психа специальная цель - ближайший зомби
        Zombie closestZombie = null;
        float minDistance = float.MaxValue;

        foreach (var zombie in zombiesInRange)
        {
            if (zombie != null)
            {
                float distance = Vector3.Distance(transform.position, zombie.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestZombie = zombie;
                }
            }
        }
        return closestZombie?.transform;
    }

    protected override void AttackImplementation()
    {
        if (currentTarget != null)
        {
            transform.LookAt(new Vector3(
                currentTarget.position.x,
                transform.position.y,
                currentTarget.position.z
            ));

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

            if (currentTarget.CompareTag("Player") && playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                return;
            }

            if (currentTarget.CompareTag("Zombie"))
            {
                EnemyHealth zombieHealth = currentTarget.GetComponent<EnemyHealth>();
                if (zombieHealth != null) zombieHealth.TakeDamage(attackDamage);
            }
        }
    }
}