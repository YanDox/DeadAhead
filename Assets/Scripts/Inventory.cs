using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
	public int[] items = new int[4]; // 4 ������ ���������

	// ������� �����
	public const int RIFLE_AMMO = 0;
	public const int AXE = 1;
	public const int BUS_PART = 2;
	public const int MEDKIT = 3;

	void Start()
	{
		// ������������� ���������
		items[RIFLE_AMMO] = 30; // ��������� ���������� ��������
		items[AXE] = 0;         // ������ ���
		items[BUS_PART] = 0;    // ������ ���
		items[MEDKIT] = 1;      // 1 �������
	}

	// ���������� �������� � ���������
	public bool AddItem(int itemType, int amount = 1)
	{
		switch (itemType)
		{
			case RIFLE_AMMO:
				items[RIFLE_AMMO] += amount;
				return true;

			case AXE:
				if (items[AXE] + amount <= 1) // �������� 1 �����
				{
					items[AXE] += amount;
					return true;
				}
				break;

			case BUS_PART:
				if (items[BUS_PART] == 0) // ����� ����� ������ 1 ������
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

	// ������������� ��������
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