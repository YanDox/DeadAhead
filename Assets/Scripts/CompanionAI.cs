using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class CompanionAI : MonoBehaviour
{
    public enum CompanionState
    {
        Staying,
        Following,
        GoingToWorkshop
    }

    [Header("Follow Settings")]
    public float followDistance = 5f;
    public float stopDistance = 10f;
    public float minFollowDistance = 1f;
    public float movementSpeed = 3f;
    public float rotationSpeed = 3f;

    [Header("Stay Point Settings")]
    public Transform targetStayPoint;
    public float stayPointRadius = 7f;
    public KeyCode stayToggleKey = KeyCode.E;
    public float detectionRadius = 15f;

    [Header("Detection")]
    public float checkInterval = 0.1f;
    public LayerMask obstacleLayers;

    [Header("Collision Settings")]
    public float collisionCooldown = 0.5f;

    private SC_TPSController player;
    private NavMeshAgent agent;
    private float lastCheckTime;
    private float lastCollisionTime;
    private CompanionState currentState = CompanionState.Staying;
    private Animator animator;
    private bool workshopDetected = false;
    private CompanionHealth health;
    private bool isManualStay = false;

    void Start()
    {
        player = FindObjectOfType<SC_TPSController>();
        health = GetComponent<CompanionHealth>();
        FindWorkshop();

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.speed = movementSpeed;
        agent.angularSpeed = rotationSpeed;
        agent.stoppingDistance = minFollowDistance;
        agent.autoBraking = true;
    }

    void Update()
    {
        FindWorkshop();
        CheckWorkshopDetection();

        if (Input.GetKeyDown(stayToggleKey))
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
        }
    }

    private void UpdateStaying()
    {
        // ќстановка движени€
        agent.isStopped = true;

        // јвтоматическое следование при приближении игрока (только если не в ручном режиме)
        if (!isManualStay && player != null &&
            Vector3.Distance(transform.position, player.transform.position) <= stopDistance)
        {
            currentState = CompanionState.Following;
        }
    }

    private void UpdateFollowing()
    {
        // ѕроверка на превышение stopDistance
        if (player != null && Vector3.Distance(transform.position, player.transform.position) > stopDistance)
        {
            currentState = CompanionState.Staying;
            return;
        }

        // јвтоматический переход в режим ожидани€ у мастерской
        if (!isManualStay && ShouldAutoStay())
        {
            currentState = CompanionState.Staying;
            return;
        }

        // ѕроверка рассто€ни€ к игроку с интервалом
        if (Time.time - lastCheckTime > checkInterval)
        {
            CheckDistanceToPlayer();
            lastCheckTime = Time.time;
        }
    }

    private void UpdateGoingToWorkshop()
    {
        if (targetStayPoint == null)
        {
            currentState = CompanionState.Following;
            return;
        }

        // ѕроверка достигли ли мастерской
        if (Vector3.Distance(transform.position, targetStayPoint.position) <= stayPointRadius)
        {
            currentState = CompanionState.Staying;
            return;
        }

        // ѕродолжаем идти к мастерской
        agent.isStopped = false;
        agent.SetDestination(targetStayPoint.position);
    }

    private void ToggleFollowMode()
    {
        isManualStay = !isManualStay;

        if (isManualStay)
        {
            // јктивируем ручное ожидание
            currentState = CompanionState.Staying;
        }
        else
        {
            // ѕровер€ем рассто€ние до игрока
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                // ≈сли игрок р€дом - сразу начинаем следовать
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

    private void CheckWorkshopDetection()
    {
        if (targetStayPoint == null) return;

        float distance = Vector3.Distance(transform.position, targetStayPoint.position);
        workshopDetected = distance <= detectionRadius;

        // ѕереход к мастерской при обнаружении
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

        // —охран€ем оригинальную логику активации движени€
        if (distanceToPlayer > minFollowDistance && distanceToPlayer <= followDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.transform.position);
        }
        // ƒобавл€ем условие дл€ продолжени€ движени€
        else if (distanceToPlayer > minFollowDistance && distanceToPlayer <= stopDistance && !agent.isStopped)
        {
            // ѕродолжаем движение если уже двигались
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
    }
}