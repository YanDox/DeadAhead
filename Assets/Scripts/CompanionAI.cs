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
        Getaway,
        TakePart,
        Repair,
        Retreating
    }

# region Settings

    [Header("Follow Settings")]
    public float followDistance = 10f;
    public float stopDistance = 20f;
    public float minFollowDistance = 3f;
    public float movementSpeed = 5f;
    public float rotationSpeed = 4f;

    [Header("Stay Point Settings")]
    public float stayPointRadius = 10f;
    public float detectionRadius = 20f;
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
    public float reactionCooldown = 0.5f;
    public LayerMask zombieLayerMask;

    [Header("Melee Attack Settings")]
    public float attackDistance = 2f;
    public float attackRate = 1f;
    public float attackDuration = 2f;
    public float chaseSpeed = 7f;
    public float attackDamage = 1000f;

    [Header("Repair Settings")]
    public KeyCode transferPartKey = KeyCode.R;
    public float transferDistance = 3f;

    [Header("Rotation Settings")]
    public float facePlayerSpeed = 5f;
    private const float MODEL_ROTATION_CORRECTION = -90f;
    private bool forceRotationToMovement = false;

    private CompanionState currentState = CompanionState.Staying;
    private CompanionState stateBeforeReaction;
    private CompanionState stateBeforeRepair;
    private float lastCheckTime;
    private float lastCollisionTime;
    private float lastReactionTime;
    private float nextAttackTime;
    private bool workshopDetected = false;
    private bool isManualStay = false;
    private bool isReactingToZombie = false;
    private bool hasBusPart = false;
    private NavMeshAgent agent;
    private SC_TPSController player;
    private CompanionHealth health;
    private CompanionInventory inventory;
    private BusRepair currentWorkshop;
    private Transform currentTarget;
    private Transform targetPart;
    private Transform workshop;
    private Coroutine escapeRoutine;
    private Coroutine nothingRoutine;
    private Coroutine attackRoutine;
    //private Animator animator;

    #endregion

#region Start

    void Start()
    {
        StartCoroutine(ZombieCheckRoutine());
        FindWorkshop();
        player = FindObjectOfType<SC_TPSController>();
        health = GetComponent<CompanionHealth>();
        inventory = GetComponent<CompanionInventory>();
        nextAttackTime = 0f;
        currentTarget = null;

        agent = GetComponent<NavMeshAgent>();
        //animator = GetComponent<Animator>();

        agent.speed = movementSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = minFollowDistance;
        agent.updateRotation = false;
        agent.autoBraking = true;
    }

#endregion

#region Update

    void Update()
    {
        HandleRotation();
        CheckWorkshopDetection();

        if (health.isDead)
        {
            agent.isStopped = true;
            return;
        }

        if (!isReactingToZombie && Input.GetKeyDown(stayToggleKey))
        {
            ToggleFollowMode();
        }

        if (Input.GetKeyDown(transferPartKey))
        {
            TryReceivePartFromPlayer();
        }

        if (workshopDetected && !isReactingToZombie && currentState != CompanionState.Repair && currentState != CompanionState.Defense && currentState != CompanionState.Getaway &&
            inventory.items[CompanionInventory.BUS_PART] > 0)
        {
            StartRepairProcess();
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
            case CompanionState.TakePart:
                UpdateTakePart();
                break;
            case CompanionState.Repair:
                UpdateRepair();
                break;
            case CompanionState.Retreating:
                UpdateRetreating();
                break;
        }
    }

    #endregion

#region Staying
    private void UpdateStaying()
    {
        agent.isStopped = true;

        if (!isManualStay && player != null && player.transform != null &&
            Vector3.Distance(transform.position, player.transform.position) <= stopDistance)
        {
            currentState = CompanionState.Following;
        }
    }
#endregion

#region Following
    private void UpdateFollowing()
    {
        if (player != null && player.transform != null && Vector3.Distance(transform.position, player.transform.position) > stopDistance)
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
#endregion

#region Workshop
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

    private void FindWorkshop()
    {
        GameObject workshopObj = GameObject.FindGameObjectWithTag("Workshop");

        if (workshopObj == null)
        {
            BusRepair busRepair = FindObjectOfType<BusRepair>();
            if (busRepair != null)
            {
                workshopObj = busRepair.gameObject;
            }
        }

        if (workshopObj != null)
        {
            targetStayPoint = workshopObj.transform;
            workshop = workshopObj.transform; // Важно: сохраняем в workshop!
            currentWorkshop = workshopObj.GetComponent<BusRepair>();
            Debug.Log($"Мастерская найдена: {workshopObj.name}");
        }
        else
        {
            Debug.LogWarning("Мастерская не найдена в сцене!");
        }
    }

    private void CheckWorkshopDetection()
    {
        if (targetStayPoint == null || workshop == null)
        {
            FindWorkshop();
        }

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
#endregion

#region Defense
    private void UpdateDefense()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            EndReaction();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToTarget <= attackDistance)
        {
            agent.isStopped = true;

            if (Time.time >= nextAttackTime)
            {
                if (attackRoutine != null) StopCoroutine(attackRoutine);
                attackRoutine = StartCoroutine(PerformAttack());
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
        try
        {
            // if (animator != null) animator.SetTrigger("Attack");

            if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
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

            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy ||
                Vector3.Distance(transform.position, currentTarget.position) > attackDistance)
            {
                EndReaction();
            }
        }

        finally
        {
            attackRoutine = null;
        }
    }

    #endregion

#region Getaway
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
            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                escapeTimer += Time.deltaTime;
                yield return null;
            }
        }

        forceRotationToMovement = false;
        isReactingToZombie = false;
        currentState = stateBeforeReaction;
        escapeRoutine = null;
    }
