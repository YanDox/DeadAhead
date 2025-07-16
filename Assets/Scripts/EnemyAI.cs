using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public abstract class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Patrolling,
        Chasing,
        Attacking,
        Returning
    }

    [Header("Common Settings")]
    public float detectionRadius = 10f;
    public float chaseRadius = 15f;
    public float patrolRadius = 20f;
    public float patrolPointMinDistance = 5f;
    public float patrolWaitTime = 2f;
    public float attackRange = 2f;
    public int attackDamage = 15;
    public float attackCooldown = 1f;
    
    [Header("Target Settings")]
    public LayerMask companionLayer;
    
    protected NavMeshAgent navMeshAgent;
    protected Transform currentTarget;
    protected EnemyState currentState = EnemyState.Patrolling;
    protected Vector3 spawnPosition;
    protected Vector3 currentPatrolPoint;
    protected Animator animator;
    protected float waitTimer = 0f;
    protected bool isWaiting = false;
    protected float lastAttackTime;

    [Header("Enemy Interactions")]
    public string[] enemyTags = new string[0];
    public bool canAttackOtherEnemies = true;

    protected Transform playerTransform;
    protected Transform enemyTransform;
    protected PlayerHealth playerHealth;
    protected EnemyHealth enemyHealth;
    protected List<CompanionHealth> companionsInRange = new List<CompanionHealth>();
    protected Collider[] hitColliders = new Collider[20];

    protected virtual void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;
        FindPlayer();
        SetRandomPatrolPoint();
    }

    protected virtual void Update()
    {
        FindAllTargets();
        ChooseMainTarget();

        if (currentTarget == null)
        {
            return;
            if (currentState != EnemyState.Patrolling && currentState != EnemyState.Returning)
            {
                currentState = EnemyState.Returning;
                navMeshAgent.SetDestination(spawnPosition);
            }
        }

        //if (currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        switch (currentState)
        {
            case EnemyState.Patrolling:
                UpdatePatrolling(distanceToTarget);
                break;

            case EnemyState.Chasing:
                UpdateChasing(distanceToTarget);
                break;

            case EnemyState.Attacking:
                UpdateAttacking(distanceToTarget);
                break;

            case EnemyState.Returning:
                UpdateReturning(distanceToTarget);
                break;
        }

        UpdateAnimation();
    }

    protected virtual void FindAllTargets()
    {
        FindCompanions();
        
        if (playerTransform == null)
        {
            FindPlayer();
        }
    }

    protected virtual void ChooseMainTarget()
    {
        // 1. Приоритет: ближайший живой компаньон
        CompanionHealth closestCompanion = FindClosestCompanion();
        if (closestCompanion != null)
        {
            currentTarget = closestCompanion.transform;
            return;
        }

        // 2. Приоритет: игрок (если жив)
        if (playerHealth != null && playerHealth.currentHealth > 0)
        {
            currentTarget = playerTransform;
            return;
        }

        // 3. Приоритет: специальная цель (реализуется в дочерних классах)
        currentTarget = GetSpecialTarget();
    }

    //// 1. Приоритет: ближайший живой компаньон
    //protected virtual void ChooseMainTarget()
    //{
    //    // 0. Если текущая цель валидна - оставляем её
    //    if (currentTarget != null && IsTargetValid(currentTarget))
    //    {
    //        // Проверяем расстояние для текущей цели
    //        float distance = Vector3.Distance(transform.position, currentTarget.position);

    //        // Если цель в пределах chaseRadius - продолжаем преследовать
    //        if (distance <= chaseRadius)
    //        {
    //            return;
    //        }
    //    }

    //    // 1. Проверка компаньонов с расстоянием
    //    CompanionHealth closestCompanion = FindClosestCompanion();
    //    if (closestCompanion != null)
    //    {
    //        float companionDistance = Vector3.Distance(transform.position, closestCompanion.transform.position);
    //        if (companionDistance <= detectionRadius)
    //        {
    //            currentTarget = closestCompanion.transform;
    //            return;
    //        }
    //    }

    //    // 2. Проверка игрока с расстоянием
    //    if (playerTransform != null && playerHealth != null && playerHealth.currentHealth > 0)
    //    {
    //        float playerDistance = Vector3.Distance(transform.position, playerTransform.position);
    //        if (playerDistance <= detectionRadius)
    //        {
    //            currentTarget = playerTransform;
    //            return;
    //        }
    //    }

    //    // 3. Специальная цель с проверкой расстояния
    //    Transform specialTarget = GetSpecialTarget();
    //    if (specialTarget != null)
    //    {
    //        float specialDistance = Vector3.Distance(transform.position, specialTarget.position);
    //        if (specialDistance <= detectionRadius)
    //        {
    //            currentTarget = specialTarget;
    //            return;
    //        }
    //    }
    //}

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerHealth = playerTransform.GetComponent<PlayerHealth>();
        }
    }

    private void FindCompanions()
    {
        companionsInRange.Clear();
        int numCompanions = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            hitColliders,
            companionLayer
        );

        for (int i = 0; i < numCompanions; i++)
        {
            CompanionHealth companion = hitColliders[i].GetComponent<CompanionHealth>();
            if (companion != null && companion.currentHealth > 0)
            {
                companionsInRange.Add(companion);
            }
        }
    }

    private CompanionHealth FindClosestCompanion()
    {
        CompanionHealth closest = null;
        float minDistance = float.MaxValue;
        
        foreach (var companion in companionsInRange)
        {
            if (companion != null)
            {
                float distance = Vector3.Distance(transform.position, companion.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = companion;
                }
            }
        }
        return closest;
    }

    protected abstract Transform GetSpecialTarget();
    protected abstract void AttackImplementation();

    protected virtual void UpdatePatrolling(float distanceToTarget)
    {
        if (currentTarget != null && distanceToTarget <= detectionRadius)
        {
            currentState = EnemyState.Chasing;
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

    protected virtual void UpdateChasing(float distanceToTarget)
    {
        if (currentTarget == null || distanceToTarget > chaseRadius)
        {
            currentState = EnemyState.Returning;
            navMeshAgent.SetDestination(spawnPosition);
            return;
        }

        navMeshAgent.SetDestination(currentTarget.position);

        if (!IsTargetValid(currentTarget))
        {
            currentTarget = null;
            currentState = EnemyState.Returning;
            return;
        }

        if (distanceToTarget <= attackRange)
        {
            currentState = EnemyState.Attacking;
        }
    }

    protected virtual void UpdateAttacking(float distanceToTarget)
    {
        if (currentTarget == null || !IsTargetValid(currentTarget))
        {
            Debug.Log($"{gameObject.name} lost target or invalid: switching to chasing.");
            currentState = EnemyState.Chasing;
            return;
        }

        if (distanceToTarget > attackRange * 1.2f)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            AttackImplementation();
            lastAttackTime = Time.time;
        }
    }

    protected bool IsTargetValid(Transform target)
    {
        if (target.CompareTag(gameObject.tag))
            return false;

        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        if (target == playerTransform && playerHealth != null)
            return playerHealth.currentHealth > 0;

        CompanionHealth companion = target.GetComponent<CompanionHealth>();
        if (companion != null)
            return companion.currentHealth > 0;

        EnemyHealth enemy = target.GetComponent<EnemyHealth>();
        if (enemy != null)
            return !enemy.isDead;

        return true;
    }

    protected virtual void UpdateReturning(float distanceToTarget)
    {
        if (currentTarget != null && distanceToTarget <= detectionRadius)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        if (navMeshAgent.destination != spawnPosition)
        {
            navMeshAgent.SetDestination(spawnPosition);
        }

        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            currentState = EnemyState.Patrolling;
            SetRandomPatrolPoint();
        }
    }

    protected virtual void SetRandomPatrolPoint(int attempts = 5)
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

    protected virtual void UpdateAnimation()
    {
        if (animator != null)
        {
            bool isMoving = navMeshAgent.velocity.magnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);
            animator.SetBool("IsChasing", currentState == EnemyState.Chasing || currentState == EnemyState.Attacking);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnPosition, patrolRadius);
    }

    public virtual void ForceChaseTarget(Transform target)
    {
        if (target != null)
        {
            currentTarget = target;
            currentState = EnemyState.Chasing;
            
            isWaiting = false;
            waitTimer = 0f;
            
            navMeshAgent.SetDestination(target.position);
            
            UpdateAnimation();
        }
    }
}