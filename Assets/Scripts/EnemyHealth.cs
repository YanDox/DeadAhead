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
	private Inventory playerInv;

	void Start()
	{
		currentHealth = maxHealth;
		playerInv = FindObjectOfType<Inventory>();
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
		else
		{
			ShowDamageEffect();
		}
	}

	void ShowDamageEffect()
	{
		if (damageEffectPrefab != null)
		{
			// Создаем эффект и уничтожаем его через заданное время
			GameObject effect = Instantiate(damageEffectPrefab, transform.position, Quaternion.identity, transform);
			Destroy(effect, effectDuration);
		}
	}

	void Die()
	{
		var zombieAI = GetComponent<Zombie>();
		if (zombieAI != null) zombieAI.enabled = false;

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

		// Вызываем событие смерти перед уничтожением
		OnDeath?.Invoke();

		Destroy(gameObject, 0);
	}
}