using UnityEngine;

public class EnemyHealth : MonoBehaviour, IEntity
{
	[Header("Health Settings")]
	public int maxHealth = 100;
	public int currentHealth;
	public float deathAnimationTime = 1.5f;
	public GameObject deathEffect;

	[Header("Damage Feedback")]
	public Material damageMaterial;
	public float flashDuration = 0.1f;
	private Material originalMaterial;
	private SkinnedMeshRenderer meshRenderer;

	void Start()
	{
		currentHealth = maxHealth;
		meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
		if (meshRenderer != null)
		{
			originalMaterial = meshRenderer.material;
		}
	}

	

	public void ApplyDamage(float damage)
	{
		TakeDamage(Mathf.RoundToInt(damage)); // Конвертируем float в int
	}

	public void TakeDamage(int damage)
	{
		Debug.Log($"Taking damage: {damage}");
		currentHealth -= damage;

		if (currentHealth <= 0)
		{
			Die();
		}
		
	}

	

	void Die()
	{
		var zombieAI = GetComponent<Zombie>();
		if (zombieAI != null) zombieAI.enabled = false;

		var collider = GetComponent<Collider>();
		if (collider != null) collider.enabled = false;

		Destroy(gameObject, deathAnimationTime);
	}
}