using UnityEngine;

public class Inventory : MonoBehaviour
{
	public int[] items = new int[5];
	public const int RIFLE_AMMO = 0;
	public const int AXE = 1;
	public const int BUS_PART = 2;
	public const int MEDKIT = 3;
	public const int COLA_CRATE = 4;

	[Header("Ultimate Settings")]
	public int ultimatePoints = 0;
	public int maxUltimate = 100;
	public float ultimateDuration = 10f;
	public bool isUltimateActive = false;

	public event System.Action<int> OnUltimateChanged;

	void Start()
	{
		items[RIFLE_AMMO] = 30;
		items[AXE] = 0;
		items[BUS_PART] = 0;
		items[MEDKIT] = 1;
		items[COLA_CRATE] = 0;
	}

	public void AddUltimatePoints(int amount)
	{
		if (!isUltimateActive)
		{
			ultimatePoints = Mathf.Min(ultimatePoints + amount, maxUltimate);
			OnUltimateChanged?.Invoke(ultimatePoints);
		}
	}

	public bool ActivateUltimate()
	{
		if (ultimatePoints >= maxUltimate && !isUltimateActive)
		{
			ultimatePoints = 0;
			isUltimateActive = true;
			OnUltimateChanged?.Invoke(ultimatePoints);
			return true;
		}
		return false;
	}

	public void DeactivateUltimate()
	{
		isUltimateActive = false;
	}

	public bool AddItem(int itemType, int amount = 1)
	{
		switch (itemType)
		{
			case RIFLE_AMMO:
				items[RIFLE_AMMO] += amount;
				return true;

			case AXE:
				if (items[AXE] + amount <= 1)
				{
					items[AXE] += amount;
					return true;
				}
				break;

			case BUS_PART:
				if (items[BUS_PART] == 0)
				{
					items[BUS_PART] = 1;
					return true;
				}
				break;

			case MEDKIT:
				items[MEDKIT] += amount;
				return true;
		
				case COLA_CRATE:
			if (items[COLA_CRATE] == 0)
			{
				items[COLA_CRATE] = 1;
				return true;
			}
			break;
		}
		return false;
	}


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