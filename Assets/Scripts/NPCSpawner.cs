using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
	[System.Serializable]
	public class SpawnGroup
	{
		public GameObject prefab;
		public int maxCount = 10;
		public float spawnRadius = 5f;
		public float minDistanceFromPlayer = 20f;
		public float spawnInterval = 10f;
		[HideInInspector] public List<GameObject> activeEntities = new List<GameObject>();
	}

	[Header("Spawn Settings")]
	public SpawnGroup[] enemyGroups;
	public SpawnGroup[] companionGroups;
	public LayerMask terrainLayer;
	public LayerMask obstacleLayer;
	public float spawnCheckHeight = 50f;
	public float spawnCheckDistance = 100f;

	private Transform player;
	private Terrain terrain;
	private float[] nextSpawnTimes;

	void Start()
	{
		player = GameObject.FindGameObjectWithTag("Player").transform;
		terrain = Terrain.activeTerrain;
		nextSpawnTimes = new float[enemyGroups.Length + companionGroups.Length];

		// Initial spawn
		for (int i = 0; i < enemyGroups.Length; i++)
		{
			SpawnInitialEntities(enemyGroups[i]);
			nextSpawnTimes[i] = Time.time + enemyGroups[i].spawnInterval;
		}

		for (int i = 0; i < companionGroups.Length; i++)
		{
			int index = enemyGroups.Length + i;
			SpawnInitialEntities(companionGroups[i]);
			nextSpawnTimes[index] = Time.time + companionGroups[i].spawnInterval;
		}
	}

	void Update()
	{
		// Check enemy spawns
		for (int i = 0; i < enemyGroups.Length; i++)
		{
			if (Time.time >= nextSpawnTimes[i] && enemyGroups[i].activeEntities.Count < enemyGroups[i].maxCount)
			{
				TrySpawnEntity(enemyGroups[i]);
				nextSpawnTimes[i] = Time.time + enemyGroups[i].spawnInterval;
			}
		}

		// Check companion spawns
		for (int i = 0; i < companionGroups.Length; i++)
		{
			int index = enemyGroups.Length + i;
			if (Time.time >= nextSpawnTimes[index] && companionGroups[i].activeEntities.Count < companionGroups[i].maxCount)
			{
				TrySpawnEntity(companionGroups[i]);
				nextSpawnTimes[index] = Time.time + companionGroups[i].spawnInterval;
			}
		}

		// Clean up null references
		CleanUpEntityLists();
	}

	private void SpawnInitialEntities(SpawnGroup group)
	{
		while (group.activeEntities.Count < group.maxCount)
		{
			TrySpawnEntity(group);
		}
	}

	private void TrySpawnEntity(SpawnGroup group)
	{
		Vector3 spawnPosition = FindSpawnPosition(group);
		if (spawnPosition != Vector3.zero)
		{
			GameObject entity = Instantiate(group.prefab, spawnPosition, Quaternion.identity);
			group.activeEntities.Add(entity);

			// Setup death event to remove from list
			var health = entity.GetComponent<EnemyHealth>();
			if (health != null)
			{
				health.OnDeath += () => group.activeEntities.Remove(entity);
			}
			else
			{
				var companionHealth = entity.GetComponent<CompanionHealth>();
			
			}
		}
	}

	private Vector3 FindSpawnPosition(SpawnGroup group)
	{
		for (int i = 0; i < 30; i++) // Try 30 times to find a valid position
		{
			// Get random point on terrain
			Vector3 randomPoint = GetRandomTerrainPosition();

			// Check distance from player
			if (Vector3.Distance(randomPoint, player.position) < group.minDistanceFromPlayer)
				continue;

			// Check if position is valid (not inside objects)
			if (!Physics.CheckSphere(randomPoint, group.spawnRadius, obstacleLayer))
			{
				// Check if position is on terrain
				if (IsPositionOnTerrain(randomPoint))
				{
					return randomPoint;
				}
			}
		}

		return Vector3.zero; // Failed to find position
	}

	private Vector3 GetRandomTerrainPosition()
	{
		if (terrain != null)
		{
			Vector3 terrainSize = terrain.terrainData.size;
			Vector3 terrainPos = terrain.transform.position;

			float x = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
			float z = Random.Range(terrainPos.z, terrainPos.z + terrainSize.z);
			float y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrainPos.y;

			return new Vector3(x, y, z);
		}
		else
		{
			// Fallback if no terrain - spawn in a large flat area
			return new Vector3(
				Random.Range(-100, 100),
				0,
				Random.Range(-100, 100)
			);
		}
	}

	private bool IsPositionOnTerrain(Vector3 position)
	{
		if (terrain == null) return true;

		Vector3 terrainPos = terrain.transform.position;
		Vector3 terrainSize = terrain.terrainData.size;

		return position.x >= terrainPos.x &&
			   position.x <= terrainPos.x + terrainSize.x &&
			   position.z >= terrainPos.z &&
			   position.z <= terrainPos.z + terrainSize.z;
	}

	private void CleanUpEntityLists()
	{
		foreach (var group in enemyGroups)
		{
			group.activeEntities.RemoveAll(item => item == null);
		}

		foreach (var group in companionGroups)
		{
			group.activeEntities.RemoveAll(item => item == null);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		foreach (var group in enemyGroups)
		{
			foreach (var entity in group.activeEntities)
			{
				if (entity != null)
				{
					Gizmos.DrawWireSphere(entity.transform.position, 1f);
				}
			}
		}

		Gizmos.color = Color.blue;
		foreach (var group in companionGroups)
		{
			foreach (var entity in group.activeEntities)
			{
				if (entity != null)
				{
					Gizmos.DrawWireSphere(entity.transform.position, 1f);
				}
			}
		}
	}
}