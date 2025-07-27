using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MedkitSpawner : MonoBehaviour
{
	public static MedkitSpawner Instance;

	[Header("Настройки спавна")]
	public GameObject medkitPrefab;
	public float respawnTime = 45f;
	public Transform[] spawnPoints;
	public int healAmount = 30;

	private Dictionary<Transform, GameObject> activeMedkits = new Dictionary<Transform, GameObject>();
	private Dictionary<Transform, Coroutine> respawnCoroutines = new Dictionary<Transform, Coroutine>();

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	void Start()
	{
		foreach (Transform spawnPoint in spawnPoints)
		{
			if (spawnPoint != null) // Проверка на null
			{
				SpawnMedkitAtPoint(spawnPoint);
			}
		}
	}

	public void SpawnMedkitAtPoint(Transform spawnPoint)
	{
		if (spawnPoint == null || activeMedkits.ContainsKey(spawnPoint)) return;

		GameObject medkit = Instantiate(medkitPrefab, spawnPoint.position, spawnPoint.rotation);
		var pickup = medkit.GetComponent<Medkit>() ?? medkit.AddComponent<Medkit>();
		pickup.SetSpawnPoint(spawnPoint);
		pickup.SetHealAmount(healAmount);

		activeMedkits[spawnPoint] = medkit;
	}

	public void HandleMedkitPickedUp(Transform spawnPoint)
	{
		if (spawnPoint == null) return; // Защита от null

		if (activeMedkits.TryGetValue(spawnPoint, out var medkit))
		{
			activeMedkits.Remove(spawnPoint);
		}

		if (respawnCoroutines.TryGetValue(spawnPoint, out var routine))
		{
			if (routine != null)
			{
				StopCoroutine(routine);
			}
		}

		respawnCoroutines[spawnPoint] = StartCoroutine(RespawnMedkitAfterDelay(spawnPoint));
	}

	IEnumerator RespawnMedkitAfterDelay(Transform spawnPoint)
	{
		yield return new WaitForSeconds(respawnTime);
		SpawnMedkitAtPoint(spawnPoint);
		respawnCoroutines.Remove(spawnPoint);
	}

	void OnDestroy()
	{
		foreach (var coroutine in respawnCoroutines.Values)
		{
			if (coroutine != null) StopCoroutine(coroutine);
		}
	}
}