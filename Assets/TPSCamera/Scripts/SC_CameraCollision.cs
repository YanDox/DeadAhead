using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SC_CameraCollision : MonoBehaviour
{
	[Header("References")]
	public Transform playerTransform; // Ссылка на трансформ игрока
	public Animator playerAnimator;  // Ссылка на аниматор игрока

	[Header("Camera Settings")]
	public float collisionOffset = 0.3f;
	public float normalSpeed = 10f;
	public float aimSpeed = 20f;
	public float defaultDistance = 2f;
	public Vector3 normalOffset = new Vector3(0, 1.6f, -2f);
	public Vector3 aimOffset = new Vector3(0.5f, 1.4f, -1f);
	public Vector3 coverOffset = new Vector3(0.3f, 0.7f, -0.8f);
	public float aimFOV = 40f;
	public float normalFOV = 60f;
	public float transitionSpeed = 10f;

	[Header("Collision Settings")]
	public LayerMask collisionMask; // Маска для коллизий камеры

	public bool IsAiming { get; private set; }

	private Camera cam;
	private Vector3 targetPosition;
	private float currentDistance;
	private bool isInCover;
	private Vector3 smoothVelocity;

	void Start()
	{
		cam = GetComponent<Camera>();
		currentDistance = defaultDistance;

		// Инициализация начальной позиции
		transform.localPosition = normalOffset;
	}

	void LateUpdate()
	{
		HandleAim();
		HandleCover();
		UpdateCameraPosition();
		UpdateFieldOfView();
	}

	void HandleAim()
	{
		if (Input.GetMouseButtonDown(1))
		{
			IsAiming = !IsAiming;
			playerAnimator.SetBool("Aim", IsAiming);
		}
	}

	void HandleCover()
	{
		isInCover = playerAnimator != null && playerAnimator.GetBool("InCover");
	}

	void UpdateCameraPosition()
	{
		// Получаем целевую позицию камеры относительно игрока
		Vector3 desiredPosition = playerTransform.TransformPoint(GetTargetOffset());

		// Проверяем коллизии
		CheckCollision(ref desiredPosition);

		// Плавное перемещение камеры
		float speed = IsAiming ? aimSpeed : normalSpeed;
		transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, 0.1f, speed);

		// Камера всегда смотрит на игрока
		transform.LookAt(playerTransform.position + Vector3.up * 1.6f);
	}

	Vector3 GetTargetOffset()
	{
		if (isInCover) return coverOffset;
		return IsAiming ? aimOffset : normalOffset;
	}

	void CheckCollision(ref Vector3 targetPos)
	{
		Vector3 direction = targetPos - playerTransform.position;
		float distance = direction.magnitude;
		direction.Normalize();

		if (Physics.SphereCast(playerTransform.position + Vector3.up * 0.5f, collisionOffset, direction, out RaycastHit hit, distance, collisionMask))
		{
			currentDistance = Mathf.Clamp(hit.distance - 0.2f, 0.5f, distance);
			targetPos = playerTransform.position + direction * currentDistance;
		}
		else
		{
			currentDistance = distance;
		}
	}

	void UpdateFieldOfView()
	{
		float targetFOV = IsAiming ? aimFOV : normalFOV;
		cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, transitionSpeed * Time.deltaTime);
	}
}