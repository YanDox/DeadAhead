using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
	[System.Serializable]
	public class SpawnGroup
	{
		public GameObject npcPrefab;
		public int spawnCount = 5;
		public float spawnRadius = 20f;
		public float minSpawnDistanceFromPlayer = 30f;
		public float spawnDelay = 0f;
		public bool spawnOnStart = true;
	}

	[Header("�������� ���������")]
	public Terrain terrain;
	public Transform playerTransform;
	public List<SpawnGroup> spawnGroups = new List<SpawnGroup>();

	[Header("��������� ����")]
	public bool useWaves = false;
	public float waveInterval = 60f;
	public int maxTotalNPCs = 50;
	public int npcsPerWaveIncrease = 5;

	private float nextWaveTime;
	private int currentWave = 0;
	private List<GameObject> activeNPCs = new List<GameObject>();

	void Start()
	{
		if (terrain == null)
		{
			terrain = Terrain.activeTerrain;
			if (terrain == null)
			{
				Debug.LogError("�� ������ ������� ��� ������ NPC!");
				enabled = false;
				return;
			}
		}

		if (playerTransform == null)
		{
			var player = GameObject.FindGameObjectWithTag("Player");
			if (player != null) playerTransform = player.transform;
		}

		if (useWaves)
		{
			nextWaveTime = Time.time + waveInterval;
		}
		else
		{
			SpawnInitialNPCs();
		}
	}

	void Update()
	{
		if (useWaves && Time.time >= nextWaveTime)
		{
			SpawnWave();
			nextWaveTime = Time.time + waveInterval;
		}

		// ������� ������������ NPC �� ������
		activeNPCs.RemoveAll(npc => npc == null);
	}

	private void SpawnInitialNPCs()
	{
		foreach (var group in spawnGroups)
		{
			if (group.spawnOnStart)
			{
				if (group.spawnDelay > 0)
				{
					StartCoroutine(SpawnWithDelay(group));
				}
				else
				{
					SpawnNPCGroup(group);
				}
			}
		}
	}

	private void SpawnWave()
	{
		currentWave++;
		Debug.Log($"������ ����� {currentWave}");

		// ����������� ���������� NPC � ������ ������
		foreach (var group in spawnGroups)
		{
			var modifiedGroup = new SpawnGroup()
			{
				npcPrefab = group.npcPrefab,
				spawnCount = group.spawnCount + (currentWave * npcsPerWaveIncrease),
				spawnRadius = group.spawnRadius,
				minSpawnDistanceFromPlayer = group.minSpawnDistanceFromPlayer,
				spawnDelay = group.spawnDelay
			};

			if (modifiedGroup.spawnDelay > 0)
			{
				StartCoroutine(SpawnWithDelay(modifiedGroup));
			}
			else
			{
				SpawnNPCGroup(modifiedGroup);
			}
		}
	}

	private System.Collections.IEnumerator SpawnWithDelay(SpawnGroup group)
	{
		yield return new WaitForSeconds(group.spawnDelay);
		SpawnNPCGroup(group);
	}

	private void SpawnNPCGroup(SpawnGroup group)
	{
		if (group.npcPrefab == null)
		{
			Debug.LogWarning("������� ���������� NPC ��� �������!");
			return;
		}

		if (activeNPCs.Count >= maxTotalNPCs && maxTotalNPCs > 0)
		{
			Debug.Log("���������� ������������ ���������� NPC, ����� ����� �������");
			return;
		}

		for (int i = 0; i < group.spawnCount; i++)
		{
			if (activeNPCs.Count >= maxTotalNPCs && maxTotalNPCs > 0) break;

			Vector3 spawnPos = FindSpawnPosition(group.minSpawnDistanceFromPlayer, group.spawnRadius);
			if (spawnPos != Vector3.zero)
			{
				GameObject npc = Instantiate(group.npcPrefab, spawnPos, Quaternion.identity);
				activeNPCs.Add(npc);
				Debug.Log($"��������� {group.npcPrefab.name} � ������� {spawnPos}");
			}
		}
	}

	private Vector3 FindSpawnPosition(float minDistanceFromPlayer, float spawnRadius)
	{
		if (playerTransform == null) return Vector3.zero;

		Vector3 spawnPos = Vector3.zero;
		bool positionFound = false;
		int attempts = 0;
		int maxAttempts = 30;

		while (!positionFound && attempts < maxAttempts)
		{
			attempts++;

			// ���������� ��������� ����� � �������� ��������
			Vector3 terrainSize = terrain.terrainData.size;
			Vector3 terrainPos = terrain.transform.position;

			float randomX = Random.Range(0, terrainSize.x);
			float randomZ = Random.Range(0, terrainSize.z);
			spawnPos = terrainPos + new Vector3(randomX, 0, randomZ);

			// �������� ������ � ���� �����
			spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;

			// ��������� ���������� �� ������
			float distanceToPlayer = Vector3.Distance(spawnPos, playerTransform.position);
			if (distanceToPlayer < minDistanceFromPlayer)
			{
				continue;
			}

			// ���������, ��� ����� �������� �� NavMesh
			UnityEngine.AI.NavMeshHit hit;
			if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, spawnRadius, UnityEngine.AI.NavMesh.AllAreas))
			{
				spawnPos = hit.position;
				positionFound = true;
			}
		}

		return positionFound ? spawnPos : Vector3.zero;
	}

	public void ClearAllNPCs()
	{
		foreach (var npc in activeNPCs)
		{
			if (npc != null) Destroy(npc);
		}
		activeNPCs.Clear();
	}

	private void OnDrawGizmosSelected()
	{
		if (terrain == null) return;

		Gizmos.color = Color.green;
		Vector3 size = terrain.terrainData.size;
		Vector3 center = terrain.transform.position + size / 2;
		Gizmos.DrawWireCube(center, size);
	}
}