using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ZombieBoss : MonoBehaviour
{
	public enum BossState
	{
		Patrolling,
		Chasing,
		Returning,
		PreparingThrow,
		Throwing
	}

	[Header("Basic Settings")]
	public float detectionRadius = 15f;
	public float chaseRadius = 25f;
	public float patrolRadius = 30f;
	public float patrolPointMinDistance = 8f;
	public float patrolWaitTime = 3f;
	public float attackRange = 2f;
	public int meleeDamage = 25;
	public float meleeCooldown = 1.5f;

	[Header("Throw Settings")]
	public float throwRange = 10f;
	public float throwCooldown = 10f;
	public float throwForce = 15f;
	public float throwHeight = 2f;
	public float throwPreparationTime = 1.5f;
	public LayerMask zombieLayer;

	[Header("References")]
	public GameObject throwIndicatorPrefab;
	public AudioClip throwSound;
	public AudioClip roarSound;

	private NavMeshAgent navMeshAgent;
	private Transform playerTransform;
	private PlayerHealth playerHealth;
	private Animator animator;
	private AudioSource audioSource;

	private BossState currentState = BossState.Patrolling;
	private Vector3 spawnPosition;
	private Vector3 currentPatrolPoint;
	private float waitTimer = 0f;
	private bool isWaiting = false;
	private float lastMeleeAttackTime;
	private float lastThrowTime;
	private GameObject currentThrowIndicator;
	private Zombie selectedZombieToThrow;
	private float throwPreparationTimer;

	private List<Zombie> nearbyZombies = new List<Zombie>();

	void Start()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
		audioSource = GetComponent<AudioSource>();
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
			case BossState.Patrolling:
				UpdatePatrolling(distanceToPlayer);
				break;

			case BossState.Chasing:
				UpdateChasing(distanceToPlayer);
				break;

			case BossState.Returning:
				UpdateReturning(distanceToPlayer);
				break;

			case BossState.PreparingThrow:
				UpdateThrowPreparation(distanceToPlayer);
				break;

			case BossState.Throwing:
				UpdateThrowing();
				break;
		}

		UpdateAnimation();
		UpdateNearbyZombiesList();
	}

	private void UpdatePatrolling(float distanceToPlayer)
	{
		if (distanceToPlayer <= detectionRadius)
		{
			currentState = BossState.Chasing;
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
			currentState = BossState.Returning;
			navMeshAgent.SetDestination(spawnPosition);
			return;
		}

		navMeshAgent.SetDestination(playerTransform.position);

		// Check for throw ability
		if (Time.time - lastThrowTime >= throwCooldown && nearbyZombies.Count > 0 && distanceToPlayer > attackRange * 1.5f)
		{
			StartThrowPreparation();
			return;
		}

		// Melee attack
		if (distanceToPlayer <= attackRange)
		{
			MeleeAttack();
		}
	}

	private void UpdateReturning(float distanceToPlayer)
	{
		if (distanceToPlayer <= detectionRadius)
		{
			currentState = BossState.Chasing;
			return;
		}

		if (navMeshAgent.destination != spawnPosition)
		{
			navMeshAgent.SetDestination(spawnPosition);
		}

		if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
		{
			currentState = BossState.Patrolling;
			SetRandomPatrolPoint();
		}
	}

	private void StartThrowPreparation()
	{
		currentState = BossState.PreparingThrow;
		navMeshAgent.isStopped = true;
		throwPreparationTimer = 0f;

		// Select closest zombie to throw
		selectedZombieToThrow = GetClosestZombie();

		if (selectedZombieToThrow != null)
		{
			// Create throw indicator
			if (throwIndicatorPrefab != null)
			{
				currentThrowIndicator = Instantiate(throwIndicatorPrefab, playerTransform.position, Quaternion.identity);
			}

			// Play preparation sound
			if (roarSound != null && audioSource != null)
			{
				audioSource.PlayOneShot(roarSound);
			}

			// Trigger animation
			if (animator != null)
			{
				animator.SetTrigger("PrepareThrow");
			}
		}
		else
		{
			// No zombie to throw, return to chasing
			currentState = BossState.Chasing;
			navMeshAgent.isStopped = false;
		}
	}

	private void UpdateThrowPreparation(float distanceToPlayer)
	{
		throwPreparationTimer += Time.deltaTime;

		// Update throw indicator position
		if (currentThrowIndicator != null)
		{
			currentThrowIndicator.transform.position = playerTransform.position;
		}

		// Face the player
		Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
		directionToPlayer.y = 0;
		if (directionToPlayer != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(directionToPlayer);
		}

		if (throwPreparationTimer >= throwPreparationTime)
		{
			currentState = BossState.Throwing;
			ThrowZombie();
		}
		else if (distanceToPlayer <= attackRange)
		{
			// Player got too close, cancel throw
			CancelThrow();
			MeleeAttack();
		}
	}

	private void UpdateThrowing()
	{
		// Wait for throw animation to complete (handled by animation events)
		// This state is mostly for animation synchronization
	}

	private void ThrowZombie()
	{
		if (selectedZombieToThrow == null)
		{
			currentState = BossState.Chasing;
			navMeshAgent.isStopped = false;
			return;
		}

		// Play throw sound
		if (throwSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(throwSound);
		}

		// Trigger throw animation
		if (animator != null)
		{
			animator.SetTrigger("Throw");
		}

		// Calculate throw direction with arc
		Vector3 throwDirection = (playerTransform.position - selectedZombieToThrow.transform.position).normalized;
		Vector3 throwVelocity = throwDirection * throwForce;
		throwVelocity.y = throwHeight;

		// Disable zombie AI and enable physics
		Rigidbody zombieRb = selectedZombieToThrow.GetComponent<Rigidbody>();
		if (zombieRb == null)
		{
			zombieRb = selectedZombieToThrow.gameObject.AddComponent<Rigidbody>();
		}

		// Make zombie a projectile
		selectedZombieToThrow.enabled = false;
		zombieRb.isKinematic = false;
		zombieRb.velocity = throwVelocity;

		// Add damage component or tag zombie as thrown
		ZombieProjectile zombieProjectile = selectedZombieToThrow.gameObject.AddComponent<ZombieProjectile>();
		zombieProjectile.damage = meleeDamage * 2; // Thrown zombie does more damage

		// Clean up
		Destroy(currentThrowIndicator);
		lastThrowTime = Time.time;
		selectedZombieToThrow = null;

		// Return to chasing
		currentState = BossState.Chasing;
		navMeshAgent.isStopped = false;
	}

	private void CancelThrow()
	{
		Destroy(currentThrowIndicator);
		selectedZombieToThrow = null;
		navMeshAgent.isStopped = false;
		currentState = BossState.Chasing;
	}

	private Zombie GetClosestZombie()
	{
		Zombie closestZombie = null;
		float closestDistance = float.MaxValue;

		foreach (Zombie zombie in nearbyZombies)
		{
			if (zombie == null) continue;

			float distance = Vector3.Distance(transform.position, zombie.transform.position);
			if (distance < closestDistance && distance <= throwRange)
			{
				closestDistance = distance;
				closestZombie = zombie;
			}
		}

		return closestZombie;
	}

	private void UpdateNearbyZombiesList()
	{
		nearbyZombies.Clear();
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, throwRange, zombieLayer);

		foreach (Collider col in hitColliders)
		{
			Zombie zombie = col.GetComponent<Zombie>();
			if (zombie != null && zombie != this)
			{
				nearbyZombies.Add(zombie);
			}
		}
	}

	private void MeleeAttack()
	{
		if (Time.time - lastMeleeAttackTime >= meleeCooldown)
		{
			lastMeleeAttackTime = Time.time;

			transform.LookAt(playerTransform);

			if (animator != null)
			{
				animator.SetTrigger("Attack");
			}

			if (playerHealth != null)
			{
				playerHealth.TakeDamage(meleeDamage);
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
			bool isMoving = navMeshAgent.velocity.magnitude > 0.1f && currentState != BossState.PreparingThrow && currentState != BossState.Throwing;
			animator.SetBool("IsMoving", isMoving);
			animator.SetBool("IsChasing", currentState == BossState.Chasing);
		}
	}

	// Called by animation event when throw is complete
	public void OnThrowComplete()
	{
		// Can add additional logic here if needed
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRadius);

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, chaseRadius);

		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(transform.position, throwRange);

		if (Application.isPlaying)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(spawnPosition, patrolRadius);
		}
	}
}

// Helper component for thrown zombies
public class ZombieProjectile : MonoBehaviour
{
	public int damage = 30;
	public float impactForce = 10f;

	private void OnCollisionEnter(Collision collision)
	{
		// Damage player if hit
		PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
		if (playerHealth != null)
		{
			playerHealth.TakeDamage(damage);

			// Apply impact force
			Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
			if (playerRb != null)
			{
				Vector3 forceDirection = (collision.transform.position - transform.position).normalized;
				playerRb.AddForce(forceDirection * impactForce, ForceMode.Impulse);
			}
		}

		// Destroy this component and re-enable zombie AI if it's still alive
		Zombie zombie = GetComponent<Zombie>();
		if (zombie != null)
		{
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb != null) Destroy(rb);

			zombie.enabled = true;
		}

		Destroy(this);
	}
}