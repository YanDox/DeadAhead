using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SC_Weapon : MonoBehaviour
{
	public bool singleFire = false;
	public float fireRate = 0.1f;
	public GameObject bulletPrefab;
	public Transform firePoint;
	public int bulletsPerMagazine = 30;
	public float timeToReload = 1.5f;
	public float weaponDamage = 15;
	public AudioClip fireAudio;
	public AudioClip reloadAudio;

	[HideInInspector]
	public SC_WeaponManager manager;

	private float nextFireTime = 0;
	private bool canFire = true;
	private int bulletsPerMagazineDefault = 0;
	private AudioSource audioSource;
	private SC_CameraCollision cameraCollision;
	private SC_TPSController playerController;

	void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.spatialBlend = 1f;
	}

	void Start()
	{
		bulletsPerMagazineDefault = bulletsPerMagazine;
		InitializeComponents();
	}

	void InitializeComponents()
	{
		if (manager != null && manager.playerCamera != null)
		{
			cameraCollision = manager.playerCamera.GetComponent<SC_CameraCollision>();
			playerController = manager.playerCamera.GetComponentInParent<SC_TPSController>();
		}
	}

	void Update()
	{
		if (cameraCollision == null || playerController == null) return;

		bool canShoot = !playerController.IsInCover || CheckPeekShooting();

		if (cameraCollision.IsAiming && canShoot)
		{
			HandleShooting();
		}

		if (Input.GetKeyDown(KeyCode.R) && canFire)
		{
			StartCoroutine(Reload());
		}
	}

	bool CheckPeekShooting()
	{
		return (Input.GetKey(KeyCode.A) && playerController.CanPeekLeft) ||
			   (Input.GetKey(KeyCode.D) && playerController.CanPeekRight);
	}

	void HandleShooting()
	{
		if (Input.GetMouseButtonDown(0) && singleFire)
		{
			TryFire();
		}
		if (Input.GetMouseButton(0) && !singleFire)
		{
			TryFire();
		}
	}

	void TryFire()
	{
		if (canFire && Time.time > nextFireTime)
		{
			nextFireTime = Time.time + fireRate;

			if (bulletsPerMagazine > 0)
			{
				FireBullet();
			}
			else
			{
				StartCoroutine(Reload());
			}
		}
	}

	void FireBullet()
	{
		Ray ray = manager.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 100) ? hit.point : ray.GetPoint(100);

		firePoint.LookAt(targetPoint);
		GameObject bulletObject = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
		bulletObject.GetComponent<SC_Bullet>().SetDamage(weaponDamage);

		bulletsPerMagazine--;
		audioSource.PlayOneShot(fireAudio);
	}

	IEnumerator Reload()
	{
		canFire = false;
		audioSource.PlayOneShot(reloadAudio);
		yield return new WaitForSeconds(timeToReload);
		bulletsPerMagazine = bulletsPerMagazineDefault;
		canFire = true;
	}

	public void ActivateWeapon(bool activate)
	{
		StopAllCoroutines();
		canFire = true;
		gameObject.SetActive(activate);
	}
}