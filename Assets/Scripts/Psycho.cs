using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Psycho : MonoBehaviour
{
	public enum PsychoState
	{
		Patrolling,
		Chasing,
		Attacking,
		Returning
	}

	[Header("Settings")]
	public float detectionRadius = 12f;
	public float chaseRadius = 20f;
	public float patrolRadius = 25f;
	public float patrolPointMinDistance = 5f;
	public float patrolWaitTime = 2f;
	public float attackRange = 2f;
	public int attackDamage = 20;
	public float attackCooldown = 1.5f;
	public LayerMask enemyLayers; // Слои для поиска врагов (игрок + зомби)

	private NavMeshAgent navMeshAgent;
	private Transform currentTarget;
	private PlayerHealth playerHealth;
	private List<Zombie> zombiesInRange = new List<Zombie>();
	private PsychoState currentState = PsychoState.Patrolling;
	private Vector3 spawnPosition;
	private Vector3 currentPatrolPoint;
	private float waitTimer = 0f;
	private bool isWaiting = false;
	private float lastAttackTime;
	private Animator animator;
	private Collider[] hitColliders = new Collider[10];

	void Start()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
		spawnPosition = transform.position;
		SetRandomPatrolPoint();
	}

	void Update()
	{
		FindEnemiesInRange();
		ChooseTarget();

		if (currentTarget == null)
		{
			if (currentState != PsychoState.Patrolling && currentState != PsychoState.Returning)
			{
				currentState = PsychoState.Returning;
				navMeshAgent.SetDestination(spawnPosition);
			}
		}

		if (currentTarget == null) return;

		float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

		switch (currentState)
		{
			case PsychoState.Patrolling:
				UpdatePatrolling(distanceToTarget);
				break;

			case PsychoState.Chasing:
				UpdateChasing(distanceToTarget);
				break;

			case PsychoState.Returning:
				UpdateReturning(distanceToTarget);
				break;

			case PsychoState.Attacking:
				UpdateAttacking(distanceToTarget);
				break;
		}

		UpdateAnimation();
	}

	private void FindEnemiesInRange()
	{
		zombiesInRange.Clear();
		int numColliders = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hitColliders, enemyLayers);

		for (int i = 0; i < numColliders; i++)
		{
			var collider = hitColliders[i];
			if (collider.CompareTag("Player"))
			{
				playerHealth = collider.GetComponent<PlayerHealth>();
			}
			else if (collider.CompareTag("Zombie"))
			{
				var zombie = collider.GetComponent<Zombie>();
				if (zombie != null) zombiesInRange.Add(zombie);
			}
		}
	}

	private void ChooseTarget()
	{
		// Приоритет - игрок
		if (playerHealth != null && playerHealth.currentHealth > 0)
		{
			currentTarget = playerHealth.transform;
			return;
		}

		// Затем ближайший зомби
		if (zombiesInRange.Count > 0)
		{
			float minDistance = float.MaxValue;
			Zombie closestZombie = null;

			foreach (var zombie in zombiesInRange)
			{
				if (zombie == null) continue;
				float dist = Vector3.Distance(transform.position, zombie.transform.position);
				if (dist < minDistance)
				{
					minDistance = dist;
					closestZombie = zombie;
				}
			}

			if (closestZombie != null)
			{
				currentTarget = closestZombie.transform;
				return;
			}
		}

		currentTarget = null;
	}

	private void UpdatePatrolling(float distanceToTarget)
	{
		if (currentTarget != null && distanceToTarget <= detectionRadius)
		{
			currentState = PsychoState.Chasing;
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

	private void UpdateChasing(float distanceToTarget)
	{
		if (currentTarget == null || distanceToTarget > chaseRadius)
		{
			currentState = PsychoState.Returning;
			navMeshAgent.SetDestination(spawnPosition);
			return;
		}

		navMeshAgent.SetDestination(currentTarget.position);

		if (distanceToTarget <= attackRange)
		{
			currentState = PsychoState.Attacking;
			AttackTarget();
		}
	}

	private void UpdateAttacking(float distanceToTarget)
	{
		if (distanceToTarget > attackRange * 1.2f)
		{
			currentState = PsychoState.Chasing;
			return;
		}

		if (Time.time - lastAttackTime >= attackCooldown)
		{
			AttackTarget();
		}
	}

	private void UpdateReturning(float distanceToTarget)
	{
		if (currentTarget != null && distanceToTarget <= detectionRadius)
		{
			currentState = PsychoState.Chasing;
			return;
		}

		if (navMeshAgent.destination != spawnPosition)
		{
			navMeshAgent.SetDestination(spawnPosition);
		}

		if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
		{
			currentState = PsychoState.Patrolling;
			SetRandomPatrolPoint();
		}
	}

	private void AttackTarget()
	{
		if (Time.time - lastAttackTime >= attackCooldown)
		{
			lastAttackTime = Time.time;
			transform.LookAt(new Vector3(currentTarget.position.x, transform.position.y, currentTarget.position.z));

			if (animator != null)
			{
				animator.SetTrigger("Attack");
			}

			// Атака игрока
			if (currentTarget.CompareTag("Player") && playerHealth != null)
			{
				playerHealth.TakeDamage(attackDamage);
			}
			// Атака зомби
			else if (currentTarget.CompareTag("Zombie"))
			{
				var zombie = currentTarget.GetComponent<EnemyHealth>();
				if (zombie != null) zombie.TakeDamage(attackDamage);
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
			animator.SetBool("IsChasing", currentState == PsychoState.Chasing);
		}
	}

	public void TakeDamage(int damage)
	{
		// Реализацию получения урона можно добавить по аналогии с зомби
		// Например, если у вас есть компонент здоровья у психа
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRadius);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, chaseRadius);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(spawnPosition, patrolRadius);
	}
}