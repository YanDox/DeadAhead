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

    [Header("Medkit Settings")]
    [SerializeField] private int medkitHealAmount = 30;

    private AudioSource audioSource;
	private SC_TPSController playerController;
    private Inventory inventory;
    private bool isDead = false;

	void Start()
	{
		currentHealth = maxHealth;
		audioSource = GetComponent<AudioSource>();
        inventory = GetComponent<Inventory>();
        playerController = GetComponent<SC_TPSController>();
	}

 
    public bool CanHeal() => currentHealth < maxHealth;

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Здоровье восстановлено! Текущее: {currentHealth}/{maxHealth}");
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