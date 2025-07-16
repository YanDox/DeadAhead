using UnityEngine;
using System.Collections;

public class CompanionHealth : MonoBehaviour, IEntity
{
    [Header("Health Settings")]
    public int maxHealth = 80;
    public int currentHealth;
    public GameObject deathEffect;
    public GameObject zombiePrefab;
    public bool isDead = false;

    [Header("Damage Feedback")]
    public GameObject damageEffectPrefab;
    public float effectDuration = 0.3f;

    private CompanionAI companionAI;
    private Collider companionCollider;

	public event System.Action<GameObject> OnDeath;

	void Start()
    {
        currentHealth = maxHealth;
        companionAI = GetComponent<CompanionAI>();
        companionCollider = GetComponent<Collider>();
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

		// �������� ������� ����� ������������
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
            Vector3 spawnPosition = transform.position;
            FindNearestNavMeshPoint(transform.position, 2f, out spawnPosition);

            Instantiate(zombiePrefab, spawnPosition, transform.rotation);
        }

        Destroy(gameObject, 0.1f);
    }

    bool FindNearestNavMeshPoint(Vector3 position, float maxDistance, out Vector3 result)
    {
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out hit, maxDistance, UnityEngine.AI.NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = position;
        return false;
    }
}