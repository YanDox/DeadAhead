using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
	public int[] items = new int[4]; // 4 ячейки инвентаря

	// Индексы ячеек
	public const int RIFLE_AMMO = 0;
	public const int AXE = 1;
	public const int BUS_PART = 2;
	public const int MEDKIT = 3;

	void Start()
	{
		// Инициализация инвентаря
		items[RIFLE_AMMO] = 30; // Начальное количество патронов
		items[AXE] = 0;         // Топора нет
		items[BUS_PART] = 0;    // Детали нет
		items[MEDKIT] = 1;      // 1 аптечка
	}

	// Добавление предмета в инвентарь
	public bool AddItem(int itemType, int amount = 1)
	{
		switch (itemType)
		{
			case RIFLE_AMMO:
				items[RIFLE_AMMO] += amount;
				return true;

			case AXE:
				if (items[AXE] + amount <= 1) // Максимум 1 топор
				{
					items[AXE] += amount;
					return true;
				}
				break;

			case BUS_PART:
				if (items[BUS_PART] == 0) // Можно нести только 1 деталь
				{
					items[BUS_PART] = 1;
					return true;
				}
				break;

			case MEDKIT:
				items[MEDKIT] += amount;
				return true;
		}
		return false;
	}

	// Использование предмета
	public bool UseItem(int itemType, int amount = 1)
	{
		if (items[itemType] >= amount)
		{
			items[itemType] -= amount;
			return true;
		}
		return false;
	}
}