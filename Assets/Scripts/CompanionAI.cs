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
    private Vector3 currentRepairPoint;
    public float transferDistance = 3f;
    public float repairStoppingDistance = 3f;
    public float repairRadius = 5f;

    [Header("Rotation Settings")]
    public float facePlayerSpeed = 5f;
    private const float MODEL_ROTATION_CORRECTION = -90f;
    private bool forceRotationToMovement = false;

    [Header("Psycho Reaction Settings")]
    public float psychoDetectionRadius = 20f;
    public float psychoEscapeDistance = 35f;
    public LayerMask psychoLayerMask;

    private CompanionState currentState = CompanionState.Staying;
    private CompanionState stateBeforeReaction;
    private float lastCheckTime;
    private float lastCollisionTime;
    private float lastReactionTime;
    private float nextAttackTime;
    private float defaultStoppingDistance;
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
        StartCoroutine(ThreatCheckRoutine());
        FindWorkshop();
        player = FindObjectOfType<SC_TPSController>();
        health = GetComponent<CompanionHealth>();
        inventory = GetComponent<CompanionInventory>();
        defaultStoppingDistance = minFollowDistance;
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
        if (targetStayPoint != null && targetStayPoint.CompareTag("Workshop"))
        {
            workshop = targetStayPoint;
            currentWorkshop = workshop.GetComponent<BusRepair>();
            return;
        }

        GameObject workshopObj = GameObject.FindGameObjectWithTag("Workshop");
        if (workshopObj != null)
        {
            targetStayPoint = workshopObj.transform;
            workshop = workshopObj.transform;
            currentWorkshop = workshopObj.GetComponent<BusRepair>();
        }
        else
        {
            BusRepair busRepair = FindObjectOfType<BusRepair>();
            if (busRepair != null)
            {
                workshop = busRepair.transform;
                targetStayPoint = workshop;
                currentWorkshop = busRepair;
            }
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
    private IEnumerator EscapeFromThreat(Transform threatTarget)
    {
        if (threatTarget == null)
        {
            isReactingToZombie = false;
            yield break;
        }

        currentState = CompanionState.Getaway;
        forceRotationToMovement = true;

        // Расчет точки побега
        Vector3 escapeDirection = threatTarget != null
            ? (transform.position - threatTarget.position).normalized
            : -transform.forward;

        Vector3 desiredEscapePoint = transform.position + escapeDirection * psychoEscapeDistance;

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
            if (agent.velocity.sqrMagnitude < 0.1f)
            {
                Vector3 newEscapeDir = (transform.position - threatTarget.position).normalized;
                Vector3 newEscapePoint = transform.position + newEscapeDir * psychoEscapeDistance;

                NavMeshHit newHit;
                if (NavMesh.SamplePosition(newEscapePoint, out newHit, 5.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(newHit.position);
                }
            }

            escapeTimer += Time.deltaTime;
            yield return null;
        }

        forceRotationToMovement = false;
        isReactingToZombie = false;
        currentState = stateBeforeReaction;
        escapeRoutine = null;
    }
    #endregion

#region RanAwayReaction
    private Transform FindNearestPsychoInRadius(float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            radius,
            psychoLayerMask
        );

        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var col in colliders)
        {
            if (!col.gameObject.activeInHierarchy) continue;

            float distance = Vector3.Distance(transform.position, col.transform.position);
            if (distance < minDistance && HasLineOfSight(col.transform))
            {
                minDistance = distance;
                nearest = col.transform;
            }
        }
        return nearest;
    }

    private void ReactToPsycho(Transform psycho)
    {
        if (isReactingToZombie) return;

        isReactingToZombie = true;
        lastReactionTime = Time.time;
        stateBeforeReaction = currentState;

        Debug.Log("Обнаружен психопат! Убегаем!");
        if (escapeRoutine != null) StopCoroutine(escapeRoutine);
        escapeRoutine = StartCoroutine(EscapeFromThreat(psycho));
    }
    #endregion

