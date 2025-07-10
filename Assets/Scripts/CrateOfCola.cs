using UnityEngine;

public class CrateOfCola : MonoBehaviour
{
	public const int COLA_CRATE = 4; // ƒобавл€ем новый тип предмета в инвентарь

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Inventory inventory = other.GetComponent<Inventory>();
			if (inventory != null && inventory.AddItem(COLA_CRATE))
			{
				Destroy(gameObject);
				Debug.Log("ящик колы добавлен в инвентарь!");
			}
		}
	}
}