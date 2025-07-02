using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusRepair : MonoBehaviour
{
	public int requiredParts = 5; // Всего нужно деталей
	public int installedParts = 0; // Установлено деталей
	public float repairTime = 3f; // Время установки одной детали

	private float repairProgress = 0f;
	private bool isPlayerNear = false;
	private Inventory playerInventory;

	void Update()
	{
		if (isPlayerNear && Input.GetKey(KeyCode.Y) && installedParts < requiredParts)
		{
			// Проверяем, есть ли у игрока деталь в инвентаре
			if (playerInventory != null && playerInventory.items[Inventory.BUS_PART] > 0)
			{
				repairProgress += Time.deltaTime;

				if (repairProgress >= repairTime)
				{
					repairProgress = 0f;
					playerInventory.UseItem(Inventory.BUS_PART);
					installedParts++;
					Debug.Log($"Деталь установлена! Осталось: {requiredParts - installedParts}");

					// Проверяем, полностью ли отремонтирован автобус
					if (installedParts >= requiredParts)
					{
						Debug.Log("Автобус полностью отремонтирован!");
						// Здесь можно добавить логику завершения ремонта
					}
				}
			}
			else
			{
				Debug.Log("У вас нет деталей для ремонта!");
				repairProgress = 0f;
			}
		}
		else
		{
			repairProgress = 0f;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			isPlayerNear = true;
			playerInventory = other.GetComponent<Inventory>();
			Debug.Log("Подойдите к автобусу и нажмите Y для ремонта");
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			isPlayerNear = false;
			repairProgress = 0f;
			Debug.Log("Вы вышли из зоны ремонта");
		}
	}
}