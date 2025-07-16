using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyHealth))]
public class ZombieCommander : EnemyAI
{
    [Header("Commander Settings")]
    public float callReinforcementsRadius = 100f;
    public float callReinforcementsCooldown = 5f;
    public LayerMask reinforcementLayer;
    public float reinforcementCallHealthThreshold = 0.5f;
    public GameObject callEffectPrefab;
    public AudioClip callSound;

    private Transform psychoTransform;
    private EnemyHealth psychoHealth;
    private float psychoSearchTimer;
    private const float PSYCHO_SEARCH_INTERVAL = 2f;

    private float lastCallTime;
    private AudioSource audioSource;
    private EnemyHealth commanderHealth;

    protected override void Start()
    {
        // Настройки командира
        detectionRadius = 10f;
        chaseRadius = 15f;
        patrolRadius = 20f;
        patrolPointMinDistance = 5f;
        patrolWaitTime = 2f;
        attackRange = 1.5f;
        attackDamage = 15;
        attackCooldown = 1f;

        canAttackOtherEnemies = true;
        enemyTags = new string[] { "Psycho" };

        base.Start();

        audioSource = GetComponent<AudioSource>();
        commanderHealth = GetComponent<EnemyHealth>();
    }

    protected override void Update()
    {
        base.Update();

        // Проверка условий для вызова подкреплений
        if (commanderHealth != null &&
            !commanderHealth.isDead &&
            (currentState == EnemyState.Chasing || currentState == EnemyState.Attacking) &&
            Time.time - lastCallTime >= callReinforcementsCooldown &&
            commanderHealth.currentHealth <= commanderHealth.maxHealth * reinforcementCallHealthThreshold)
        {
            CallReinforcements();
            lastCallTime = Time.time;
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
        if (currentTarget == null) return;

        transform.LookAt(currentTarget);

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Нанесение урона в зависимости от типа цели
        CompanionHealth companion = currentTarget.GetComponent<CompanionHealth>();
        if (companion != null)
        {
            companion.TakeDamage(attackDamage);
            return;
        }

        PlayerHealth player = currentTarget.GetComponent<PlayerHealth>();
        if (player != null)
        {
            player.TakeDamage(attackDamage);
            return;
        }

        EnemyHealth enemy = currentTarget.GetComponent<EnemyHealth>();
        if (enemy != null && canAttackOtherEnemies)
        {
            enemy.TakeDamage(attackDamage);
        }
    }

    private void CallReinforcements()
    {
        Collider[] hitColliders = new Collider[20];
        int numColliders = Physics.OverlapSphereNonAlloc(
            transform.position,
            callReinforcementsRadius,
            hitColliders,
            reinforcementLayer
        );

        for (int i = 0; i < numColliders; i++)
        {
            Zombie zombie = hitColliders[i].GetComponent<Zombie>();
            if (zombie != null && zombie != this)
            {
                zombie.ForceChaseTarget(playerTransform);
            }
        }

        // Визуальные и звуковые эффекты
        if (callEffectPrefab != null)
        {
            Instantiate(callEffectPrefab, transform.position, Quaternion.identity);
        }

        if (callSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(callSound);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, callReinforcementsRadius);
    }
}