using UnityEngine;
using System.Collections;
using System;

public class EnemyHealth : MonoBehaviour, IEntity
{
	[Header("Health Settings")]
	public int maxHealth = 100;
	public int currentHealth;
	public float deathAnimationTime = 1.5f;
	public GameObject deathEffect;
	public int Cost = 10;
	public event Action OnDeath;

	[Header("Damage Feedback")]
	public GameObject damageEffectPrefab; // Префаб эффекта получения урона
	public float effectDuration = 0.3f; // Длительность эффекта
	public bool isDead { get; private set; }

	private Inventory playerInv;

	void Start()
	{
		currentHealth = maxHealth;
		playerInv = FindObjectOfType<Inventory>();
		isDead = false;
	}

	public void ApplyDamage(float damage)
	{
		TakeDamage(Mathf.RoundToInt(damage), transform.position); // По умолчанию в центр объекта
	}

	// Новая версия метода для попаданий с указанием точки
	public void TakeDamage(int damage, Vector3 hitPoint)
	{
		if (isDead) return;

		currentHealth -= damage;

		if (currentHealth <= 0)
		{
			Die();
		}
		else
		{
			ShowDamageEffect(hitPoint);
		}
	}

	// Старая версия для совместимости
	public void TakeDamage(int damage)
	{
		TakeDamage(damage, transform.position);
	}

	void ShowDamageEffect(Vector3 hitPoint)
	{
		if (damageEffectPrefab != null)
		{
			// Создаем эффект в точке попадания
			GameObject effect = Instantiate(
				damageEffectPrefab,
				hitPoint,
				Quaternion.LookRotation(transform.position - hitPoint), // Разворачиваем эффект "от" врага
				transform
			);
			Destroy(effect, effectDuration);
		}
	}

	void Die()
	{
		isDead = true;

		var enemyAI = GetComponent<EnemyAI>();
		if (enemyAI != null) enemyAI.enabled = false;

		foreach (var col in GetComponentsInChildren<Collider>())
			col.enabled = false;

		Rigidbody rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.isKinematic = true;
			rb.detectCollisions = false;
		}

		if (deathEffect != null)
		{
			Instantiate(deathEffect, transform.position, Quaternion.identity);
		}

		if (playerInv != null)
		{
			playerInv.AddUltimatePoints(Cost);
		}

		OnDeath?.Invoke();
		Destroy(gameObject, 0);
	}
}