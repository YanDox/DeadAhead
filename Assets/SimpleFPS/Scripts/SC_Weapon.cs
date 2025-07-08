using UnityEngine;
using System.Collections; // Добавьте эту строку

[RequireComponent(typeof(AudioSource))]
public class SC_Weapon : MonoBehaviour
{
	public bool singleFire = false;
	public float fireRate = 0.1f;
	public GameObject bulletPrefab;
	public Transform firePoint;
	public int bulletsPerMagazine = 30;
	public float timeToReload = 1.5f;
	public float weaponDamage = 15; //How much damage should this weapon deal
	public AudioClip fireAudio;
	public AudioClip reloadAudio;

	[HideInInspector]
	public SC_WeaponManager manager;

	float nextFireTime = 0;
	bool canFire = true;
	int bulletsPerMagazineDefault = 0;
	AudioSource audioSource;

	// Start is called before the first frame update
	void Start()
	{
		bulletsPerMagazineDefault = bulletsPerMagazine;
		audioSource = GetComponent<AudioSource>();
		audioSource.playOnAwake = false;
		//Make sound 3D
		audioSource.spatialBlend = 1f;
	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButtonDown(0) && singleFire)
		{
			Fire();
		}
		if (Input.GetMouseButton(0) && !singleFire)
		{
			Fire();
		}
		if (Input.GetKeyDown(KeyCode.R) && canFire)
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

				if (bulletsPerMagazine > 0)
				{
					//Point fire point at the current center of Camera
					Vector3 firePointPointerPosition = manager.playerCamera.transform.position + manager.playerCamera.transform.forward * 100;
					RaycastHit hit;
					if (Physics.Raycast(manager.playerCamera.transform.position, manager.playerCamera.transform.forward, out hit, 100))
					{
						firePointPointerPosition = hit.point;
					}
					firePoint.LookAt(firePointPointerPosition);
					//Fire
					GameObject bulletObject = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
					SC_Bullet bullet = bulletObject.GetComponent<SC_Bullet>();
					//Set bullet damage according to weapon damage value
					bullet.SetDamage(weaponDamage);

					bulletsPerMagazine--;
					audioSource.clip = fireAudio;
					audioSource.Play();
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

		bulletsPerMagazine = bulletsPerMagazineDefault;

		canFire = true;
	}

	//Called from SC_WeaponManager
	public void ActivateWeapon(bool activate)
	{
		StopAllCoroutines();
		canFire = true;
		gameObject.SetActive(activate);
	}
}
