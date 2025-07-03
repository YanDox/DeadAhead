using UnityEngine;

public class SC_CameraCollision : MonoBehaviour
{
	[Header("References")]
	public Transform referenceTransform;

	[Header("Collision Settings")]
	public float collisionOffset = 0.3f;
	public float cameraSpeed = 15f;
	public float aimCameraSpeed = 20f;

	[Header("Aim Settings")]
	public Vector3 defaultAimOffset = new Vector3(0.5f, -0.2f, -1f);
	public Vector3 defaultLeftOffset = new Vector3(-0.5f, -0.2f, -1f);
	public Vector3 coverAimOffset = new Vector3(0.3f, -0.1f, -0.8f);
	public Vector3 coverLeftOffset = new Vector3(-0.3f, -0.1f, -0.8f);
	public float aimFOV = 40f;
	public float aimTransitionSpeed = 10f;
	public KeyCode aimKey = KeyCode.Mouse1;
	public KeyCode switchShoulderKey = KeyCode.Q;

	[Header("Debug")]
	[SerializeField] private bool _isAiming = false;
	[SerializeField] private bool _isRightShoulder = true;

	private Vector3 _defaultPos;
	private Vector3 _directionNormalized;
    private Vector3 _currentOffset;
    private Transform _parentTransform;
    private Camera _playerCamera;
    private SC_TPSController _tpsController;
    private float _defaultDistance;
	private float _defaultFOV;
	private float _currentFOV;

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
		HandleAimInput();
		UpdateCameraPosition();
		UpdateFieldOfView();
	}

	void HandleAimInput()
	{
		if (Input.GetKeyDown(aimKey))
		{
			_isAiming = !_isAiming;
			Debug.Log($"Aiming: {_isAiming}");
		}

		if (Input.GetKeyDown(switchShoulderKey))
		{
			_isRightShoulder = !_isRightShoulder;
			Debug.Log($"Shoulder: {(_isRightShoulder ? "Right" : "Left")}");
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

		float speed = _isAiming ? aimCameraSpeed : cameraSpeed;
		transform.localPosition = Vector3.Lerp(
			transform.localPosition,
			adjustedPos,
			speed * Time.deltaTime
		);
	}

	Vector3 GetTargetPosition()
	{
		if (!_isAiming) return _defaultPos;

		bool inCover = _tpsController != null && _tpsController.IsInCover;

		if (_isRightShoulder)
		{
			return inCover ? coverAimOffset : defaultAimOffset;
		}
		else
		{
			return inCover ? coverLeftOffset : defaultLeftOffset;
		}
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
			dir,
			out RaycastHit hit,
			_defaultDistance))
		{
			float adjustedDistance = Mathf.Max(0, hit.distance - collisionOffset);
			return _directionNormalized * adjustedDistance;
		}

		return targetPosition;
	}
}