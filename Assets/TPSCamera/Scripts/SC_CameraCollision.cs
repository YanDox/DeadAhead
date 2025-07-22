using UnityEngine;

public class SC_CameraCollision : MonoBehaviour
{
	[Header("References")]
	public Transform referenceTransform;


	[Header("Collision Settings")]
	public float collisionOffset = 0.3f;
	public float cameraSpeed = 15f;
	public float aimCameraSpeed = 20f;
	public float collisionSmoothTime = 0.1f;

	[Header("Camera Rotation Limits")]
	public float minVerticalAngle = -30f;
	public float maxVerticalAngle = 70f;
	public float rotationSmoothness = 10f;
	public float horizontalRotationSpeed = 5f;
	public float verticalRotationSpeed = 5f;

	[Header("Aim Settings")]
	public Vector3 defaultAimOffset = new Vector3(0.5f, -0.2f, -1f);
	public Vector3 defaultLeftOffset = new Vector3(-0.5f, -0.2f, -1f);
	public Vector3 coverAimOffset = new Vector3(0.3f, -0.1f, -0.8f);
	public Vector3 coverLeftOffset = new Vector3(-0.3f, -0.1f, -0.8f);
	public float aimFOV = 40f;
	public float aimTransitionSpeed = 10f;
	public float aimPositionSmoothTime = 0.15f;
	public KeyCode aimKey = KeyCode.Mouse1;
	public KeyCode switchShoulderKey = KeyCode.Q;

	[Header("Other Settings")]
	public LayerMask collisionLayers = ~0;
	public float cameraReturnSmoothness = 5f;
	public Animator animator;

	[Header("Debug")]
	[SerializeField] private bool _isAiming = false;
	[SerializeField] private bool _isRightShoulder = true;

	private Vector3 _defaultPos;
	private Vector3 _directionNormalized;
	private Transform _parentTransform;
	private Camera _playerCamera;
	private SC_TPSController _tpsController;
	private float _defaultDistance;
	private float _defaultFOV;
	private float _currentFOV;
	private Vector3 _cameraVelocity;
	private float _currentXRotation = 0f;
	private Vector3 _aimPositionVelocity;
	private Vector3 _collisionPositionVelocity;

	public bool IsAiming => _isAiming;

	void Start()
	{
		InitializeCamera();
		InitializeReferences();
		SetupCursor();
	}

	void InitializeCamera()
	{
		_defaultPos = transform.localPosition;
		_directionNormalized = _defaultPos.normalized;
		_parentTransform = transform.parent;
		_defaultDistance = Vector3.Distance(_defaultPos, Vector3.zero);
		_playerCamera = GetComponent<Camera>();
		_defaultFOV = _playerCamera.fieldOfView;
		_currentFOV = _defaultFOV;


	}

	void InitializeReferences()
	{
		if (referenceTransform == null)
		{
			Debug.LogError("Reference Transform is not assigned!", this);
			return;
		}

		_tpsController = referenceTransform.GetComponent<SC_TPSController>();
		if (_tpsController == null)
		{
			Debug.LogWarning("No SC_TPSController found on reference transform", this);
		}
	}

	void SetupCursor()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void LateUpdate()
	{
		HandleCameraRotation();
		HandleAimInput();
		UpdateCameraPosition();
		UpdateFieldOfView();
	}

	void HandleCameraRotation()
	{
		

		float mouseX = Input.GetAxis("Mouse X") * horizontalRotationSpeed;
		float mouseY = Input.GetAxis("Mouse Y") * verticalRotationSpeed;

		// Плавное вращение по горизонтали
		_parentTransform.Rotate(Vector3.up, mouseX);

		// Плавное вращение по вертикали с ограничениями
		_currentXRotation -= mouseY;
		_currentXRotation = Mathf.Clamp(_currentXRotation, minVerticalAngle, maxVerticalAngle);
	
	}

	void HandleAimInput()
	{
		if (Input.GetKeyDown(aimKey))
		{
			_isAiming = !_isAiming;
			if (animator != null)
			{
				animator.SetBool("Aim", _isAiming);
			}
		}

		if (Input.GetKeyDown(switchShoulderKey))
		{
			_isRightShoulder = !_isRightShoulder;
		}
	}

	void UpdateFieldOfView()
	{
		float targetFOV = _isAiming ? aimFOV : _defaultFOV;
		_currentFOV = Mathf.Lerp(_currentFOV, targetFOV, aimTransitionSpeed * Time.deltaTime);
		_playerCamera.fieldOfView = _currentFOV;
	}

	void UpdateCameraPosition()
	{
		Vector3 targetPos = GetTargetPosition();
		Vector3 adjustedPos = AdjustForCollision(targetPos);

		// Разные параметры сглаживания для разных состояний
		float positionSmoothTime = _isAiming ? aimPositionSmoothTime : collisionSmoothTime;

		// Плавное перемещение камеры с учетом коллизий
		transform.localPosition = Vector3.SmoothDamp(
			transform.localPosition,
			adjustedPos,
			ref _cameraVelocity,
			positionSmoothTime
		);
	}

	Vector3 GetTargetPosition()
	{
		if (!_isAiming) return _defaultPos;

		bool inCover = _tpsController != null && _tpsController.IsInCover;

		return _isRightShoulder ?
			(inCover ? coverAimOffset : defaultAimOffset) :
			(inCover ? coverLeftOffset : defaultLeftOffset);
	}

	Vector3 AdjustForCollision(Vector3 targetPosition)
	{
		if (_parentTransform == null || referenceTransform == null)
			return targetPosition;

		Vector3 worldTargetPos = _parentTransform.TransformPoint(targetPosition);
		Vector3 dir = worldTargetPos - referenceTransform.position;

		if (Physics.SphereCast(
			referenceTransform.position,
			collisionOffset,
			dir.normalized,
			out RaycastHit hit,
			dir.magnitude,
			collisionLayers,
			QueryTriggerInteraction.Ignore))
		{
			float adjustedDistance = Mathf.Max(0.1f, hit.distance - collisionOffset);
			return _parentTransform.InverseTransformPoint(referenceTransform.position + dir.normalized * adjustedDistance);
		}

		return targetPosition;
	}

	public void ForceAim(bool enable)
	{
		_isAiming = enable;
		if (animator != null)
		{
			animator.SetBool("Aim", _isAiming);
		}
	}
}