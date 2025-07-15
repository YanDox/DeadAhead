using UnityEngine;

public class SC_WeaponManager : MonoBehaviour
{
	public Camera playerCamera;
	public SC_Weapon primaryWeapon;
	public SC_Weapon secondaryWeapon;
	private Inventory playerInv;
	public bool isUltimateActive = false;
	public SC_CameraCollision cameraCollision;

	[HideInInspector]
	public SC_Weapon selectedWeapon;

	void Start()
	{
		playerInv = GetComponent<Inventory>();
		if (playerInv == null)
		{
			playerInv = FindObjectOfType<Inventory>();
		}

		if (cameraCollision == null)
		{
			cameraCollision = FindObjectOfType<SC_CameraCollision>();
		}

		// Настройка оружия
		primaryWeapon.infiniteAmmo = false; // Основное оружие с ограниченными патронами
		secondaryWeapon.infiniteAmmo = true; // Второе оружие с бесконечными патронами

		primaryWeapon.ActivateWeapon(true);
		secondaryWeapon.ActivateWeapon(false);
		selectedWeapon = primaryWeapon;
		primaryWeapon.manager = this;
		secondaryWeapon.manager = this;
	}

	// Остальной код без изменений
	void Update()
	{
		HandleWeaponSwitch();

		if (isUltimateActive && selectedWeapon == secondaryWeapon)
		{
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

		if (Input.GetKeyDown(KeyCode.Alpha2) && !isUltimateActive)
		{
			SelectWeapon(secondaryWeapon);
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