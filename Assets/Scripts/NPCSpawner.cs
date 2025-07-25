using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
	[Header("NPC Prefabs")]
	public GameObject psychoPrefab;
	public GameObject zombiePrefab;
	public GameObject zombieBossPrefab;
	public GameObject zombieCommanderPrefab;
	public GameObject zombieFatPrefab;
	public GameObject companionPrefab;

	[Header("Spawn Settings")]
	[Tooltip("Невидимые стены, ограничивающие зону спавна")]
	public List<Collider> invisibleWalls = new List<Collider>();
	public int maxNPCs = 20;
	public float spawnInterval = 5f;
	public float spawnCheckRadius = 2f;
	public LayerMask spawnCheckLayerMask;

	[Header("Water Settings")]
	public GameObject waterPlane;
	public float waterHeightOffset = 0.5f;

	private List<GameObject> spawnedNPCs = new List<GameObject>();
	private float spawnTimer;
	private Bounds spawnArea;
	private float waterHeight;
	private bool spawnAreaValid = true;

	void Start()
	{
		// Проверка необходимых компонентов
		if (invisibleWalls.Count < 4)
		{
			Debug.LogError("Необходимо назначить 4 невидимые стены!");
			spawnAreaValid = false;
			return;
		}

		if (waterPlane == null)
		{
			Debug.LogError("Water Plane не назначен!");
			spawnAreaValid = false;
			return;
		}

		CalculateSpawnArea();
		waterHeight = waterPlane.transform.position.y + waterHeightOffset;
		spawnTimer = spawnInterval;

		// Первоначальный спавн
		for (int i = 0; i < maxNPCs / 2; i++)
		{
			TrySpawnNPC();
		}
	}

	void Update()
	{
		if (!spawnAreaValid || spawnedNPCs.Count >= maxNPCs) return;

		spawnTimer -= Time.deltaTime;
		if (spawnTimer <= 0f)
		{
			spawnTimer = spawnInterval;
			TrySpawnNPC();
		}

		// Очистка списка от уничтоженных NPC
		spawnedNPCs.RemoveAll(npc => npc == null);
	}

	private void CalculateSpawnArea()
	{
		Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

		foreach (var wall in invisibleWalls)
		{
			if (wall == null) continue;

			min = Vector3.Min(min, wall.bounds.min);
			max = Vector3.Max(max, wall.bounds.max);
		}

		spawnArea = new Bounds((min + max) * 0.5f, max - min);
	}

	private void TrySpawnNPC()
	{
		Vector3 spawnPosition = GetRandomSpawnPosition();
		if (spawnPosition == Vector3.zero)
		{
			Debug.LogWarning("Не удалось найти позицию для спавна");
			return;
		}

		GameObject npcPrefab = GetRandomNPCPrefab();
		if (npcPrefab == null) return;

		GameObject npc = Instantiate(npcPrefab, spawnPosition, Quaternion.identity);
		spawnedNPCs.Add(npc);
	}

	private Vector3 GetRandomSpawnPosition()
	{
		for (int i = 0; i < 50; i++) // Увеличили количество попыток
		{
			Vector3 randomPoint = new Vector3(
				Random.Range(spawnArea.min.x, spawnArea.max.x),
				0,
				Random.Range(spawnArea.min.z, spawnArea.max.z)
			);

			// Проверка на NavMesh с увеличенным радиусом
			if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 20f, NavMesh.AllAreas))
			{
				// Проверка высоты с запасом
				if (hit.position.y > waterHeight + 0.1f)
				{
					// Проверка коллизий с уменьшенным радиусом
					if (!Physics.CheckSphere(hit.position, spawnCheckRadius * 0.8f, spawnCheckLayerMask))
					{
						// Дополнительная проверка под точкой
						if (!Physics.Raycast(hit.position + Vector3.up * 0.5f, Vector3.down, 1f, spawnCheckLayerMask))
						{
							return hit.position;
						}
					}
				}
			}
		}
		return Vector3.zero;
	}

	private GameObject GetRandomNPCPrefab()
	{
		float randomValue = Random.value;

		if (randomValue < 0.4f) return zombiePrefab;
		if (randomValue < 0.7f) return psychoPrefab;
		if (randomValue < 0.85f) return zombieFatPrefab;
		if (randomValue < 0.95f) return zombieCommanderPrefab;
		return zombieBossPrefab;
	}

	private void OnDrawGizmosSelected()
	{
		if (invisibleWalls.Count >= 4)
		{
			CalculateSpawnArea();
			Gizmos.color = new Color(0, 1, 0, 0.3f);
			Gizmos.DrawCube(spawnArea.center, spawnArea.size);
		}

		if (waterPlane != null)
		{
			Gizmos.color = new Color(0, 0, 1, 0.3f);
			Gizmos.DrawCube(
				new Vector3(spawnArea.center.x, waterPlane.transform.position.y, spawnArea.center.z),
				new Vector3(spawnArea.size.x, 0.1f, spawnArea.size.z)
			);
		}
	}
}