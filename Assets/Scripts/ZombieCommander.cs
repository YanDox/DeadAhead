using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieCommander : MonoBehaviour, IEntity
{
	[Header("Commander Settings")]
	public float callReinforcementsRadius = 20f;
	public float callReinforcementsCooldown = 10f;
	public LayerMask enemyLayer;
	public float reinforcementCallHealthThreshold = 0.5f;
	public GameObject callEffectPrefab;
	public AudioClip callSound;

	[Header("Combat Settings")]
	public float detectionRadius = 10f;
	public float chaseRadius = 15f;
	public float patrolRadius = 20f;
	public float patrolPointMinDistance = 5f;
	public float patrolWaitTime = 2f;
	public float attackRange = 1.5f;
	public int attackDamage = 15;
	public float attackCooldown = 1f;

	public enum ZombieState
	{
		Patrolling,
		Chasing,
		Returning
	}

	private EnemyHealth health;
	private UnityEngine.AI.NavMeshAgent navMeshAgent;
	private Animator animator;
	private Transform playerTransform;
	private PlayerHealth playerHealth;
	private CompanionHealth companionHealth;
	private ZombieState currentState = ZombieState.Patrolling;
	private Vector3 spawnPosition;
	private Vector3 currentPatrolPoint;
	private float waitTimer = 0f;
	private bool isWaiting = false;
	private float lastAttackTime;
	private float lastCallTime;
	private AudioSource audioSource;

	void Start()
	{
		navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
		animator = GetComponent<Animator>();
		audioSource = GetComponent<AudioSource>();
		spawnPosition = transform.position;
		health = GetComponent<EnemyHealth>();

		if (health == null)
		{
			Debug.LogError("EnemyHealth component is missing!", this);
			health = gameObject.AddComponent<EnemyHealth>();
		}

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
		}

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

		if (currentState == ZombieState.Chasing &&
			Time.time - lastCallTime >= callReinforcementsCooldown &&
			(health != null && health.currentHealth / health.maxHealth <= reinforcementCallHealthThreshold))
		{
			CallReinforcements();
			lastCallTime = Time.time;
		}

		UpdateAnimation();
	}

	public void ApplyDamage(float damage)
	{
		if (health != null)
		{
			health.TakeDamage(Mathf.RoundToInt(damage));

			if (currentState != ZombieState.Chasing && playerTransform != null)
			{
				currentState = ZombieState.Chasing;
				navMeshAgent.SetDestination(playerTransform.position);
			}
		}
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
			transform.LookAt(playerTransform);

			if (animator != null)
			{
				animator.SetTrigger("Attack");
			}

			if (playerHealth != null)
			{
				playerHealth.TakeDamage(attackDamage);
			}

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

		UnityEngine.AI.NavMeshHit hit;
		if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, UnityEngine.AI.NavMesh.AllAreas))
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
			navMeshAgent.SetDestination(playerTransform.position);
		}
	}

	private void CallReinforcements()
	{
		Collider[] zombies = Physics.OverlapSphere(
			transform.position,
			callReinforcementsRadius,
			enemyLayer
		);

		foreach (Collider zombieCollider in zombies)
		{
			Zombie zombie = zombieCollider.GetComponent<Zombie>();
			if (zombie != null && zombie != this)
			{
				zombie.ForceChasePlayer(playerTransform);
			}
		}

		if (callEffectPrefab != null)
		{
			Instantiate(callEffectPrefab, transform.position, Quaternion.identity);
		}

		if (callSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(callSound);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRadius);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, chaseRadius);

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, callReinforcementsRadius);

		if (Application.isPlaying)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(spawnPosition, patrolRadius);
		}
	}
}