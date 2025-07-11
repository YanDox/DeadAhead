using UnityEngine;

public class SC_WeaponManager : MonoBehaviour
{
	public Camera playerCamera;
	public SC_Weapon primaryWeapon;
	public SC_Weapon secondaryWeapon;
	private Inventory playerInv;
	public bool isUltimateActive = false;
	public SC_CameraCollision cameraCollision; // Добавлена ссылка на SC_CameraCollision

	[HideInInspector]
	public SC_Weapon selectedWeapon;

	void Start()
	{
		playerInv = GetComponent<Inventory>();
		if (playerInv == null)
		{
			playerInv = FindObjectOfType<Inventory>();
		}

		// Получаем ссылку на SC_CameraCollision, если не установлена в инспекторе
		if (cameraCollision == null)
		{
			cameraCollision = FindObjectOfType<SC_CameraCollision>();
		}

		primaryWeapon.ActivateWeapon(true);
		secondaryWeapon.ActivateWeapon(false);
		selectedWeapon = primaryWeapon;
		primaryWeapon.manager = this;
		secondaryWeapon.manager = this;
	}

	void Update()
	{
		HandleWeaponSwitch();

		if (isUltimateActive && selectedWeapon == secondaryWeapon)
		{
			// Принудительно включаем прицеливание во время ульты
			if (cameraCollision != null && !cameraCollision.IsAiming)
			{
				cameraCollision.ForceAim(true);
			}
			secondaryWeapon.Fire();
		}
	}

	void HandleWeaponSwitch()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1) && !isUltimateActive)
		{
			SelectWeapon(primaryWeapon);
		}

		if (Input.GetKeyDown(KeyCode.U))
		{
			if (playerInv != null && playerInv.ActivateUltimate())
			{
				isUltimateActive = true;
				SelectWeapon(secondaryWeapon);
				Invoke(nameof(EndUltimate), playerInv.ultimateDuration);
			}
		}
	}

	void EndUltimate()
	{
		isUltimateActive = false;
		if (playerInv != null)
		{
			playerInv.DeactivateUltimate();
		}

		// Возвращаем прицеливание в нормальное состояние после ульты
		if (cameraCollision != null)
		{
			cameraCollision.ForceAim(false);
		}

		SelectWeapon(primaryWeapon);
	}

	void SelectWeapon(SC_Weapon weaponToSelect)
	{
		if (selectedWeapon != null)
		{
			selectedWeapon.ActivateWeapon(false);
		}

		weaponToSelect.ActivateWeapon(true);
		selectedWeapon = weaponToSelect;
	}
}