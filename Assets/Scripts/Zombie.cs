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
    public int attackDamage = 15;
    public float attackCooldown = 1f;
    public LayerMask companionLayer;

    private NavMeshAgent navMeshAgent;
    private Transform playerTransform;
    private PlayerHealth playerHealth;
    private Transform currentTarget;
    private ZombieState currentState = ZombieState.Patrolling;
    private Vector3 spawnPosition;
    private Vector3 currentPatrolPoint;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private float lastAttackTime;
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

    void InitializeTargets()
    {
        var playerController = FindObjectOfType<SC_TPSController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerHealth = playerController.GetComponent<PlayerHealth>();
            currentTarget = playerTransform;
        }
    }
    #endregion

#region Update
    void Update()
    {
        if (playerTransform == null)
        {
            InitializeTargets();
            if (playerTransform == null) return;
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
        Collider[] companions = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            companionLayer
        );

        Transform closestCompanion = null;
        float closestDistance = float.MaxValue;

        foreach (var col in companions)
        {
            // Проверяем наличие компонента, но не проверяем "живость"
            if (col.GetComponent<CompanionHealth>() != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCompanion = col.transform;
                }
            }
        }

        if (closestCompanion != null)
        {
            currentTarget = closestCompanion;
        }
        else if (playerHealth != null)
        {
            currentTarget = playerTransform;
        }
        else
        {
            currentTarget = null;
        }
    }

    private Psycho FindPsychoInRange()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Psycho"))
            {
                return collider.GetComponent<Psycho>();
            }
        }
        return null;
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

        navMeshAgent.SetDestination(currentTarget.position);

        if (distanceToTarget <= attackRange)
        {
            AttackTarget();
        }
    }

    private void AttackTarget()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            if (currentTarget != null) transform.LookAt(currentTarget);

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Атака основной цели
            if (currentTarget != null)
            {
                CompanionHealth companion = currentTarget.GetComponent<CompanionHealth>();
                if (companion != null)
                {
                    companion.TakeDamage(attackDamage);
                }
                else if (currentTarget == playerTransform && playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
            }

            // Атака всех компаньонов в радиусе
            Collider[] companionsInRange = Physics.OverlapSphere(
                transform.position,
                attackRange,
                companionLayer
            );

            foreach (var col in companionsInRange)
            {
                CompanionHealth companion = col.GetComponent<CompanionHealth>();
                if (companion != null)
                {
                    companion.TakeDamage(attackDamage);
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