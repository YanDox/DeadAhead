using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoCrate : MonoBehaviour
{
	public int ammoType = Inventory.RIFLE_AMMO;
	public int ammoAmount = 30;
	public AudioClip pickupSound;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Inventory inventory = other.GetComponent<Inventory>();
			if (inventory != null)
			{
				if (inventory.AddItem(ammoType, ammoAmount))
				{
					AudioSource.PlayClipAtPoint(pickupSound, transform.position);
					Destroy(gameObject);
				}
			}
		}
	}
}
