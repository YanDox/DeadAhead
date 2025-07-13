using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
public class ZombieFat : MonoBehaviour, IEntity
{
	public enum ZombieState
	{
		Patrolling,
		Chasing,
		Charging,
		Attacking,
		Stunned
	}

	[Header("Health Settings")]
	public int maxHealth = 300;
	public int currentHealth;
	public float deathAnimationTime = 2f;
	public GameObject deathEffect;
	public int Cost = 30;
	public event Action OnDeath;

	[Header("Damage Feedback")]
	public GameObject damageEffectPrefab;
	public float effectDuration = 0.3f;

	[Header("Fat Zombie Settings")]
	public float detectionRadius = 15f;
	public float chaseRadius = 25f;
	public float chargeDistance = 10f;
	public float chargeSpeedMultiplier = 2f;
	public float chargeCooldown = 8f;
	public float attackRange = 2.5f;
	public int attackDamage = 30;
	public int chargeDamage = 50;
	public float attackCooldown = 2f;
	public float stunDuration = 3f;
	public float explosionRadius = 5f;
	public int explosionDamage = 60;
	public GameObject explosionEffect;
	public float bellyImpactForce = 15f;
	public float patrolRadius = 20f;
	public float patrolPointMinDistance = 5f;

	private NavMeshAgent navMeshAgent;
	private Transform playerTransform;
	private PlayerHealth playerHealth;
	private ZombieState currentState = ZombieState.Patrolling;
	private float lastAttackTime;
	private float lastChargeTime;
	private float stunTimer;
	private Animator animator;
	private Inventory playerInv;
	private bool isExploded = false;
	private Vector3 spawnPosition;
	private Vector3 currentPatrolPoint;
	private float originalSpeed;
	private float originalAcceleration;

	void Start()
	{
		navMeshAgent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
		currentHealth = maxHealth;
		playerInv = FindObjectOfType<Inventory>();
		spawnPosition = transform.position;

		// —охран€ем оригинальные значени€ скорости
		originalSpeed = navMeshAgent.speed;
		originalAcceleration = navMeshAgent.acceleration;

		var playerController = FindObjectOfType<SC_TPSController>();
		if (playerController != null)
		{
			playerTransform = playerController.transform;
			playerHealth = playerController.GetComponent<PlayerHealth>();
		}

		SetRandomPatrolPoint();
	}

	void Update()
	{
		if (playerTransform == null) return;

		float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

		switch (currentState)
		{
			case ZombieState.Patrolling:
				UpdatePatrolling(distanceToPlayer);
				break;

			case ZombieState.Chasing:
				UpdateChasing(distanceToPlayer);
				break;

			case ZombieState.Charging:
				UpdateCharging(distanceToPlayer);
				break;

			case ZombieState.Attacking:
				UpdateAttacking(distanceToPlayer);
				break;

			case ZombieState.Stunned:
				UpdateStunned();
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
			SetRandomPatrolPoint();
		}
	}

	private void UpdateChasing(float distanceToPlayer)
	{
		if (distanceToPlayer > chaseRadius)
		{
			currentState = ZombieState.Patrolling;
			SetRandomPatrolPoint();
			return;
		}

		navMeshAgent.SetDestination(playerTransform.position);

		if (distanceToPlayer <= chargeDistance && Time.time - lastChargeTime > chargeCooldown)
		{
			StartCharge();
			return;
		}

		if (distanceToPlayer <= attackRange)
		{
			currentState = ZombieState.Attacking;
			Attack();
		}
	}

	private void StartCharge()
	{
		currentState = ZombieState.Charging;
		lastChargeTime = Time.time;
		navMeshAgent.speed = originalSpeed * chargeSpeedMultiplier;
		navMeshAgent.acceleration = originalAcceleration * 2f;
		animator.SetTrigger("Charge");
	}

	private void UpdateCharging(float distanceToPlayer)
	{
		navMeshAgent.SetDestination(playerTransform.position);

		if (distanceToPlayer <= attackRange * 1.5f)
		{
			ChargeImpact();
		}
		else if (navMeshAgent.velocity.magnitude < 0.1f)
		{
			ChargeImpact();
		}
	}

