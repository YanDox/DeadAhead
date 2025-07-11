using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZombieSpawnQuest : MonoBehaviour
{
	[Header("Spawn Settings")]
	public GameObject zombiePrefab;
	public int zombiesToSpawn = 10;
	public float spawnRadius = 20f;
	public float spawnDelay = 3f;
	public int zombiesToKill = 5;

	[Header("Quest Objects")]
	public GameObject protectiveDome;
	public GameObject keyItem;

	[Header("Terrain Settings")]
	public Terrain terrain; // Ссылка на террейн
	public float yOffset = 0.5f; // Небольшой отступ от земли

	private List<GameObject> spawnedZombies = new List<GameObject>();
	private int zombiesKilled = 0;
	private bool questCompleted = false;

	void Start()
	{
		// Если террейн не назначен, попробуем найти его автоматически
		if (terrain == null)
		{
			terrain = Terrain.activeTerrain;
		}

		StartCoroutine(SpawnZombies());

		if (keyItem != null)
			keyItem.SetActive(false);
	}

	IEnumerator SpawnZombies()
	{
		for (int i = 0; i < zombiesToSpawn; i++)
		{
			if (questCompleted) yield break;

			Vector3 randomPos = GetValidSpawnPosition();
			GameObject zombie = Instantiate(zombiePrefab, randomPos, Quaternion.identity);
			spawnedZombies.Add(zombie);

			EnemyHealth health = zombie.GetComponent<EnemyHealth>();
			if (health != null)
			{
				health.OnDeath += HandleZombieDeath;
			}

			yield return new WaitForSeconds(spawnDelay);
		}
	}

	private Vector3 GetValidSpawnPosition()
	{
		Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
		Vector3 randomPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

		// Получаем высоту террейна в этой точке
		if (terrain != null)
		{
			randomPos.y = terrain.SampleHeight(randomPos) + yOffset;
		}
		else
		{
			// Если террейн не найден, используем Raycast для определения поверхности
			RaycastHit hit;
			if (Physics.Raycast(randomPos + Vector3.up * 100, Vector3.down, out hit, Mathf.Infinity))
			{
				randomPos.y = hit.point.y + yOffset;
			}
		}

		return randomPos;
	}

	private void HandleZombieDeath()
	{
		zombiesKilled++;
		Debug.Log($"Зомби убито: {zombiesKilled}/{zombiesToKill}");

		if (zombiesKilled >= zombiesToKill && !questCompleted)
		{
			CompleteQuest();
		}
	}

	private void CompleteQuest()
	{
		questCompleted = true;

		if (protectiveDome != null)
			protectiveDome.SetActive(false);

		if (keyItem != null)
			keyItem.SetActive(true);

		Debug.Log("Квест завершен! Защитный купол деактивирован.");
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, spawnRadius);
	}

	void OnDestroy()
	{
		foreach (var zombie in spawnedZombies)
		{
			if (zombie != null)
			{
				EnemyHealth health = zombie.GetComponent<EnemyHealth>();
				if (health != null)
				{
					health.OnDeath -= HandleZombieDeath;
				}
			}
		}
	}
}