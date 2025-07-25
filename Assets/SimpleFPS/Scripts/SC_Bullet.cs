using UnityEngine;

public class SC_Bullet : MonoBehaviour
{
	public float bulletSpeed = 345;
	public float hitForce = 50f;
	public float destroyAfter = 3.5f;
	public GameObject impactEffect;

	private float damagePoints;
	private Vector3 previousPosition;

	void Start()
	{
		previousPosition = transform.position;
		Destroy(gameObject, destroyAfter);
	}

	void FixedUpdate()
	{
		Vector3 movement = transform.forward * bulletSpeed * Time.fixedDeltaTime;
		transform.position += movement;

		if (Physics.Linecast(previousPosition, transform.position, out RaycastHit hit))
		{
			HandleHit(hit);
			Destroy(gameObject);
		}

		previousPosition = transform.position;
	}

	void HandleHit(RaycastHit hit)
	{
		// Применяем физическое воздействие
		if (hit.rigidbody != null)
		{
			hit.rigidbody.AddForce(transform.forward * hitForce);
		}

		// Создаем эффект попадания
		if (impactEffect != null)
		{
			Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
		}

		// Обрабатываем попадание в сущность
		IEntity entity = hit.transform.GetComponent<IEntity>();
		if (entity != null)
		{
			// Для EnemyHealth используем специальную обработку
			EnemyHealth enemyHealth = hit.transform.GetComponent<EnemyHealth>();
			if (enemyHealth != null)
			{
				enemyHealth.TakeDamage(Mathf.RoundToInt(damagePoints), hit.point);
			}
			else // Для других объектов, реализующих IEntity
			{
				entity.ApplyDamage(damagePoints);
			}
		}
	}

	public void SetDamage(float points)
	{
		damagePoints = points;
	}
}