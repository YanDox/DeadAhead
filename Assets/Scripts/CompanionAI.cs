using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CompanionAI : MonoBehaviour
{
    public enum CompanionState
    {
        Staying,
        Following,
        GoingToWorkshop,
        Defense,
        Getaway
    }

    [Header("Follow Settings")]
    public float followDistance = 10f;
    public float stopDistance = 20f;
    public float minFollowDistance = 3f;
    public float movementSpeed = 5f;
    public float rotationSpeed = 4f;

    [Header("Stay Point Settings")]
    public float stayPointRadius = 7f;
    public float detectionRadius = 15f;
    public Transform targetStayPoint;
    public KeyCode stayToggleKey = KeyCode.E;

    [Header("Detection")]
    public float checkInterval = 0.1f;
    public LayerMask obstacleLayers;

    [Header("Collision Settings")]
    public float collisionCooldown = 0.5f;
    public float repulsionForce = 5f;
    public float minDistance = 0.35f;

    [Header("Zombie Reaction Settings")]
    public float zombieDetectionRadius = 15f;
    public float reactionCooldown = 1f;
    public float shootingChance = 1f;
    public LayerMask zombieLayerMask;

    [Header("Melee Attack Settings")]
    public float attackDistance = 2f;
    public float attackRate = 1f;
    public float attackDuration = 2f;
    public float chaseSpeed = 7f;
    public float attackDamage = 1000f;

    private CompanionState currentState = CompanionState.Staying;
    private CompanionState stateBeforeReaction;
    private float lastCheckTime;
    private float lastCollisionTime;
    private float lastReactionTime;
    private float nextAttackTime;
    private bool workshopDetected = false;
    private bool isManualStay = false;
    private bool isReactingToZombie = false;
    private SC_TPSController player;
    private NavMeshAgent agent;
    private CompanionHealth health;
    private Transform currentTarget;
    //private Animator animator;

    void Start()
    {
        FindWorkshop();
        player = FindObjectOfType<SC_TPSController>();
        health = GetComponent<CompanionHealth>();
        nextAttackTime = 0f;
        currentTarget = null;

        agent = GetComponent<NavMeshAgent>();
        //animator = GetComponent<Animator>();

        agent.speed = movementSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = minFollowDistance;
        agent.autoBraking = true;
    }

    void Update()
    {
        CheckWorkshopDetection();

        if (currentState != CompanionState.GoingToWorkshop)
        {
            CheckZombieThreat();
        }

        if (!isReactingToZombie && Input.GetKeyDown(stayToggleKey))
        {
            ToggleFollowMode();
        }

        switch (currentState)
        {
            case CompanionState.Staying:
                UpdateStaying();
                break;

            case CompanionState.Following:
                UpdateFollowing();
                break;

            case CompanionState.GoingToWorkshop:
                UpdateGoingToWorkshop();
                break;
            case CompanionState.Defense:
                UpdateDefense();
                break;
            case CompanionState.Getaway:
                break;
        }
    }

    private void UpdateStaying()
    {
        agent.isStopped = true;

        if (!isManualStay && player != null &&
            Vector3.Distance(transform.position, player.transform.position) <= stopDistance)
        {
            currentState = CompanionState.Following;
        }
    }

    private void UpdateFollowing()
    {
        if (player != null && Vector3.Distance(transform.position, player.transform.position) > stopDistance)
        {
            currentState = CompanionState.Staying;
            return;
        }

        if (!isManualStay && ShouldAutoStay())
        {
            currentState = CompanionState.Staying;
            return;
        }

        if (Time.time - lastCheckTime > checkInterval)
        {
            CheckDistanceToPlayer();
            lastCheckTime = Time.time;
        }
    }

    private void ToggleFollowMode()
    {
        isManualStay = !isManualStay;

        if (isReactingToZombie || health.isDead) return;

        if (isManualStay)
        {
            currentState = CompanionState.Staying;
        }
        else
        {
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                if (distance <= followDistance)
                {
                    currentState = CompanionState.Following;
                }
                else
                {
                    currentState = CompanionState.Staying;
                }
            }
        }
    }

    private void CheckZombieThreat()
    {
        if (Time.time - lastReactionTime < reactionCooldown || isReactingToZombie)
            return;

        Transform nearestZombie = FindNearestZombieInRadius(zombieDetectionRadius);

        if (nearestZombie != null)
        {
            isReactingToZombie = true;
            lastReactionTime = Time.time;
            stateBeforeReaction = currentState;

            bool defend = Random.Range(0f, 1f) <= shootingChance;
            if (defend)
            {
                currentTarget = nearestZombie;
                agent.speed = chaseSpeed;
                currentState = CompanionState.Defense;
            }
            else
            {
                currentState = CompanionState.Getaway;
                StartCoroutine(EscapeFromZombie(nearestZombie));
            }
        }
    }

    private Transform FindNearestZombieInRadius(float radius)
    {
        Transform nearest = null;
        float minSqrDistance = Mathf.Infinity;
        float sqrRadius = radius * radius;

        Collider[] colliders = new Collider[50];
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            colliders,
            zombieLayerMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < count; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            Vector3 toZombie = col.transform.position - transform.position;
            float sqrDistance = toZombie.sqrMagnitude;

            // Проверка видимости
            bool hasClearLineOfSight = true;
            if (obstacleLayers.value != 0)
            {
                hasClearLineOfSight = !Physics.Linecast(
                    transform.position,
                    col.transform.position,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore
                );
            }

            if (sqrDistance <= sqrRadius &&
                sqrDistance < minSqrDistance &&
                hasClearLineOfSight)
            {
                minSqrDistance = sqrDistance;
                nearest = col.transform;
            }
        }
        return nearest;
    }

    private IEnumerator EscapeFromZombie(Transform zombieTarget)
    {
        if (zombieTarget == null)
        {
            isReactingToZombie = false;
            yield break;
        }

        // Расчет точки побега
        Vector3 escapeDirection = zombieTarget != null
            ? (transform.position - zombieTarget.position).normalized
            : -transform.forward;

        Vector3 desiredEscapePoint = transform.position + escapeDirection * stopDistance * 2;

        Vector3 finalEscapePoint = desiredEscapePoint;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredEscapePoint, out hit, 5.0f, NavMesh.AllAreas))
        {
            finalEscapePoint = hit.position;
        }

        agent.SetDestination(finalEscapePoint);
        agent.isStopped = false;

        float escapeTimer = 0f;
        while ((agent.pathPending || agent.remainingDistance > 0.5f) && escapeTimer < 5f)
        {
            escapeTimer += Time.deltaTime;
            yield return null;
        }

        isReactingToZombie = false;
        currentState = stateBeforeReaction;
    }

    private void UpdateGetaway()
    {

    }

    private void UpdateDefense()
    {
        
        if (currentTarget == null || !currentTarget.gameObject.activeSelf)
        {
            EndReaction();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToTarget <= attackDistance)
        {
            agent.isStopped = true;

            // Поворот к цели
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0; // Игнорируем разницу по высоте
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(PerformAttack());
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            
            if (distanceToTarget > zombieDetectionRadius * 1.5f)
            {
                EndReaction();
            }
        }
    }

    private IEnumerator PerformAttack()
    {
        // if (animator != null) animator.SetTrigger("Attack");

        if (currentTarget != null && currentTarget.gameObject.activeSelf)
        {
            EnemyHealth zombieHealth = currentTarget.GetComponent<EnemyHealth>();
            if (zombieHealth != null)
            {
                zombieHealth.ApplyDamage(attackDamage);

                if (zombieHealth.currentHealth <= 0)
                {
                    EndReaction();
                    yield break;
                }
            }
        }

        yield return new WaitForSeconds(attackDuration);

        if (currentTarget == null || !currentTarget.gameObject.activeSelf ||
            Vector3.Distance(transform.position, currentTarget.position) > attackDistance)
        {
            EndReaction();
        }
    }

    private void EndReaction()
    {
        isReactingToZombie = false;
        currentState = stateBeforeReaction;
        agent.speed = movementSpeed;
        if (currentTarget == null || !currentTarget.gameObject.activeSelf)
        {
            currentTarget = null;
        }
    }

    private void FindWorkshop()
    {
        if (targetStayPoint == null)
        {
            GameObject workshopObj = GameObject.FindGameObjectWithTag("Workshop");
            if (workshopObj != null)
            {
                targetStayPoint = workshopObj.transform;
            }
        }
    }

    private void UpdateGoingToWorkshop()
    {
        if (targetStayPoint == null)
        {
            currentState = CompanionState.Following;
            return;
        }

        if (Vector3.Distance(transform.position, targetStayPoint.position) <= stayPointRadius)
        {
            currentState = CompanionState.Staying;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(targetStayPoint.position);
    }


    private void CheckWorkshopDetection()
    {
        if (targetStayPoint == null) return;

        float distance = Vector3.Distance(transform.position, targetStayPoint.position);
        workshopDetected = distance <= detectionRadius;

        if (workshopDetected && currentState != CompanionState.Staying &&
        currentState != CompanionState.GoingToWorkshop &&
        !isReactingToZombie)
        {
            currentState = CompanionState.GoingToWorkshop;
        }
    }

    private bool ShouldAutoStay()
    {
        return workshopDetected && Vector3.Distance(transform.position, targetStayPoint.position) <= stayPointRadius;
    }

    void CheckDistanceToPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float adjustedDistance = distanceToPlayer - minFollowDistance;


        if (distanceToPlayer > minFollowDistance && distanceToPlayer <= followDistance)
        {
            agent.isStopped = false;
            Vector3 direction = (transform.position - player.transform.position).normalized;
            Vector3 targetPosition = player.transform.position + direction * minFollowDistance;

            // Проверяем доступность позиции
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(player.transform.position);
            }
        }
        else if (distanceToPlayer > followDistance && distanceToPlayer <= stopDistance)
        {
            agent.SetDestination(player.transform.position);
        }
        else if (adjustedDistance <= followDistance)
        {
            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                agent.velocity *= 0.85f;
            }
            else
            {
                agent.isStopped = true;
            }
        }
        else
        {
            agent.isStopped = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastCollisionTime < collisionCooldown) return;
        lastCollisionTime = Time.time;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Zombie"))
        {
            HandleCollision(collision);
        }
        else if (collision.gameObject.CompareTag("Workshop"))
        {
            currentState = CompanionState.Staying;
        }
    }

    private void HandleCollision(Collision collision)
    {
        StartCoroutine(StopTemporarily(1f));

        Vector3 direction = (transform.position - collision.transform.position).normalized;
        float safeDistance = minDistance + 0.1f;

        transform.position += direction * safeDistance;
    }

    private IEnumerator StopTemporarily(float duration)
    {
        bool wasStopped = agent.isStopped;
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        if (!wasStopped && currentState != CompanionState.Staying)
            agent.isStopped = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, followDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        if (targetStayPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetStayPoint.position, detectionRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetStayPoint.position, stayPointRadius);
        }

        Gizmos.color = new Color(1, 0.5f, 0);
        Gizmos.DrawWireSphere(transform.position, zombieDetectionRadius);
    }
}