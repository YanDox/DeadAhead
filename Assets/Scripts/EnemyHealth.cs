using UnityEngine;
using System.Collections;
public class EnemyHealth : MonoBehaviour, IEntity
{
	[Header("Health Settings")]
	public int maxHealth = 100;
	public int currentHealth;
	public float deathAnimationTime = 1.5f;
	public GameObject deathEffect;
	public int Cost = 10;

	[Header("Damage Feedback")]
	public Material damageMaterial;
	public float flashDuration = 0.1f;
	private Material originalMaterial;
	private SkinnedMeshRenderer meshRenderer;
	private Inventory playerInv;

	void Start()
	{
		currentHealth = maxHealth;
		meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
		if (meshRenderer != null)
		{
			originalMaterial = meshRenderer.material;
		}

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
			StartCoroutine(DamageFlash());
		}
	}

	IEnumerator DamageFlash()
	{
		if (meshRenderer != null && damageMaterial != null)
		{
			meshRenderer.material = damageMaterial;
			yield return new WaitForSeconds(flashDuration);
			meshRenderer.material = originalMaterial;
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

		Destroy(gameObject,0);
	}
}