using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SC_EnemySpawner : MonoBehaviour
{
	[Header("Spawn Settings")]
	public Terrain terrain;
	public int totalEnemies = 50;
	public int totalCompanions = 3;
	public float spawnHeightAboveGround = 1f;
	public float minSpawnDistanceFromPlayer = 20f;
	public float maxSpawnDistanceFromPlayer = 100f;

	[Header("Enemy Prefabs")]
	public GameObject regularZombiePrefab;
	public GameObject fatZombiePrefab;
	public GameObject bossZombiePrefab;
	public GameObject commanderZombiePrefab;
	public GameObject psychoPrefab;

	[Header("Companion Prefab")]
	public GameObject companionPrefab;

	[Header("Spawn Ratios")]
	[Range(0f, 1f)] public float regularZombieRatio = 0.6f;
	[Range(0f, 1f)] public float fatZombieRatio = 0.2f;
	[Range(0f, 1f)] public float bossZombieRatio = 0.1f;
	[Range(0f, 1f)] public float commanderZombieRatio = 0.08f;
	[Range(0f, 1f)] public float psychoRatio = 0.02f;

	private List<GameObject> activeCompanions = new List<GameObject>();
	private Transform playerTransform;
	private bool isInitialized = false;

	void Start()
	{
		// Find player
		var playerController = FindObjectOfType<SC_TPSController>();
		if (playerController != null)
		{
			playerTransform = playerController.transform;
		}

		// Validate ratios
		float totalRatio = regularZombieRatio + fatZombieRatio + bossZombieRatio + commanderZombieRatio + psychoRatio;
		if (Mathf.Abs(totalRatio - 1f) > 0.01f)
		{
			Debug.LogWarning("Enemy spawn ratios don't sum to 1. Adjusting automatically.");
			float adjustment = 1f / totalRatio;
			regularZombieRatio *= adjustment;
			fatZombieRatio *= adjustment;
			bossZombieRatio *= adjustment;
			commanderZombieRatio *= adjustment;
			psychoRatio *= adjustment;
		}

		// Initialize spawner
		if (terrain == null)
		{
			terrain = Terrain.activeTerrain;
			if (terrain == null)
			{
				Debug.LogError("No terrain found in scene!");
				return;
			}
		}

		SpawnInitialEnemies();
		SpawnInitialCompanions();
		isInitialized = true;
	}

	void SpawnInitialEnemies()
	{
		for (int i = 0; i < totalEnemies; i++)
		{
			SpawnEnemy();
		}
	}

	void SpawnInitialCompanions()
	{
		for (int i = 0; i < totalCompanions; i++)
		{
			SpawnCompanion();
		}
	}

	void SpawnEnemy()
	{
		if (playerTransform == null) return;

		Vector3 spawnPosition = GetRandomSpawnPosition();
		if (spawnPosition == Vector3.zero) return;

		// Determine which enemy to spawn based on ratios
		float randomValue = Random.value;
		GameObject enemyPrefab = regularZombiePrefab;

		if (randomValue < psychoRatio)
		{
			enemyPrefab = psychoPrefab;
		}
		else if (randomValue < psychoRatio + commanderZombieRatio)
		{
			enemyPrefab = commanderZombiePrefab;
		}
		else if (randomValue < psychoRatio + commanderZombieRatio + bossZombieRatio)
		{
			enemyPrefab = bossZombiePrefab;
		}
		else if (randomValue < psychoRatio + commanderZombieRatio + bossZombieRatio + fatZombieRatio)
		{
			enemyPrefab = fatZombiePrefab;
		}

		if (enemyPrefab != null)
		{
			Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
		}
	}

	public void EnemyEliminated(SC_NPCEnemy enemy)
	{
		// Вызываем SpawnEnemy через некоторое время после смерти врага
		Invoke("SpawnEnemy", 2f);
	}

	void SpawnCompanion()
	{
		Vector3 spawnPosition = GetRandomSpawnPosition();
		if (spawnPosition == Vector3.zero) return;

		if (companionPrefab != null)
		{
			GameObject companion = Instantiate(companionPrefab, spawnPosition, Quaternion.identity);
			CompanionHealth companionHealth = companion.GetComponent<CompanionHealth>();
			if (companionHealth != null)
			{
				companionHealth.OnDeath += HandleCompanionDeath;
			}
			activeCompanions.Add(companion);
		}
	}

	void HandleCompanionDeath(GameObject deadCompanion)
	{
		if (activeCompanions.Contains(deadCompanion))
		{
			activeCompanions.Remove(deadCompanion);
		}

		// Respawn a new companion after a delay
		Invoke("SpawnCompanion", 5f);
	}

	Vector3 GetRandomSpawnPosition()
	{
		if (terrain == null || playerTransform == null) return Vector3.zero;

		Vector3 spawnPosition = Vector3.zero;
		bool positionFound = false;
		int attempts = 0;
		const int maxAttempts = 30;

		while (!positionFound && attempts < maxAttempts)
		{
			attempts++;

			// Get random point within terrain bounds
			Vector3 terrainSize = terrain.terrainData.size;
			Vector3 terrainPos = terrain.transform.position;

			float randomX = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
			float randomZ = Random.Range(terrainPos.z, terrainPos.z + terrainSize.z);

			// Check distance from player
			Vector3 potentialPosition = new Vector3(randomX, 0, randomZ);
			float distanceToPlayer = Vector3.Distance(potentialPosition, playerTransform.position);

			if (distanceToPlayer < minSpawnDistanceFromPlayer || distanceToPlayer > maxSpawnDistanceFromPlayer)
				continue;

			// Get ground height
			float groundHeight = terrain.SampleHeight(potentialPosition) + terrain.transform.position.y;
			potentialPosition.y = groundHeight + spawnHeightAboveGround;

			// Check if position is valid (on navmesh)
			UnityEngine.AI.NavMeshHit hit;
			if (UnityEngine.AI.NavMesh.SamplePosition(potentialPosition, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
			{
				spawnPosition = hit.position;
				positionFound = true;
			}
		}

		return spawnPosition;
	}

	void Update()
	{
		if (!isInitialized) return;

		// Maintain enemy count
		int currentEnemyCount = FindObjectsOfType<EnemyAI>().Length;
		if (currentEnemyCount < totalEnemies)
		{
			SpawnEnemy();
		}

		// Maintain companion count
		if (activeCompanions.Count < totalCompanions)
		{
			SpawnCompanion();
		}
	}

	void OnDestroy()
	{
		// Clean up event subscriptions
		foreach (var companion in activeCompanions)
		{
			if (companion != null)
			{
				CompanionHealth companionHealth = companion.GetComponent<CompanionHealth>();
				if (companionHealth != null)
				{
					companionHealth.OnDeath -= HandleCompanionDeath;
				}
			}
		}
	}
}