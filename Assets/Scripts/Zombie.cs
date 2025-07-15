using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour
{
    public enum ZombieState
    {
        Patrolling,
        Chasing,
        Returning
    }

    #region Settings
    [Header("Settings")]
    public float detectionRadius = 10f;
    public float chaseRadius = 15f;
    public float patrolRadius = 20f;
    public float patrolPointMinDistance = 5f;
    public float patrolWaitTime = 2f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public int attackDamage = 15;
    public LayerMask companionLayer;

    private ZombieState currentState = ZombieState.Patrolling;
    private NavMeshAgent navMeshAgent;
    private Transform playerTransform;
    private Transform psychoTransform;
    private Transform currentTarget;
    private EnemyHealth psychoHealth;
    private PlayerHealth playerHealth;
    private Vector3 spawnPosition;
    private Vector3 currentPatrolPoint;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float lastAttackTime;
    private float psychoSearchTimer;
    private const float PSYCHO_SEARCH_INTERVAL = 2f;
    private Animator animator;
    #endregion

    #region Start
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
        InitializeTargets();
        SetRandomPatrolPoint();
    }
    #endregion

    #region Update
    void Update()
    {
        psychoSearchTimer += Time.deltaTime;
        if (psychoSearchTimer >= PSYCHO_SEARCH_INTERVAL)
        {
            InitializeTargets();
            psychoSearchTimer = 0f;
        }

        UpdateCurrentTarget();

        float distanceToTarget = currentTarget != null
            ? Vector3.Distance(transform.position, currentTarget.position)
            : float.MaxValue;

        switch (currentState)
        {
            case ZombieState.Patrolling:
                UpdatePatrolling(distanceToTarget);
                break;

            case ZombieState.Chasing:
                UpdateChasing(distanceToTarget);
                break;

            case ZombieState.Returning:
                UpdateReturning(distanceToTarget);
                break;
        }

        UpdateAnimation();
    }
    #endregion

    #region Target
    private void UpdateCurrentTarget()
    {
        // 1. Всегда сначала проверяем компаньонов (высший приоритет)
        Collider[] companions = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            companionLayer
        );

        Transform closestCompanion = null;
        float closestCompanionDistance = float.MaxValue;

        foreach (var col in companions)
        {
            CompanionHealth companion = col.GetComponent<CompanionHealth>();
            if (companion != null && companion.currentHealth > 0)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestCompanionDistance)
                {
                    closestCompanionDistance = distance;
                    closestCompanion = col.transform;
                }
            }
        }

        if (closestCompanion != null)
        {
            currentTarget = closestCompanion;
            return;
        }

        // 2. Проверяем игрока (второй приоритет)
        float playerDistance = float.MaxValue;
        bool playerAlive = false;

        if (playerTransform != null && playerHealth != null)
        {
            playerDistance = Vector3.Distance(transform.position, playerTransform.position);
            playerAlive = playerHealth.currentHealth > 0;
        }

        if (playerAlive && playerDistance <= detectionRadius)
        {
            currentTarget = playerTransform;
            return;
        }

        // 3. Проверяем психа (низший приоритет)
        float psychoDistance = float.MaxValue;
        bool psychoAlive = false;

        if (psychoTransform != null && psychoHealth != null)
        {
            psychoDistance = Vector3.Distance(transform.position, psychoTransform.position);
            psychoAlive = psychoHealth.currentHealth > 0;
        }

        if (psychoAlive && psychoDistance <= detectionRadius)
        {
            currentTarget = psychoTransform;
            return;
        }

        // 4. Если нет подходящих целей
        currentTarget = null;
    }

    void InitializeTargets()
    {
        // Инициализация игрока (один раз)
        if (playerTransform == null)
        {
            var playerController = FindObjectOfType<SC_TPSController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                playerHealth = playerController.GetComponent<PlayerHealth>();
            }
        }

        // Периодический поиск психа
        if (psychoHealth == null)
        {
            GameObject psychoObject = GameObject.FindGameObjectWithTag("Psycho");
            if (psychoObject != null)
            {
                psychoTransform = psychoObject.transform;
                psychoHealth = psychoObject.GetComponent<EnemyHealth>();
            }
        }
    }
    #endregion

    #region Patrolling
    private void UpdatePatrolling(float distanceToTarget)
    {
        if (distanceToTarget <= detectionRadius && currentTarget != null)
        {
            currentState = ZombieState.Chasing;
            return;
        }

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = 0f;
            }
            else
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= patrolWaitTime)
                {
                    isWaiting = false;
                    SetRandomPatrolPoint();
                }
            }
        }
    }

    private void SetRandomPatrolPoint(int attempts = 5)
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += spawnPosition;

        if (attempts <= 0)
        {
            navMeshAgent.SetDestination(spawnPosition);
            return;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            if (Vector3.Distance(transform.position, hit.position) >= patrolPointMinDistance)
            {
                currentPatrolPoint = hit.position;
                navMeshAgent.SetDestination(currentPatrolPoint);
                return;
            }
        }
        SetRandomPatrolPoint(attempts - 1);
    }
    #endregion

    #region Chasing
    private void UpdateChasing(float distanceToTarget)
    {
        if (currentTarget == null || distanceToTarget > chaseRadius)
        {
            currentState = ZombieState.Returning;
            navMeshAgent.SetDestination(spawnPosition);
            return;
        }

        // Проверяем живость цели
        bool targetAlive = true;
        if (currentTarget == playerTransform && playerHealth != null)
        {
            targetAlive = playerHealth.currentHealth > 0;
        }
        else if (currentTarget == psychoTransform && psychoHealth != null)
        {
            targetAlive = psychoHealth.currentHealth > 0;
        }
        else
        {
            CompanionHealth companion = currentTarget.GetComponent<CompanionHealth>();
            if (companion != null) targetAlive = companion.currentHealth > 0;
        }

        if (!targetAlive)
        {
            currentState = ZombieState.Returning;
            navMeshAgent.SetDestination(spawnPosition);
            return;
        }

        navMeshAgent.SetDestination(currentTarget.position);

        if (distanceToTarget <= attackRange)
        {
            AttackTarget();
        }
    }

    private void AttackTarget()
    {
        if (Time.time - lastAttackTime >= attackCooldown && currentTarget != null)
        {
            lastAttackTime = Time.time;
            transform.LookAt(currentTarget);

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Основная атака
            CompanionHealth companion = currentTarget.GetComponent<CompanionHealth>();
            if (companion != null)
            {
                companion.TakeDamage(attackDamage);
            }
            else if (currentTarget == playerTransform && playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
            else if (currentTarget == psychoTransform && psychoHealth != null)
            {
                psychoHealth.TakeDamage(attackDamage);
            }

            // Атака всех компаньонов в радиусе
            Collider[] companionsInRange = Physics.OverlapSphere(
                transform.position,
                attackRange,
                companionLayer
            );

            foreach (var col in companionsInRange)
            {
                if (col.transform != currentTarget) // Не атакуем основную цель повторно
                {
                    CompanionHealth additionalCompanion = col.GetComponent<CompanionHealth>();
                    if (additionalCompanion != null)
                    {
                        additionalCompanion.TakeDamage(attackDamage);
                    }
                }
            }
        }
    }
    #endregion

    #region Returning
    private void UpdateReturning(float distanceToTarget)
    {
        if (distanceToTarget <= detectionRadius && currentTarget != null)
        {
            currentState = ZombieState.Chasing;
            return;
        }

        if (navMeshAgent.destination != spawnPosition)
        {
            navMeshAgent.SetDestination(spawnPosition);
        }

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            currentState = ZombieState.Patrolling;
            SetRandomPatrolPoint();
        }
    }
    #endregion

    #region Animation
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            bool isMoving = navMeshAgent.velocity.magnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsChasing", currentState == ZombieState.Chasing);
        }
    }
    #endregion

    #region Utils
    public void ForceChasePlayer(Transform playerTarget)
    {
        if (playerTarget != null)
        {
            currentTarget = playerTarget;
            currentState = ZombieState.Chasing;
            navMeshAgent.SetDestination(currentTarget.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPosition, patrolRadius);
        }
    }
}
#endregion