using UnityEngine;

public class SC_WeaponManager : MonoBehaviour
{
	public Camera playerCamera;
	public SC_Weapon primaryWeapon;
	public SC_Weapon secondaryWeapon;

	[HideInInspector]
	public SC_Weapon selectedWeapon;

	void Start()
	{
		InitializeWeapons();
		SelectWeapon(primaryWeapon);
	}

	void Update()
	{
		HandleWeaponSwitch();
	}

	void InitializeWeapons()
	{
		primaryWeapon.manager = this;
		secondaryWeapon.manager = this;
	}

	void HandleWeaponSwitch()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			SelectWeapon(primaryWeapon);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			SelectWeapon(secondaryWeapon);
		}
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