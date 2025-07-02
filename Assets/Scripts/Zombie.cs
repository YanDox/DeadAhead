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

	[Header("Settings")]
	public float detectionRadius = 10f;
	public float chaseRadius = 15f;
	public float patrolRadius = 20f;
	public float patrolPointMinDistance = 5f;
	public float patrolWaitTime = 2f;
	public float attackRange = 1.5f;
	public int attackDamage = 15;
	public float attackCooldown = 1f;

	private NavMeshAgent navMeshAgent;
	private Transform playerTransform;
	private PlayerHealth playerHealth;
	private CompanionHealth companionHealth;
	private ZombieState currentState = ZombieState.Patrolling;
	private Vector3 spawnPosition;
	private Vector3 currentPatrolPoint;
	private float waitTimer = 0f;
	private bool isWaiting = false;
	private float lastAttackTime;
	private Animator animator;

	void Start()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
		spawnPosition = transform.position;

		// Инициализация цели
		InitializeTargets();
		SetRandomPatrolPoint();
	}

	void InitializeTargets()
	{
		// Поиск игрока
		var playerController = FindObjectOfType<SC_TPSController>();
		if (playerController != null)
		{
			playerTransform = playerController.transform;
			playerHealth = playerController.GetComponent<PlayerHealth>();
		}

		// Поиск компаньона (опционально)
		var companion = FindObjectOfType<CompanionAI>();
		if (companion != null)
		{
			companionHealth = companion.GetComponent<CompanionHealth>();
		}
	}

	void Update()
	{
		if (playerTransform == null)
		{
			InitializeTargets();
			if (playerTransform == null) return;
		}

		float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

		switch (currentState)
		{
			case ZombieState.Patrolling:
				UpdatePatrolling(distanceToPlayer);
				break;

			case ZombieState.Chasing:
				UpdateChasing(distanceToPlayer);
				break;

			case ZombieState.Returning:
				UpdateReturning(distanceToPlayer);
				break;
		}

		UpdateAnimation();
	}

	private void UpdatePatrolling(float distanceToPlayer)
	{
		if (distanceToPlayer <= detectionRadius)
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

	private void UpdateChasing(float distanceToPlayer)
	{
		if (distanceToPlayer > chaseRadius)
		{
			currentState = ZombieState.Returning;
			navMeshAgent.SetDestination(spawnPosition);
			return;
		}

		navMeshAgent.SetDestination(playerTransform.position);

		if (distanceToPlayer <= attackRange)
		{
			AttackTarget();
		}
	}

	private void UpdateReturning(float distanceToPlayer)
	{
		if (distanceToPlayer <= detectionRadius)
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

	private void AttackTarget()
	{
		if (Time.time - lastAttackTime >= attackCooldown)
		{
			lastAttackTime = Time.time;

			// Поворот к цели
			transform.LookAt(playerTransform);

			// Анимация атаки
			if (animator != null)
			{
				animator.SetTrigger("Attack");
			}

			// Атака игрока
			if (playerHealth != null)
			{
				playerHealth.TakeDamage(attackDamage);
			}

			// Атака компаньона (если в радиусе)
			if (companionHealth != null &&
				Vector3.Distance(transform.position, companionHealth.transform.position) <= attackRange)
			{
				companionHealth.TakeDamage(attackDamage);
			}
		}
	}

	private void SetRandomPatrolPoint()
	{
		Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
		randomDirection += spawnPosition;

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
		SetRandomPatrolPoint();
	}

	private void UpdateAnimation()
	{
		if (animator != null)
		{
			bool isMoving = navMeshAgent.velocity.magnitude > 0.1f;
			animator.SetBool("IsMoving", isMoving);
			animator.SetBool("IsChasing", currentState == ZombieState.Chasing);
		}
	}
	public void ForceChasePlayer(Transform playerTarget)
{
    if (playerTarget != null)
    {
        playerTransform = playerTarget;
        currentState = ZombieState.Chasing;
        // Сразу обновляем цель для NavMeshAgent
        navMeshAgent.SetDestination(playerTransform.position);
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