using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColaQuestDoor : MonoBehaviour
{
	public GameObject busPartPrefab; // Префаб детали автобуса для выдачи
	public Transform dropPoint; // Точка, куда выкидывать деталь
	public string questMessage = "Принесите мне ящик колы!";
	public GameObject Ui;
	public Text text;
	public TMP_Text colaText;

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			Inventory inventory = other.GetComponent<Inventory>();
			if (inventory != null)
			{
				Ui.active = true;
				// Проверяем есть ли у игрока ящик колы
				if (inventory.UseItem(CrateOfCola.COLA_CRATE))
				{
					// Выдаем награду - деталь автобуса
					if (busPartPrefab != null && dropPoint != null)
					{
						Instantiate(busPartPrefab, dropPoint.position, dropPoint.rotation);
						Debug.Log("Деталь автобуса выброшена из двери!");
						text.text = ("Деталь автобуса выброшена из двери!");
						
					}

					
				}
				else
				{
					text.text = questMessage;
					Debug.Log(questMessage);
					
				}
			}
		}
	}
	private void OnTriggerExit(Collider other)
	{
		Ui.active = false;
	}
}