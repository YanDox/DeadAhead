using UnityEngine;

public class Health : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private int healAmount = 30;
	private Transform spawnPoint;

	public void SetSpawnPoint(Transform point)
	{
		if (point != null) // Проверка на null
		{
			spawnPoint = point;
		}
	}

	public void SetHealAmount(int amount) => healAmount = amount;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
			if (playerHealth != null && playerHealth.CanHeal())
			{
				playerHealth.Heal(healAmount);
				if (spawnPoint != null) // Проверка перед вызовом
				{
					MedkitSpawner.Instance?.HandleMedkitPickedUp(spawnPoint);
				}
				Destroy(gameObject);
			}
		}
	}

}