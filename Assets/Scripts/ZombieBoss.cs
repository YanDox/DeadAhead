using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ZombieBoss : MonoBehaviour
{
	public enum BossState
	{
		Patrolling,
		Chasing,
		Returning,
		PreparingDash,
		Dashing,
		Attacking
	}

	[Header("Movement Settings")]
	public float detectionRadius = 15f;
	public float chaseRadius = 25f;
	public float patrolRadius = 30f;
	public float patrolPointMinDistance = 8f;
	public float patrolWaitTime = 3f;
	public float rotationSpeed = 5f;

	[Header("Attack Settings")]
	public float attackRange = 2f;
	public int meleeDamage = 25;
	public float meleeCooldown = 1.5f;

	[Header("Dash Attack Settings")]
	public float dashDistance = 10f;
	public float dashSpeed = 20f;
	public float dashCooldown = 10f;
	public float dashPreparationTime = 1f;
	public float dashDuration = 0.5f;
	public int dashDamage = 40; // Изменено с float на int
	public float dashImpactForce = 15f;

	[Header("Audio References")]
	public AudioClip dashSound;
	public AudioClip roarSound;
	public AudioClip attackSound;

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
	private float lastDashTime;
	private float dashTimer;
	private bool isDashing = false;
	private Vector3 dashDirection;
	private bool isAttackAnimationPlaying = false;

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
		else
		{
			Debug.LogError("Player controller not found!");
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

		if (distanceToPlayer <= detectionRadius)
		{
			FacePlayer();
		}

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

			case BossState.PreparingDash:
				UpdateDashPreparation(distanceToPlayer);
				break;

			case BossState.Dashing:
				break;

			case BossState.Attacking:
				break;
		}

		UpdateAnimation();
	}

	private void FacePlayer()
	{
		Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
		directionToPlayer.y = 0;
		if (directionToPlayer != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		}
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

		if (Time.time - lastDashTime >= dashCooldown && !isDashing)
		{
			StartDashPreparation();
			return;
		}

		navMeshAgent.SetDestination(playerTransform.position);

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

		if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
		{
			currentState = BossState.Patrolling;
			SetRandomPatrolPoint();
		}
	}

	private void StartDashPreparation()
	{
		currentState = BossState.PreparingDash;
		navMeshAgent.isStopped = true;
		dashTimer = 0f;

		if (roarSound != null)
		{
			audioSource.PlayOneShot(roarSound);
		}

		if (animator != null)
		{
			animator.SetTrigger("PrepareDash");
		}
	}

	private void UpdateDashPreparation(float distanceToPlayer)
	{
		dashTimer += Time.deltaTime;
		FacePlayer();

		if (dashTimer >= dashPreparationTime)
		{
			StartDash();
		}
		else if (distanceToPlayer <= attackRange)
		{
			CancelDash();
			MeleeAttack();
		}
	}

	private void StartDash()
	{
		currentState = BossState.Dashing;
		isDashing = true;
		dashDirection = (playerTransform.position - transform.position).normalized;
		dashDirection.y = 0;

		if (animator != null)
		{
			animator.SetTrigger("Dash");
		}

		if (dashSound != null)
		{
			audioSource.PlayOneShot(dashSound);
		}

		StartCoroutine(PerformDash());
	}

	private IEnumerator PerformDash()
	{
		float startTime = Time.time;
		Vector3 startPosition = transform.position;
		Vector3 targetPosition = startPosition + dashDirection * dashDistance;

		NavMeshHit hit;
		if (NavMesh.SamplePosition(targetPosition, out hit, dashDistance, NavMesh.AllAreas))
		{
			targetPosition = hit.position;
		}

		while (Time.time < startTime + dashDuration)
		{
			if (currentState != BossState.Dashing) yield break;

			float t = (Time.time - startTime) / dashDuration;
			Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, t);
			navMeshAgent.Warp(newPosition);

			CheckDashHit();
			yield return null;
		}

		CompleteDash();
	}

	private void CheckDashHit()
	{
		if (playerHealth == null) return;

		float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
		if (distanceToPlayer <= attackRange * 1.5f)
		{
			Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
			float dot = Vector3.Dot(toPlayer, dashDirection);

			if (dot > 0.7f)
			{
				playerHealth.TakeDamage(dashDamage);

				Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
				if (playerRb != null)
				{
					playerRb.AddForce(dashDirection * dashImpactForce, ForceMode.Impulse);
				}
			}
		}
	}

	private void CompleteDash()
	{
		isDashing = false;
		navMeshAgent.isStopped = false;
		lastDashTime = Time.time;
		currentState = BossState.Chasing;
	}

	private void CancelDash()
	{
		isDashing = false;
		navMeshAgent.isStopped = false;
		currentState = BossState.Chasing;
	}

	private void MeleeAttack()
	{
		if (Time.time - lastMeleeAttackTime >= meleeCooldown && !isAttackAnimationPlaying)
		{
			lastMeleeAttackTime = Time.time;
			currentState = BossState.Attacking;
			isAttackAnimationPlaying = true;

			if (animator != null)
			{
				animator.SetTrigger("Attack");
			}

			if (attackSound != null)
			{
				audioSource.PlayOneShot(attackSound);
			}
		}
	}

	public void OnAttackHit()
	{
		if (playerHealth != null && Vector3.Distance(transform.position, playerTransform.position) <= attackRange * 1.2f)
		{
			playerHealth.TakeDamage(meleeDamage);
		}
	}

	public void OnAttackComplete()
	{
		isAttackAnimationPlaying = false;
		currentState = BossState.Chasing;
	}

	private void SetRandomPatrolPoint()
	{
		Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
		randomDirection += spawnPosition;
		randomDirection.y = spawnPosition.y;

		NavMeshHit hit;
		if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
		{
			float distance = Vector3.Distance(transform.position, hit.position);
			if (distance >= patrolPointMinDistance)
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
		if (animator == null) return;

		bool isMoving = navMeshAgent.velocity.magnitude > 0.1f &&
					   currentState != BossState.PreparingDash &&
					   currentState != BossState.Dashing &&
					   currentState != BossState.Attacking;

		animator.SetBool("IsMoving", isMoving);
		animator.SetBool("IsChasing", currentState == BossState.Chasing);
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