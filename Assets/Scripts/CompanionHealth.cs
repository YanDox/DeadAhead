using UnityEngine;
using System.Collections;

public class CompanionHealth : MonoBehaviour, IEntity
{
    [Header("Health Settings")]
    public int maxHealth = 80;
    public int currentHealth;
    public float deathAnimationTime = 1f;
    public GameObject deathEffect;
    public GameObject zombiePrefab;
    public bool isDead = false;

    [Header("Damage Feedback")]
    public GameObject damageEffectPrefab;
    public float effectDuration = 0.3f;

    private CompanionAI companionAI;

	public event System.Action<GameObject> OnDeath;

	void Start()
    {
        currentHealth = maxHealth;
        companionAI = GetComponent<CompanionAI>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

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

    public void ApplyDamage(float damage)
    {
        TakeDamage((int)damage);
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
		if (isDead) return;
		isDead = true;

		// Вызываем событие перед уничтожением
		OnDeath?.Invoke(gameObject);

		if (companionAI != null) companionAI.enabled = false;
		var collider = GetComponent<Collider>();
		if (collider != null) collider.enabled = false;

		if (deathEffect != null)
		{
			Instantiate(deathEffect, transform.position, Quaternion.identity);
		}

		StartCoroutine(ReplaceWithZombieAfterDelay());
	}

	IEnumerator ReplaceWithZombieAfterDelay()
    {
        yield return new WaitForSeconds(deathAnimationTime);

        if (zombiePrefab != null)
        {
            GameObject zombieInstance = Instantiate(zombiePrefab, transform.position, transform.rotation);
            zombieInstance.SetActive(false);

            if (FindNearestNavMeshPoint(transform.position, 2f, out Vector3 spawnPosition))
            {
                zombieInstance.transform.position = spawnPosition;
            }

            zombieInstance.SetActive(true);
        }

        Destroy(gameObject);
    }

    bool FindNearestNavMeshPoint(Vector3 position, float maxDistance, out Vector3 result)
    {
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = position;
        return false;
    }
}