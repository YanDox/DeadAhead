using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AmmoCrateSpawner : MonoBehaviour
{
	[Header("Настройки спавна")]
	public GameObject ammoCratePrefab;
	public float respawnTime = 45f;
	public Transform[] spawnPoints;
	public int ammoAmount = 30;

	private Dictionary<Transform, GameObject> activeCrates = new Dictionary<Transform, GameObject>();
	private Dictionary<Transform, Coroutine> respawnCoroutines = new Dictionary<Transform, Coroutine>();

	void Start()
	{
		foreach (Transform spawnPoint in spawnPoints)
		{
			SpawnAmmoCrateAtPoint(spawnPoint);
		}
	}

	void SpawnAmmoCrateAtPoint(Transform spawnPoint)
	{
		if (activeCrates.ContainsKey(spawnPoint) && activeCrates[spawnPoint] != null)
			return;

		GameObject crate = Instantiate(ammoCratePrefab, spawnPoint.position, spawnPoint.rotation);
		var pickup = crate.AddComponent<AmmoCratePickup>();
		pickup.SetSpawnPoint(spawnPoint);
		pickup.SetAmmoAmount(ammoAmount);
		pickup.OnPickedUp += (point) => HandleAmmoCratePickedUp(point);

		activeCrates[spawnPoint] = crate;
	}

	void HandleAmmoCratePickedUp(Transform spawnPoint)
	{
		if (respawnCoroutines.ContainsKey(spawnPoint) && respawnCoroutines[spawnPoint] != null)
			StopCoroutine(respawnCoroutines[spawnPoint]);

		respawnCoroutines[spawnPoint] = StartCoroutine(RespawnAmmoCrateAfterDelay(spawnPoint));
	}

	IEnumerator RespawnAmmoCrateAfterDelay(Transform spawnPoint)
	{
		yield return new WaitForSeconds(respawnTime);
		SpawnAmmoCrateAtPoint(spawnPoint);
		respawnCoroutines.Remove(spawnPoint);
	}

	void OnDestroy()
	{
		foreach (var coroutine in respawnCoroutines.Values)
		{
			if (coroutine != null)
				StopCoroutine(coroutine);
		}
	}
}

public class AmmoCratePickup : MonoBehaviour
{
	public System.Action<Transform> OnPickedUp;
	private Transform spawnPoint;
	private int ammoAmount;

	public void SetSpawnPoint(Transform point)
	{
		spawnPoint = point;
	}

	public void SetAmmoAmount(int amount)
	{
		ammoAmount = amount;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Inventory inventory = other.GetComponent<Inventory>();
			if (inventory != null && inventory.AddItem(Inventory.RIFLE_AMMO, ammoAmount))
			{
				OnPickedUp?.Invoke(spawnPoint);
				Destroy(gameObject);
			}
		}
	}
}