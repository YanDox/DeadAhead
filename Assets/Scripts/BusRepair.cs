using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusRepair : MonoBehaviour
{
	public int requiredParts = 5; // ����� ����� �������
	public int installedParts = 0; // ����������� �������
	public float repairTime = 3f; // ����� ��������� ����� ������

	private float repairProgress = 0f;
	private bool isPlayerNear = false;
	private Inventory playerInventory;

	void Update()
	{
		if (isPlayerNear && Input.GetKey(KeyCode.Y) && installedParts < requiredParts)
		{
			// ���������, ���� �� � ������ ������ � ���������
			if (playerInventory != null && playerInventory.items[Inventory.BUS_PART] > 0)
			{
				repairProgress += Time.deltaTime;

				if (repairProgress >= repairTime)
				{
					repairProgress = 0f;
					playerInventory.UseItem(Inventory.BUS_PART);
					installedParts++;
					Debug.Log($"������ �����������! ��������: {requiredParts - installedParts}");

					// ���������, ��������� �� �������������� �������
					if (installedParts >= requiredParts)
					{
						Debug.Log("������� ��������� ��������������!");
						// ����� ����� �������� ������ ���������� �������
					}
				}
			}
			else
			{
				Debug.Log("� ��� ��� ������� ��� �������!");
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
			Debug.Log("��������� � �������� � ������� Y ��� �������");
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			isPlayerNear = false;
			repairProgress = 0f;
			Debug.Log("�� ����� �� ���� �������");
		}
	}
}