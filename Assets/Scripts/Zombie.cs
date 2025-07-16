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
        if (psychoHealth != null && psychoHealth.currentHealth > 0 && !psychoHealth.isDead)
        {
            // Решение: добавить проверку расстояния
            float dist = Vector3.Distance(transform.position, psychoTransform.position);
            if (dist <= detectionRadius)
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

            // Основная логика атаки
            if (currentTarget == psychoTransform && psychoHealth != null)
            {
                // Сохраняем ссылку перед атакой
                EnemyHealth targetHealth = psychoHealth;

                targetHealth.TakeDamage(attackDamage);

                // Проверяем состояние цели ПОСЛЕ атаки
                if (targetHealth.isDead)
                {
                    // Сбрасываем только если текущая цель - псих
                    if (currentTarget == psychoTransform)
                    {
                        currentTarget = null;
                        psychoTransform = null;
                        psychoHealth = null;
                    }
                }
                return;
            }
        }
    }

    // Для обратной совместимости
    public void ForceChasePlayer(Transform playerTarget)
    {
        ForceChaseTarget(playerTarget);
    }
}