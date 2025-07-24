using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class Psycho : EnemyAI
{
	[Header("Psycho Specific Settings")]
	public LayerMask zombieLayer;
	private List<Zombie> zombiesInRange = new List<Zombie>();

	[Header("Animation Settings")]
	public float rotationSpeedDuringAttack = 10f;
	public float attackAnimationDelay = 0.3f; // Задержка перед нанесением урона в анимации
	private bool isAttacking = false;
	private float attackAnimationTimer = 0f;

	protected override void Start()
	{
		base.Start();
		canAttackOtherEnemies = true;
		enemyTags = new string[] { "Zombie" };
	}

	protected override void Update()
	{
		base.Update();

		// Обработка таймера анимации атаки
		if (isAttacking)
		{
			attackAnimationTimer -= Time.deltaTime;
			if (attackAnimationTimer <= 0)
			{
				isAttacking = false;
			}
		}
	}

	protected override void FindAllTargets()
	{
		base.FindAllTargets();
		zombiesInRange.Clear();
		int numZombies = Physics.OverlapSphereNonAlloc(
			transform.position,
			detectionRadius,
			hitColliders,
			zombieLayer
		);

		for (int i = 0; i < numZombies; i++)
		{
			Zombie zombie = hitColliders[i].GetComponent<Zombie>();
			if (zombie != null && !zombie.GetComponent<EnemyHealth>().isDead)
			{
				zombiesInRange.Add(zombie);
			}
		}
	}

	protected override Transform GetSpecialTarget()
	{
		Zombie closestZombie = null;
		float minDistance = float.MaxValue;

		foreach (var zombie in zombiesInRange)
		{
			if (zombie != null && !zombie.GetComponent<EnemyHealth>().isDead)
			{
				float distance = Vector3.Distance(transform.position, zombie.transform.position);
				if (distance < minDistance && distance <= detectionRadius)
				{
					minDistance = distance;
					closestZombie = zombie;
				}
			}
		}
		return closestZombie?.transform;
	}

	protected override void AttackImplementation()
	{
		if (currentTarget == null || isAttacking) return;

		// Запуск анимации атаки
		if (animator != null)
		{
			animator.SetTrigger("Attack");
			isAttacking = true;
			attackAnimationTimer = attackAnimationDelay;

			// Запускаем корутину для нанесения урона с задержкой
			StartCoroutine(DealDamageAfterAnimation());
		}
	}

	private IEnumerator DealDamageAfterAnimation()
	{
		// Поворачиваемся к цели в начале атаки
		Vector3 targetPosition = new Vector3(
			currentTarget.position.x,
			transform.position.y,
			currentTarget.position.z
		);

		// Плавный поворот во время анимации атаки
		float rotationTimer = 0f;
		while (rotationTimer < attackAnimationDelay)
		{
			rotationTimer += Time.deltaTime;
			Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				targetRotation,
				rotationSpeedDuringAttack * Time.deltaTime
			);
			yield return null;
		}

		// Нанесение урона после завершения поворота
		if (currentTarget != null)
		{
			CompanionHealth companion = currentTarget.GetComponent<CompanionHealth>();
			if (companion != null)
			{
				companion.TakeDamage(attackDamage);
				yield break;
			}

			if (currentTarget.CompareTag("Player") && playerHealth != null)
			{
				playerHealth.TakeDamage(attackDamage);
				yield break;
			}

			if (currentTarget.CompareTag("Zombie"))
			{
				EnemyHealth zombieHealth = currentTarget.GetComponent<EnemyHealth>();
				if (zombieHealth != null)
				{
					Transform originalTarget = currentTarget;
					zombieHealth.TakeDamage(attackDamage);

					if (zombieHealth.isDead && currentTarget == originalTarget)
					{
						currentTarget = null;
					}
				}
				yield break;
			}

			EnemyHealth enemy = currentTarget.GetComponent<EnemyHealth>();
			if (enemy != null)
			{
				enemy.TakeDamage(attackDamage);
				if (enemy.isDead)
				{
					currentTarget = null;
					currentState = EnemyState.Returning;
				}
			}
		}
	}

	protected override void UpdateAnimation()
	{
		if (animator != null)
		{
			// Базовые анимации движения
			bool isMoving = navMeshAgent.velocity.magnitude > 0.1f;
			animator.SetBool("IsMoving", isMoving);
			animator.SetBool("IsChasing", currentState == EnemyState.Chasing);

			// Анимация атаки (управляется через триггер в AttackImplementation)

			// Анимация состояния
			animator.SetBool("IsAttacking", currentState == EnemyState.Attacking);
			animator.SetBool("IsPatrolling", currentState == EnemyState.Patrolling);
			animator.SetBool("IsReturning", currentState == EnemyState.Returning);
		}
	}

	public void ForceChasePlayer(Transform playerTarget)
	{
		ForceChaseTarget(playerTarget);
	}
}