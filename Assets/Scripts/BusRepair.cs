using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusRepair : MonoBehaviour
{
	public int requiredParts = 5; // Всего нужно деталей
	public int installedParts = 0; // Установлено деталей
	public float repairTime = 5f; // Время установки одной детали

	private float repairProgress = 0f;
	private bool isPlayerNear = false;
    private bool isCompanionNear = false;
    private Inventory playerInventory;
    private CompanionInventory companionInventory;

    void Update()
    {
        // Ремонт игроком
        if (isPlayerNear && Input.GetKey(KeyCode.Y)
        {
            TryRepair(playerInventory);
        }
        // Ремонт компаньоном
        else if (isCompanionNear)
        {
            TryRepair(companionInventory);
        }
        else
        {
            repairProgress = 0f;
        }
    }

    private void TryRepair(Inventory inventory)
    {
        if (installedParts < requiredParts && inventory != null && inventory.items[Inventory.BUS_PART] > 0)
        {
            repairProgress += Time.deltaTime;
            if (repairProgress >= repairTime)
            {
                repairProgress = 0f;
                inventory.UseItem(Inventory.BUS_PART);
                installedParts++;
                if (installedParts >= requiredParts)
                {
                    Debug.Log("Автобус отремонтирован!");
                }
            }
        }
    }

    private void TryRepair(CompanionInventory inventory)
    {
        if (installedParts < requiredParts && inventory != null && inventory.items[CompanionInventory.BUS_PART] > 0)
        {
            repairProgress += Time.deltaTime;
            if (repairProgress >= repairTime)
            {
                repairProgress = 0f;
                inventory.UseItem(CompanionInventory.BUS_PART);
                installedParts++;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            playerInventory = other.GetComponent<Inventory>();
        }
        else if (other.CompareTag("Companion"))
        {
            isCompanionNear = true;
            companionInventory = other.GetComponent<CompanionInventory>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            repairProgress = 0f;
        }
        else if (other.CompareTag("Companion"))
        {
            isCompanionNear = false;
            repairProgress = 0f;
        }
    }
}