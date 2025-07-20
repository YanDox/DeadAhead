using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SC_Weapon : MonoBehaviour
{
	public bool singleFire = false;
	public float fireRate = 0.1f;
	public GameObject bulletPrefab;
	public Transform firePoint;
	public int magazineSize = 30;
	public int currentBulletsInMagazine = 0;
	public float timeToReload = 1.5f;
	public float weaponDamage = 15;
	public AudioClip fireAudio;
	public AudioClip reloadAudio;
	public int ammoType = Inventory.RIFLE_AMMO;
	public bool infiniteAmmo = false; // Добавлено поле для бесконечных патронов

	[HideInInspector]
	public SC_WeaponManager manager;

	float nextFireTime = 0;
	bool canFire = true;
	AudioSource audioSource;
	Inventory inventory;

	void Start()
	{
		audioSource = GetComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.spatialBlend = 1f;
		inventory = FindObjectOfType<Inventory>();
		currentBulletsInMagazine = magazineSize;
	}

	void Update()
	{
		bool canShoot = manager.cameraCollision.IsAiming || manager.isUltimateActive;

		if (canShoot)
		{
			if (Input.GetMouseButtonDown(0) && singleFire)
			{
				Fire();
			}
			if (Input.GetMouseButton(0) && !singleFire)
			{
				Fire();
			}
		}

		if (Input.GetKeyDown(KeyCode.R) && canFire && currentBulletsInMagazine < magazineSize && !infiniteAmmo)
		{
			StartCoroutine(Reload());
		}
	}

	public void Fire()
	{
		if (canFire)
		{
			if (Time.time > nextFireTime)
			{
				nextFireTime = Time.time + fireRate;

				if (currentBulletsInMagazine > 0 || infiniteAmmo)
				{
					Vector3 firePointPointerPosition = manager.playerCamera.transform.position + manager.playerCamera.transform.forward * 100;
					RaycastHit hit;
					if (Physics.Raycast(manager.playerCamera.transform.position, manager.playerCamera.transform.forward, out hit, 100))
					{
						firePointPointerPosition = hit.point;
					}
					firePoint.LookAt(firePointPointerPosition);

					GameObject bulletObject = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
					SC_Bullet bullet = bulletObject.GetComponent<SC_Bullet>();
					bullet.SetDamage(weaponDamage);

					if (!infiniteAmmo)
					{
						currentBulletsInMagazine--;
					}

					//audioSource.clip = fireAudio;
					//audioSource.Play();
				}
				else
				{
					StartCoroutine(Reload());
				}
			}
		}
	}

	IEnumerator Reload()
	{
		canFire = false;
		audioSource.clip = reloadAudio;
		audioSource.Play();
		yield return new WaitForSeconds(timeToReload);

		if (!infiniteAmmo && inventory != null && inventory.items[ammoType] > 0)
		{
			int bulletsNeeded = magazineSize - currentBulletsInMagazine;
			int bulletsToLoad = Mathf.Min(bulletsNeeded, inventory.items[ammoType]);

			currentBulletsInMagazine += bulletsToLoad;
			inventory.items[ammoType] -= bulletsToLoad;
		}

		canFire = true;
	}

	public void ActivateWeapon(bool activate)
	{
		StopAllCoroutines();
		canFire = true;
		gameObject.SetActive(activate);
	}
}