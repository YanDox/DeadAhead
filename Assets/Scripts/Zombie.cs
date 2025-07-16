using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Zombie : EnemyAI
{
    private Transform psychoTransform;
    private EnemyHealth psychoHealth;
    private float psychoSearchTimer;
    private const float PSYCHO_SEARCH_INTERVAL = 2f;

    protected override void Start()
    {
        base.Start();

        // Настройки для зомби
        canAttackOtherEnemies = true;
        enemyTags = new string[] { "Psycho" }; // Атакуем психов по тегу
    }

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
        psychoTransform = null;
        psychoHealth = null;

        GameObject psychoObject = GameObject.FindGameObjectWithTag("Psycho");
        if (psychoObject != null)
        {
            EnemyHealth health = psychoObject.GetComponent<EnemyHealth>();
            if (health != null && !health.isDead && health.enabled)
            {
                psychoTransform = psychoObject.transform;
                psychoHealth = health;
            }
        }
    }

    protected override Transform GetSpecialTarget()
    {
        // Для зомби специальная цель - псих
        if (psychoHealth != null && psychoHealth.currentHealth > 0 && !psychoHealth.isDead)
        {
            float dist = Vector3.Distance(transform.position, psychoTransform.position);
            if (dist <= detectionRadius) // <-- ЭТО ДОБАВИЛСЯ ПОСЛЕ
            {
                return psychoTransform;
            }
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
                // Принудительное обновление целей
                FindAllTargets();
                ChooseMainTarget();
                if (psychoHealth != null)
                {
                    psychoHealth.TakeDamage(attackDamage);

                    // если цель умерла — сразу очищаем и ищем новую
                    if (psychoHealth.isDead)
                    {
                        currentTarget = null;
                        currentState = EnemyState.Returning;
                    }
                    return;
                }
            }
        }
    }

    // Для обратной совместимости
    public void ForceChasePlayer(Transform playerTarget)
    {
        ForceChaseTarget(playerTarget);
    }
}