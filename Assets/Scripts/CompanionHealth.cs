using UnityEngine;

public class CompanionHealth : MonoBehaviour, IEntity
{
	[Header("Health Settings")]
	public int maxHealth = 80;
	public int currentHealth;
	public float deathAnimationTime = 2f;
	public GameObject deathEffect;
	public GameObject zombiePrefab; // Префаб зомби для замены
    public bool isDead = false;

    private CompanionAI companionAI;
    private Renderer[] renderers; // Для отключения видимости
    private Collider[] colliders; // Для отключения коллайдеров

    void Start()
	{
		currentHealth = maxHealth;
		companionAI = GetComponent<CompanionAI>();

        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
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
        if (isDead) return;
        isDead = true;

        // 1. Отключаем визуальное отображение
        ToggleVisibility(false);

        // 2. Отключаем физику и коллайдеры
        ToggleColliders(false);

        // 3. Отключаем AI
        if (companionAI != null)
            companionAI.enabled = false;

        // 4. Создаем эффект смерти
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // 5. Создаем зомби (если задан префаб)
        if (zombiePrefab != null)
        {
            Instantiate(zombiePrefab, transform.position, transform.rotation);
        }

        // 6. Уничтожаем объект с задержкой
        Destroy(gameObject, deathAnimationTime);
    }

    // Метод для отключения/включения видимости
    private void ToggleVisibility(bool state)
    {
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = state;
        }
    }

    // Метод для отключения/включения коллайдеров
    private void ToggleColliders(bool state)
    {
        foreach (var collider in colliders)
        {
            if (collider != null)
                collider.enabled = state;
        }
    }
}