	private void ChargeImpact()
	{
		if (playerHealth != null && Vector3.Distance(transform.position, playerTransform.position) <= attackRange * 1.5f)
		{
			playerHealth.TakeDamage(chargeDamage);

			Vector3 direction = (playerTransform.position - transform.position).normalized;
			var playerController = playerHealth.GetComponent<CharacterController>();
			if (playerController != null)
			{
				playerController.Move(direction * bellyImpactForce * Time.deltaTime);
			}
		}

		navMeshAgent.speed = originalSpeed;
		navMeshAgent.acceleration = originalAcceleration;
		currentState = ZombieState.Stunned;
		stunTimer = stunDuration;
		animator.SetTrigger("Stunned");
	}

	private void UpdateAttacking(float distanceToPlayer)
	{
		if (distanceToPlayer > attackRange * 1.2f)
		{
			currentState = ZombieState.Chasing;
			return;
		}

		if (Time.time - lastAttackTime >= attackCooldown)
		{
			Attack();
		}
	}

	private void Attack()
	{
		lastAttackTime = Time.time;
		transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
		animator.SetTrigger("Attack");

		if (playerHealth != null && Vector3.Distance(transform.position, playerTransform.position) <= attackRange)
		{
			playerHealth.TakeDamage(attackDamage);
		}
	}

	private void UpdateStunned()
	{
		stunTimer -= Time.deltaTime;
		if (stunTimer <= 0)
		{
			currentState = ZombieState.Chasing;
			animator.SetTrigger("Recover");
		}
	}

	private void UpdateAnimation()
	{
		if (animator != null)
		{
			bool isMoving = navMeshAgent.velocity.magnitude > 0.1f;
			animator.SetBool("IsMoving", isMoving);
			animator.SetBool("IsChasing", currentState == ZombieState.Chasing || currentState == ZombieState.Charging);
			animator.SetFloat("MoveSpeed", navMeshAgent.velocity.magnitude / navMeshAgent.speed);
		}
	}

	private void SetRandomPatrolPoint()
	{
		Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * patrolRadius;
		randomDirection += spawnPosition;

		NavMeshHit hit;
		if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
		{
			if (Vector3.Distance(transform.position, hit.position) >= patrolPointMinDistance)
			{
				currentPatrolPoint = hit.position;
				navMeshAgent.SetDestination(currentPatrolPoint);
			}
			else
			{
				SetRandomPatrolPoint();
			}
		}
	}

	public void ApplyDamage(float damage)
	{
		TakeDamage(Mathf.RoundToInt(damage));
	}

	public void TakeDamage(int damage)
	{
		currentHealth -= damage;

		if (currentHealth <= 0)
		{
			Die();
		}
		else if (currentHealth <= maxHealth / 3 && !isExploded)
		{
			Explode();
		}
		else
		{
			ShowDamageEffect();
		}
	}

	private void Explode()
	{
		isExploded = true;
		animator.SetTrigger("Explode");

		Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
		foreach (Collider hit in colliders)
		{
			PlayerHealth health = hit.GetComponent<PlayerHealth>();
			if (health != null)
			{
				health.TakeDamage(explosionDamage);

				Vector3 direction = (hit.transform.position - transform.position).normalized;
				var controller = health.GetComponent<CharacterController>();
				if (controller != null)
				{
					controller.Move(direction * bellyImpactForce * Time.deltaTime);
				}
			}
		}

		if (explosionEffect != null)
		{
			Instantiate(explosionEffect, transform.position, Quaternion.identity);
		}

		Die();
	}

	void ShowDamageEffect()
	{
		if (damageEffectPrefab != null)
		{
			GameObject effect = Instantiate(damageEffectPrefab, transform.position, Quaternion.identity, transform);
			Destroy(effect, effectDuration);
		}
	}

	void Die()
	{
		enabled = false;
		navMeshAgent.enabled = false;

		var collider = GetComponent<Collider>();
		if (collider != null) collider.enabled = false;

		if (deathEffect != null)
		{
			Instantiate(deathEffect, transform.position, Quaternion.identity);
		}

		if (playerInv != null)
		{
			playerInv.AddUltimatePoints(Cost);
		}

		OnDeath?.Invoke();

		Destroy(gameObject, 0.1f);
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

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, detectionRadius);
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, chaseRadius);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(transform.position, chargeDistance);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, explosionRadius);
	}
}