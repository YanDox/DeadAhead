using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ammoCrate : MonoBehaviour
{
	public const int AMMO_AMOUNT = 30; // Количество добавляемых патронов

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Inventory inventory = other.GetComponent<Inventory>();
			if (inventory != null && inventory.AddItem(Inventory.RIFLE_AMMO, AMMO_AMOUNT))
			{
				Destroy(gameObject);
			}
		}
	}
}
