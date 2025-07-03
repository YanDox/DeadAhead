using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

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
    public float followDistance = 5f;
    public float stopDistance = 10f;
    public float minFollowDistance = 1f;
    public float movementSpeed = 3f;
    public float rotationSpeed = 3f;

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

    [Header("Zombie Reaction Settings")]
    public float zombieDetectionRadius = 15f;
    public float shootingChance = 0f;
    public float reactionCooldown = 1f;
    public GameObject projectilePrefab;
    public Transform shootPoint;

    private CompanionState currentState = CompanionState.Staying;
    private CompanionState stateBeforeReaction;
    private float lastCheckTime;
    private float lastCollisionTime;
    private float lastReactionTime;
    private bool workshopDetected = false;
    private bool isManualStay = false;
    private bool isReactingToZombie = false;
    private SC_TPSController player;
    private NavMeshAgent agent;
    //private Animator animator;
    private CompanionHealth health;

    void Start()
    {
        FindWorkshop();
        player = FindObjectOfType<SC_TPSController>();
        health = GetComponent<CompanionHealth>();

        agent = GetComponent<NavMeshAgent>();
        //animator = GetComponent<Animator>();

        agent.speed = movementSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = minFollowDistance;
        agent.autoBraking = true;
    }

    void Update()
    {
        FindWorkshop();
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
                UpdateGetaway();
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

        if (isReactingToZombie) return;

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
        if (Time.time - lastReactionTime < reactionCooldown ||
            isReactingToZombie)
            return;

        Transform nearestZombie = FindNearestZombieInRadius(zombieDetectionRadius);

        if (nearestZombie != null)
        {
            isReactingToZombie = true;
            lastReactionTime = Time.time;

            stateBeforeReaction = currentState;

            bool defend = Random.Range(0f, 1f) <= shootingChance;
            currentState = defend ? CompanionState.Defense : CompanionState.Getaway;

            StartCoroutine(defend ? DefendAgainstZombie(nearestZombie) : EscapeFromZombie(nearestZombie));
        }
    }

    private Transform FindNearestZombieInRadius(float radius)
    {
        Transform nearest = null;
        float minSqrDistance = Mathf.Infinity;
        float sqrRadius = radius * radius; // Используем квадрат радиуса для сравнения

        // Получаем всех зомби по тегу
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");

        foreach (GameObject zombie in zombies)
        {
            if (zombie == null) continue;

            // Вычисляем квадрат расстояния (оптимизация)
            Vector3 toZombie = zombie.transform.position - transform.position;
            float sqrDistance = toZombie.sqrMagnitude;

            // Проверяем в радиусе с помощью квадрата расстояния
            if (sqrDistance <= sqrRadius && sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearest = zombie.transform;
            }
        }
        return nearest;
    }

    private IEnumerator DefendAgainstZombie(Transform zombieTarget)
    {
        Debug.Log("Companion: Defending position!");
        //if (animator != null) animator.SetTrigger("Shoot");

        if (zombieTarget != null)
        {
            // Поворот к цели
            Vector3 direction = (zombieTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = lookRotation;

            // Создание снаряда
            if (projectilePrefab && shootPoint)
            {
                Instantiate(projectilePrefab, shootPoint.position, lookRotation);
            }
        }

        yield return new WaitForSeconds(1f);

        if (this == null) yield break;

        isReactingToZombie = false;
        currentState = stateBeforeReaction;
    }

    private IEnumerator EscapeFromZombie(Transform zombieTarget)
    {
        Debug.Log("Companion: Running away!");

        // Расчет точки побега
        Vector3 escapeDirection = zombieTarget != null
            ? (transform.position - zombieTarget.position).normalized
            : -transform.forward;

        // Рассчитываем желаемую точку побега
        Vector3 desiredEscapePoint = transform.position + escapeDirection * stopDistance * 2;

        // Пытаемся найти ближайшую точку на NavMesh
        Vector3 finalEscapePoint = desiredEscapePoint; // По умолчанию используем желаемую точку
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredEscapePoint, out hit, 5.0f, NavMesh.AllAreas))
        {
            finalEscapePoint = hit.position;
        }
        else
        {
            Debug.LogWarning("Escape point not found on NavMesh! Using fallback position");
        }

        // Устанавливаем точку назначения
        agent.SetDestination(finalEscapePoint);
        agent.isStopped = false;

        // Ждем достижения точки или таймаут
        float escapeTimer = 0f;
        while ((agent.pathPending || agent.remainingDistance > 0.5f) && escapeTimer < 5f)
        {
            escapeTimer += Time.deltaTime;
            yield return null;
        }

        isReactingToZombie = false;
        currentState = stateBeforeReaction;
        Debug.Log("Escape completed!");
    }

    private void UpdateDefense()
    {
        agent.isStopped = true;

        //Добавляем поворот к цели
        Transform nearestZombie = FindNearestZombieInRadius(zombieDetectionRadius);
        if (nearestZombie != null)
        {
            Vector3 direction = (nearestZombie.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void UpdateGetaway()
    {

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

        if (workshopDetected && currentState != CompanionState.Staying)
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

        if (distanceToPlayer > minFollowDistance && distanceToPlayer <= followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
        }
        else if (distanceToPlayer > minFollowDistance && distanceToPlayer <= stopDistance && !agent.isStopped)
        {
            // Продолжаем движение если уже двигались
            agent.SetDestination(player.transform.position);
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

        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerCollision(collision);
        }
        else if (collision.gameObject.CompareTag("Workshop"))
        {
            currentState = CompanionState.Staying;
        }
    }

    private void HandlePlayerCollision(Collision collision)
    {
        StartCoroutine(StopTemporarily(0.3f));
        Vector3 direction = (transform.position - collision.transform.position).normalized;
        transform.position += direction * 0.5f;
    }

    private IEnumerator StopTemporarily(float duration)
    {
        bool wasStopped = agent.isStopped;
        agent.isStopped = true;

        yield return new WaitForSeconds(duration);

        if (!wasStopped && currentState != CompanionState.Staying)
            agent.isStopped = false;
    }

    bool IsObstacleBetween()
    {
        if (obstacleLayers.value == 0 || player == null) return false;

        Vector3 direction = player.transform.position - transform.position;
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, obstacleLayers))
        {
            return hit.transform != player.transform;
        }
        return false;
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