#endregion

#region ZombieReaction
    IEnumerator ZombieCheckRoutine()
    {
        while (true)
        {
            if (currentState != CompanionState.GoingToWorkshop)
                CheckZombieThreat();
            yield return new WaitForSeconds(reactionCooldown);
        }
    }

    private void CheckZombieThreat()
    {
        if (Time.time - lastReactionTime < reactionCooldown || isReactingToZombie)
            return;

        if (health.isDead)
        {
            agent.isStopped = true;
            return;
        }

        Transform nearestZombie = FindNearestZombieInRadius(zombieDetectionRadius);

        if (nearestZombie != null)
        {
            isReactingToZombie = true;
            lastReactionTime = Time.time;
            stateBeforeReaction = currentState;

            float reactionChoice = Random.Range(0f, 1f);

            if (reactionChoice <= 0.25f) // 25% - оборона
            {
                Debug.Log("чел пиздится");
                StartDefense(nearestZombie);
            }
            else if (reactionChoice <= 0.75f) // 50% - побег
            {
                Debug.Log("чел бежит");
                StartGetaway(nearestZombie);
            }
            else
            {
                Debug.Log("чел нихуя не делает");
                if (nothingRoutine != null) StopCoroutine(nothingRoutine);
                nothingRoutine = StartCoroutine(StartNothing(nearestZombie));
            }
            // else 25% - остаемся в текущем состоянии (ничего не делаем)
        }
    }

    private Transform FindNearestZombieInRadius(float radius)
    {
        Transform nearest = null;
        float minSqrDist = radius * radius;
        Vector3 position = transform.position;

        Collider[] colliders = Physics.OverlapSphere(position, radius, zombieLayerMask);
        foreach (var col in colliders)
        {
            if (!col.gameObject.activeInHierarchy) continue;

            Vector3 toZombie = col.transform.position - position;
            float sqrDist = toZombie.sqrMagnitude;

            if (sqrDist < minSqrDist && HasLineOfSight(col.transform))
            {
                minSqrDist = sqrDist;
                nearest = col.transform;
            }
        }
        return nearest;
    }

    private bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;

        Vector3 start = transform.position;
        Vector3 end = target.position;

        // Корректировка высоты для реалистичной проверки
        float heightOffset = 0.5f;
        start.y += heightOffset;
        end.y += heightOffset;

        // Проверка прямой видимости
        return !Physics.Linecast(
            start,
            end,
            obstacleLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private void StartDefense(Transform zombie)
    {
        isReactingToZombie = true;
        stateBeforeReaction = currentState;

        currentTarget = zombie;
        agent.speed = chaseSpeed;
        currentState = CompanionState.Defense;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }
    }

    private void StartGetaway(Transform zombie)
    {
        isReactingToZombie = true;
        stateBeforeReaction = currentState;
        currentState = CompanionState.Getaway;

        if (escapeRoutine != null)
        {
            StopCoroutine(escapeRoutine);
        }

        escapeRoutine = StartCoroutine(EscapeFromZombie(zombie));
    }

    private IEnumerator StartNothing(Transform zombie)
    {
        isReactingToZombie = true;
        currentTarget = zombie;

        if (zombie == null)
        {
            isReactingToZombie = false;
            yield break;
        }

        Transform originalZombie = zombie;

        while (originalZombie != null &&
               originalZombie.gameObject.activeInHierarchy &&
               Vector3.Distance(transform.position, originalZombie.position) <= zombieDetectionRadius)
        {
            yield return new WaitForSeconds(0.5f);
        }
        EndReaction();
        nothingRoutine = null;
    }

    private void EndReaction()
    {
        isReactingToZombie = false;
        currentState = stateBeforeReaction;
        agent.speed = movementSpeed;
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
        }
    }
    #endregion

#region TakePart
    private void UpdateTakePart()
    {
        
    }
    #endregion

