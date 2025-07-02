using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
	[Header("Health Settings")]
	public int maxHealth = 100;
	public int currentHealth;
	public float deathAnimationTime = 2f;
	public GameObject deathEffect;
	public AudioClip damageSound;
	public AudioClip deathSound;

	private AudioSource audioSource;
	private SC_TPSController playerController;
	private bool isDead = false;

	void Start()
	{
		currentHealth = maxHealth;
		audioSource = GetComponent<AudioSource>();
		playerController = GetComponent<SC_TPSController>();
	}

	public void TakeDamage(int damage)
	{
		if (isDead) return;

		currentHealth -= damage;

		if (audioSource != null && damageSound != null)
		{
			audioSource.PlayOneShot(damageSound);
		}

		if (currentHealth <= 0)
		{
			Die();
		}
	}

	public void ApplyDamage(float damage)
	{
		TakeDamage((int)damage);
	}

	void Die()
	{
		isDead = true;
		currentHealth = 0;

		if (playerController != null)
		{
			playerController.canMove = false;
		}

		if (audioSource != null && deathSound != null)
		{
			audioSource.PlayOneShot(deathSound);
		}

		if (deathEffect != null)
		{
			Instantiate(deathEffect, transform.position, Quaternion.identity);
		}

		Destroy(gameObject, deathAnimationTime);
	}
}
