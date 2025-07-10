using UnityEngine;

public class ColaQuestDoor : MonoBehaviour
{
	public GameObject busPartPrefab; // Префаб детали автобуса для выдачи
	public Transform dropPoint; // Точка, куда выкидывать деталь
	public string questMessage = "Принесите мне ящик колы!";

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Inventory inventory = other.GetComponent<Inventory>();
			if (inventory != null)
			{
				// Проверяем есть ли у игрока ящик колы
				if (inventory.UseItem(CrateOfCola.COLA_CRATE))
				{
					// Выдаем награду - деталь автобуса
					if (busPartPrefab != null && dropPoint != null)
					{
						Instantiate(busPartPrefab, dropPoint.position, dropPoint.rotation);
						Debug.Log("Деталь автобуса выброшена из двери!");
					}

					// Можно добавить анимацию открытия двери и т.д.
				}
				else
				{
					// Показываем сообщение о квесте
					Debug.Log(questMessage);
					// Здесь можно добавить UI-сообщение для игрока
				}
			}
		}
	}
}