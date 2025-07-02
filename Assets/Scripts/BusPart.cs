using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusPart : MonoBehaviour
{
	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Inventory Inventory = other.GetComponent<Inventory>();
			if (Inventory != null && Inventory.AddItem(Inventory.BUS_PART))
			{
				Destroy(gameObject);
				Debug.Log("Деталь автобуса подобрана!");
			}
		}
	}
}
