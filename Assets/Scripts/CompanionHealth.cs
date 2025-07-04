using UnityEngine;

public class CompanionHealth : MonoBehaviour, IEntity
{
	[Header("Health Settings")]
	public int maxHealth = 80;
	public int currentHealth;
	public float deathAnimationTime = 2f;
	public GameObject deathEffect;
	public GameObject zombiePrefab; // ������ ����� ��� ������
    public bool isDead = false;

    private CompanionAI companionAI;

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
	}

	public void ApplyDamage(float damage)
	{
		TakeDamage((int)damage);
	}

	void Die()
	{
		isDead = true;

		// ��������� ���������� ����������
		if (companionAI != null) companionAI.enabled = false;
		var collider = GetComponent<Collider>();
		if (collider != null) collider.enabled = false;

		// ������ ������
		if (deathEffect != null)
		{
			Instantiate(deathEffect, transform.position, Quaternion.identity);
		}

		// �������� �� �����
		if (zombiePrefab != null)
		{
			Instantiate(zombiePrefab, transform.position, transform.rotation);
		}

		// ���������� ����������
		Destroy(gameObject, deathAnimationTime);
	}
}