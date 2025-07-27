using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class NPCSpawner : MonoBehaviour
{
	[Header("Зомби")]
	public GameObject[] commonZombieGroups;  // Обычные зомби (70%)
	public GameObject[] specialZombieGroups; // Особые зомби (20%)

	[Header("Особые враги")]
	public GameObject[] psychoGroups;       // Психи (10%)
	public GameObject[] bossGroups;        // Боссы (спавнятся отдельно)

	[Header("Компаньоны")]
	public GameObject[] companionPrefabs;   // Префабы компаньонов
	public float companionSpawnChance = 0.2f;
	public int maxCompanions = 3;

	[Header("Настройки спавна")]
	public List<Collider> invisibleWalls = new List<Collider>();
	public int maxGroups = 15;
	public float spawnInterval = 15f;
	public float spawnCheckRadius = 5f;
	public LayerMask spawnCheckLayerMask;
	public float activationDistance = 30f;

	[Header("Вода")]
	public GameObject waterPlane;
	public float waterHeightOffset = 0.5f;

	// Приватные переменные
	private List<GameObject> spawnedGroups = new List<GameObject>();
	private List<GameObject> spawnedCompanions = new List<GameObject>();
	private float spawnTimer;
	private Bounds spawnArea;
	private float waterHeight;
	private Transform player;
	private bool spawnAreaValid = true;

	void Start()
	{
		FindPlayer();
		InitializeSpawnArea();
		InitialSpawn();
	}

	void Update()
	{
		if (!spawnAreaValid) return;

		HandleGroupSpawning();
		HandleCompanionSpawning();
		UpdateGroupsActivity();
	}

	#region Инициализация
	private void FindPlayer()
	{
		player = GameObject.FindGameObjectWithTag("Player")?.transform;
		if (player == null)
		{
			Debug.LogError("Player not found! Add 'Player' tag to player object.");
			spawnAreaValid = false;
		}
	}

	private void InitializeSpawnArea()
	{
		if (invisibleWalls.Count < 4)
		{
			Debug.LogError("Need 4 invisible walls!");
			spawnAreaValid = false;
			return;
		}

		if (waterPlane == null)
		{
			Debug.LogError("Water Plane not assigned!");
			spawnAreaValid = false;
			return;
		}

		CalculateSpawnArea();
		waterHeight = waterPlane.transform.position.y + waterHeightOffset;
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

	private void InitialSpawn()
	{
		spawnTimer = 0f;
		for (int i = 0; i < Mathf.Min(maxGroups / 2, 5); i++)
		{
			TrySpawnGroup();
		}
	}
	#endregion

	#region Спавн
	private void HandleGroupSpawning()
	{
		if (spawnedGroups.Count >= maxGroups) return;

		spawnTimer -= Time.deltaTime;
		if (spawnTimer <= 0f)
		{
			spawnTimer = spawnInterval;
			TrySpawnGroup();
		}
	}

	private void HandleCompanionSpawning()
	{
		if (spawnedCompanions.Count >= maxCompanions) return;

		if (Random.value < companionSpawnChance)
		{
			TrySpawnCompanion();
		}
	}

	private void TrySpawnGroup()
	{
		Vector3 spawnPosition = FindValidSpawnPosition();
		if (spawnPosition == Vector3.zero) return;

		GameObject groupPrefab = SelectRandomGroupPrefab();
		if (groupPrefab == null) return;

		GameObject group = Instantiate(groupPrefab, spawnPosition, Quaternion.identity);
		spawnedGroups.Add(group);
		InitializeGroup(group);
		SetGroupActive(group, ShouldBeActive(spawnPosition));
	}

	private void TrySpawnCompanion()
	{
		Vector3 spawnPosition = FindValidSpawnPosition();
		if (spawnPosition == Vector3.zero) return;

		GameObject companionPrefab = companionPrefabs[Random.Range(0, companionPrefabs.Length)];
		GameObject companion = Instantiate(companionPrefab, spawnPosition, Quaternion.identity);
		spawnedCompanions.Add(companion);

		CompanionAI companionAI = companion.GetComponent<CompanionAI>();
		if (companionAI != null)
		{
			companionAI.Initialize(player);
		}
	}

	private GameObject SelectRandomGroupPrefab()
	{
		float randomValue = Random.value;

		if (randomValue < 0.7f && commonZombieGroups.Length > 0)
			return commonZombieGroups[Random.Range(0, commonZombieGroups.Length)];

		if (randomValue < 0.9f && specialZombieGroups.Length > 0)
			return specialZombieGroups[Random.Range(0, specialZombieGroups.Length)];

		if (psychoGroups.Length > 0)
			return psychoGroups[Random.Range(0, psychoGroups.Length)];

		return null;
	}

	private Vector3 FindValidSpawnPosition()
	{
		for (int i = 0; i < 50; i++)
		{
			Vector3 randomPoint = new Vector3(
				Random.Range(spawnArea.min.x, spawnArea.max.x),
				0,
				Random.Range(spawnArea.min.z, spawnArea.max.z)
			);

			if (IsValidSpawnPoint(randomPoint, out Vector3 validPoint))
			{
				return validPoint;
			}
		}
		return Vector3.zero;
	}

	private bool IsValidSpawnPoint(Vector3 point, out Vector3 validPoint)
	{
		validPoint = Vector3.zero;

		if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 20f, NavMesh.AllAreas))
			return false;

		if (hit.position.y <= waterHeight + 0.1f)
			return false;

		if (Physics.CheckSphere(hit.position, spawnCheckRadius, spawnCheckLayerMask))
			return false;

		validPoint = hit.position;
		return true;
	}
	#endregion

	#region Управление активностью
	private void UpdateGroupsActivity()
	{
		UpdateSpawnedObjectsActivity(spawnedGroups);
		UpdateSpawnedObjectsActivity(spawnedCompanions, true);

		// Дополнительная очистка мертвых объектов
		CleanupDeadObjects(spawnedGroups);
		CleanupDeadObjects(spawnedCompanions);
	}

	private void CleanupDeadObjects(List<GameObject> objects)
	{
		for (int i = objects.Count - 1; i >= 0; i--)
		{
			if (objects[i] == null ||
				(objects[i].CompareTag("Dead") &&
				 Vector3.Distance(objects[i].transform.position, player.position) > activationDistance))
			{
				if (objects[i] != null)
				{
					Destroy(objects[i]);
				}
				objects.RemoveAt(i);
			}
		}
	}

	private void UpdateSpawnedObjectsActivity(List<GameObject> objects, bool alwaysActive = false)
	{
		for (int i = objects.Count - 1; i >= 0; i--)
		{
			if (objects[i] == null)
			{
				objects.RemoveAt(i);
				continue;
			}

			// Удаляем объекты с тегом "Dead" вне радиуса активации
			if (objects[i].CompareTag("Dead") &&
				Vector3.Distance(objects[i].transform.position, player.position) > activationDistance)
			{
				Destroy(objects[i]);
				objects.RemoveAt(i);
				continue;
			}

			bool shouldBeActive = alwaysActive || ShouldBeActive(objects[i].transform.position);
			if (objects[i].activeSelf != shouldBeActive)
			{
				SetGroupActive(objects[i], shouldBeActive);
			}
		}
	}

	private bool ShouldBeActive(Vector3 position)
	{
		return Vector3.Distance(position, player.position) <= activationDistance;
	}

	private void InitializeGroup(GameObject group)
	{
		foreach (Transform npc in group.transform)
		{
			if (npc == null) continue;

			InitializeNPC(npc.gameObject);
		}
	}

	private void InitializeNPC(GameObject npc)
	{
		npc.SetActive(false);

		var agent = npc.GetComponent<NavMeshAgent>();
		if (agent != null)
		{
			agent.enabled = false;
			agent.Warp(npc.transform.position);
		}

		var anim = npc.GetComponent<Animator>();
		if (anim != null) anim.enabled = false;

		var collider = npc.GetComponent<Collider>();
		if (collider != null) collider.enabled = false;
	}

	private void SetGroupActive(GameObject group, bool active)
	{
		if (group == null) return;

		group.SetActive(active);

		foreach (Transform npc in group.transform)
		{
			if (npc == null) continue;

			SetNPCActive(npc.gameObject, active);
		}
	}

	private void SetNPCActive(GameObject npc, bool active)
	{
		npc.SetActive(active);

		var agent = npc.GetComponent<NavMeshAgent>();
		if (agent != null)
		{
			agent.enabled = active;
			if (active)
			{
				agent.Warp(npc.transform.position);
				agent.isStopped = false;
			}
		}

		var anim = npc.GetComponent<Animator>();
		if (anim != null) anim.enabled = active;

		var collider = npc.GetComponent<Collider>();
		if (collider != null) collider.enabled = active;

		var ai = npc.GetComponent<EnemyAI>();
		if (ai != null) ai.enabled = active;
	}
	#endregion

	#region Визуализация
	private void OnDrawGizmosSelected()
	{
		DrawSpawnArea();
		DrawWaterLevel();
		DrawActivationZone();
	}

	private void DrawSpawnArea()
	{
		if (invisibleWalls.Count < 4) return;

		CalculateSpawnArea();
		Gizmos.color = new Color(0, 1, 0, 0.3f);
		Gizmos.DrawCube(spawnArea.center, spawnArea.size);
	}

	private void DrawWaterLevel()
	{
		if (waterPlane == null) return;

		Gizmos.color = new Color(0, 0, 1, 0.3f);
		Gizmos.DrawCube(
			new Vector3(spawnArea.center.x, waterPlane.transform.position.y, spawnArea.center.z),
			new Vector3(spawnArea.size.x, 0.1f, spawnArea.size.z)
		);
	}

	private void DrawActivationZone()
	{
		if (player == null) return;

		Gizmos.color = new Color(1, 0, 0, 0.2f);
		Gizmos.DrawSphere(player.position, activationDistance);
	}
	#endregion
}