#region Repair
    private void StartRepairProcess()
    {
        stateBeforeRepair = currentState;
        stateBeforeReaction = currentState;

        currentState = CompanionState.Repair;
        Debug.Log("Компаньон начинает процесс ремонта в мастерской...");
    }

    private void UpdateRepair()
    {
        if (workshop == null)
        {
            FindRepairStation();
            if (workshop == null)
            {
                currentState = stateBeforeReaction;
                return;
            }
        }

        float distanceToBus = Vector3.Distance(transform.position, workshop.position);
        bool inPosition = distanceToBus <= agent.stoppingDistance + 0.5f;

        if (inPosition)
        {
            agent.isStopped = true;
            StartRepairing();
        }
        else
        {
            agent.isStopped = false;

            // Проверяем возможность достижения цели
            if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                Debug.Log("Путь к мастерской заблокирован, выход из ремонта");
                currentState = stateBeforeReaction;
                return;
            }

            agent.SetDestination(workshop.position);
            Debug.Log($"Компаньон будет ремонтировать автобус через ({distanceToBus:F1} метров)");
        }
    }

    private void FindRepairStation()
    {
        if (targetStayPoint != null)
        {
            workshop = targetStayPoint;
            currentWorkshop = targetStayPoint.GetComponent<BusRepair>();
            return;
        }

        FindWorkshop();

        if (targetStayPoint != null)
        {
            workshop = targetStayPoint;
            currentWorkshop = targetStayPoint.GetComponent<BusRepair>();
        }

        if (workshop == null)
        {
            currentState = stateBeforeReaction;
        }
    }

    private void StartRepairing()
    {
        if (inventory.items[CompanionInventory.BUS_PART] > 0 && currentWorkshop != null)
        {
            if (currentWorkshop.InstallPart(inventory))
            {
                Debug.Log("Компаньон установил деталь");

                agent.isStopped = false;

                // 1.Рассчитываем точку отхода вокруг мастерской
                Vector3 randomDirection = Random.insideUnitSphere.normalized;
                if (randomDirection == Vector3.zero)
                {
                    randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                }
                Vector3 retreatPoint = workshop.position + randomDirection * stayPointRadius;

                // 2. Проверяем доступность позиции на NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(retreatPoint, out hit, stayPointRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    agent.isStopped = false;
                    currentState = CompanionState.Retreating;
                    Debug.Log($"Компаньон отходит к точке: {hit.position}");
                }
                else
                {
                    Debug.LogWarning("Не удалось найти точку отхода на NavMesh");
                    currentState = CompanionState.Staying;
                }
            }
            else
            {
                Debug.Log("Сё");
                currentState = stateBeforeReaction;
            }
        }
        else
        {
            Debug.Log("Нема у нас запчастей");
            currentState = stateBeforeReaction;
        }
    }

    private void UpdateRetreating()
    {
        // Если агент еще рассчитывает путь - ждем
        if (agent.pathPending) return;

        // Проверяем, достиг ли компаньон точки отхода
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Если агент уже на месте или не двигается
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f)
            {
                agent.isStopped = true;
                currentState = CompanionState.Staying;
                Debug.Log("Компаньон завершил отход и переходит в режим ожидания");
            }
        }
        // Если путь заблокирован
        else if (agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            Debug.Log("Путь отхода заблокирован, остаемся на месте");
            agent.isStopped = true;
            currentState = CompanionState.Staying;
        }
    }

    private void TryReceivePartFromPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > transferDistance) return;

        Inventory playerInventory = player.GetComponent<Inventory>();
        if (playerInventory != null && playerInventory.items[Inventory.BUS_PART] > 0)
        {
            playerInventory.UseItem(Inventory.BUS_PART);
            inventory.AddItem(CompanionInventory.BUS_PART);
            hasBusPart = true;

            Debug.Log("Игрок передал деталь компаньону!");

            if (workshopDetected)
            {
                StartRepairProcess();
            }
            else
            {
                Debug.Log("Компаньон установит деталь, когда вернется в мастерскую");
            }
        }
        else
        {
            Debug.Log("Деталей нема");
        }
    }
    #endregion

#region Utils
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

        agent.Warp(transform.position + direction * safeDistance);
    }

    private IEnumerator StopTemporarily(float duration)
    {
        bool wasStopped = agent.isStopped;
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        if (!wasStopped && currentState != CompanionState.Staying)
            agent.isStopped = false;
    }

    private void HandleRotation()
    {
        if (currentState == CompanionState.Getaway || currentState == CompanionState.Retreating || forceRotationToMovement)
        {
            RotateToDirection(agent.velocity.normalized);
        }
        else if (currentState == CompanionState.Defense && currentTarget != null)
        {
            RotateToDirection(currentTarget.position - transform.position);
        }
        else if (currentState == CompanionState.Repair && workshop != null)
        {
            RotateToDirection(workshop.position - transform.position);
        }
        else if (player != null)
        {
            RotateToDirection(player.transform.position - transform.position);
        }
    }

    private void RotateToDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        direction.y = 0;
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        targetRotation *= Quaternion.Euler(0, MODEL_ROTATION_CORRECTION, 0);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            facePlayerSpeed * Time.deltaTime
        );
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
#endregion