#region ZombieReaction
    IEnumerator ThreatCheckRoutine()
    {
        while (true)
        {
            if (currentState != CompanionState.GoingToWorkshop)
                CheckThreats();
            yield return new WaitForSeconds(reactionCooldown);
        }
    }

    private void CheckThreats()
    {
        if (Time.time - lastReactionTime < reactionCooldown || isReactingToZombie)
            return;

        if (health.isDead)
        {
            agent.isStopped = true;
            return;
        }

        Transform nearestPsycho = FindNearestPsychoInRadius(psychoDetectionRadius);
        if (nearestPsycho != null && nearestPsycho.gameObject.activeInHierarchy)
        {
            ReactToPsycho(nearestPsycho);
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

        escapeRoutine = StartCoroutine(EscapeFromThreat(zombie));
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

    private void TryReceivePartFromPlayer()
    {
        if (player == null) return;

        if (currentWorkshop != null && currentWorkshop.installedParts >= currentWorkshop.requiredParts)
        {
            Debug.Log("Автобус уже отремонтирован! Деталь не нужна.");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > transferDistance) return;

        Inventory playerInventory = player.GetComponent<Inventory>();
        if (inventory.items[CompanionInventory.BUS_PART] > 0)
        {
            Debug.Log("У компаньона уже есть деталь!");
            return;
        }
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

#region Repair
    private void StartRepairProcess()
    {
        if (currentWorkshop != null && currentWorkshop.installedParts >= currentWorkshop.requiredParts)
        {
            Debug.Log("Автобус уже отремонтирован! Ремонт не требуется.");
            return;
        }

        if (inventory.items[CompanionInventory.BUS_PART] <= 0) return;
        agent.stoppingDistance = repairStoppingDistance;

        currentState = CompanionState.Repair;
        Debug.Log($"Начало ремонта. Предыдущее состояние стояния");
    }

    private void UpdateRepair()
    {

        if (workshop == null)
        {
            FindWorkshop();
            if (workshop == null)
            {
                ExitRepairState();
                return;
            }
        }

        Vector3 repairPosition = currentWorkshop.GetRepairPosition(transform.position);

        float distanceToBus = Vector3.Distance(transform.position, repairPosition);
        bool inPosition = distanceToBus <= repairStoppingDistance;

        if (currentWorkshop.installedParts >= currentWorkshop.requiredParts)
        {
            Debug.Log("Ремонт автобуса завершен!");
            ExitRepairState();
            return;
        }

        if (inventory.items[CompanionInventory.BUS_PART] <= 0)
        {
            Debug.Log("Детали закончились! Прерывание ремонта.");
            ExitRepairState();
            return;
        }

        if (inPosition)
        {
            agent.isStopped = true;
            Debug.Log("В позиции для ремонта...");

            // Устанавливаем деталь
            if (currentWorkshop.TryRepair(inventory))
            {
                Debug.Log("Деталь успешно установлена");
                ExitRepairState();
            }
        }
        else
        {
            Debug.Log($"Движение к точке ремонта (расстояние: {distanceToBus:F1})");
            agent.isStopped = false;
            agent.SetDestination(repairPosition);

            // Простая проверка застревания
            if (agent.velocity.sqrMagnitude < 0.1f && distanceToBus > 1f)
            {
                Debug.Log("Перенаправление к новой точке ремонта");
                repairPosition = currentWorkshop.GetRepairPosition(transform.position);
                agent.SetDestination(repairPosition);
            }
        }
    }

    private void ExitRepairState()
    {
        if (currentWorkshop != null)
        {
            currentWorkshop.ResetRepair();
        }

        agent.stoppingDistance = defaultStoppingDistance;

        RetreatFromBus();
    }
    #endregion

#region Retreating
    private void RetreatFromBus()
    {
        Debug.Log("Отход от автобуса после ремонта");

        Vector3 retreatDirection = (transform.position - workshop.position).normalized;
        Vector3 retreatPoint = transform.position + retreatDirection * stayPointRadius;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(retreatPoint, out hit, stayPointRadius, NavMesh.AllAreas))
        {
            agent.stoppingDistance = 0;
            agent.isStopped = false;
            agent.SetDestination(hit.position);
            currentState = CompanionState.Retreating;
            Debug.Log($"Отход к точке: {hit.position}");
        }
        else
        {
            Debug.Log("Не удалось найти точку отхода, возврат в обычный режим");
            currentState = CompanionState.Staying;
        }
    }

    private void UpdateRetreating()
    {
        if (agent.pathPending) return;
        Debug.Log($"Отход от автобуса... Осталось: {agent.remainingDistance:F1}m");

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                agent.isStopped = true;
                agent.stoppingDistance = defaultStoppingDistance;
                currentState = CompanionState.Staying;
                Debug.Log("Отход завершен. Возврат в состояние стояния");
            }
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

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, psychoDetectionRadius);
    }
}
#endregion