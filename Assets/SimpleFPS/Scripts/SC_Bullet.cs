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
		if (hit.rigidbody != null)
		{
			hit.rigidbody.AddForce(transform.forward * hitForce);
		}

		IEntity entity = hit.transform.GetComponent<IEntity>();
		if (entity != null)
		{
			entity.ApplyDamage(damagePoints);
		}

		if (impactEffect != null)
		{
			Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
		}
	}

	public void SetDamage(float points)
	{
		damagePoints = points;
	}
}