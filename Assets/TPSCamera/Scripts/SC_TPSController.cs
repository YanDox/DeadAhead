using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SC_TPSController : MonoBehaviour
{
	[Header("Movement Settings")]
	public float speed = 7.5f;
	public float aimMovementSpeed = 3.5f;
	public float sprintSpeed = 12f;
	public float crouchSpeed = 3f;
	public float jumpSpeed = 8.0f;
	public float gravity = 20.0f;
	public float crouchHeight = 1f;
	public float standHeight = 2f;

	[Header("Cover System")]
	public float coverCheckDistance = 0.8f;
	public float lowCoverHeight = 1.2f;
	public float coverSnapSpeed = 5f;
	public LayerMask coverMask;
	public KeyCode coverKey = KeyCode.E;

	[Header("Camera")]
	public Transform playerCameraParent;
	public float lookSpeed = 2.0f;
	public float lookXLimit = 60.0f;

	private CharacterController characterController;
	private Vector3 moveDirection = Vector3.zero;
	private Vector2 rotation = Vector2.zero;
	private float originalSpeed;
	private float originalHeight;
	private bool isCrouching = false;
	private bool isSprinting = false;
	private bool isInCover = false;
	private Vector3 coverNormal;
	private float coverTimer;
	private bool canPeekLeft;
	private bool canPeekRight;

	[HideInInspector] public bool canMove = true;
	[HideInInspector] public bool CanPeekLeft => canPeekLeft;
	[HideInInspector] public bool CanPeekRight => canPeekRight;
	[HideInInspector] public bool IsInCover => isInCover;

	void Start()
	{
		characterController = GetComponent<CharacterController>();
		rotation.y = transform.eulerAngles.y;
		originalSpeed = speed;
		originalHeight = characterController.height;
	}

	void Update()
	{
		HandleCoverSystem();
		HandleMovement();
		HandleCameraRotation();
	}

	void HandleCoverSystem()
	{
		bool coverDetected = CheckCover();

		if (coverDetected && Input.GetKeyDown(coverKey) && !isInCover)
		{
			EnterCover();
		}
		else if (isInCover && (Input.GetKey(KeyCode.S) || !coverDetected))
		{
			ExitCover();
		}

		if (isInCover)
		{
			coverTimer += Time.deltaTime;
			if (coverTimer < 0.5f)
			{
				SnapToCover();
			}
			CheckPeekDirections();
			HandleCoverMovement();
		}
	}

	bool CheckCover()
	{
		if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward,
						  out RaycastHit hit, coverCheckDistance, coverMask))
		{
			coverNormal = hit.normal;
			if (hit.collider.bounds.size.y < lowCoverHeight && !isInCover)
			{
				isCrouching = true;
				characterController.height = crouchHeight;
			}
			return true;
		}
		return false;
	}

	void EnterCover()
	{
		isInCover = true;
		coverTimer = 0f;
		canMove = false;
	}

	void ExitCover()
	{
		isInCover = false;
		canMove = true;

		if (isCrouching && !Physics.Raycast(transform.position, transform.forward,
										  coverCheckDistance, coverMask))
		{
			isCrouching = false;
			characterController.height = originalHeight;
		}
	}

	void SnapToCover()
	{
		Vector3 targetPosition = transform.position - coverNormal * 0.3f;
		transform.position = Vector3.Lerp(transform.position, targetPosition, coverSnapSpeed * Time.deltaTime);
	}

	void CheckPeekDirections()
	{
		canPeekLeft = !Physics.Raycast(transform.position, -transform.right, 1f, coverMask);
		canPeekRight = !Physics.Raycast(transform.position, transform.right, 1f, coverMask);
	}

	void HandleCoverMovement()
	{
		if (canPeekLeft && Input.GetKey(KeyCode.A))
		{
			characterController.Move(-transform.right * speed * 0.5f * Time.deltaTime);
		}
		else if (canPeekRight && Input.GetKey(KeyCode.D))
		{
			characterController.Move(transform.right * speed * 0.5f * Time.deltaTime);
		}
	}

	void HandleMovement()
	{
		if (isInCover) return;

		if (Input.GetKeyDown(KeyCode.C) && canMove && characterController.isGrounded)
		{
			isCrouching = !isCrouching;
			characterController.height = isCrouching ? crouchHeight : originalHeight;
		}

		isSprinting = Input.GetKey(KeyCode.LeftShift) && canMove && !isCrouching && characterController.isGrounded;

		speed = isSprinting ? sprintSpeed :
			   isCrouching ? crouchSpeed :
			   originalSpeed;

		if (characterController.isGrounded)
		{
			Vector3 forward = transform.TransformDirection(Vector3.forward);
			Vector3 right = transform.TransformDirection(Vector3.right);
			float curSpeedX = canMove ? speed * Input.GetAxis("Vertical") : 0;
			float curSpeedY = canMove ? speed * Input.GetAxis("Horizontal") : 0;
			moveDirection = (forward * curSpeedX) + (right * curSpeedY);

			if (Input.GetButton("Jump") && canMove && !isCrouching)
			{
				moveDirection.y = jumpSpeed;
			}
		}

		moveDirection.y -= gravity * Time.deltaTime;
		characterController.Move(moveDirection * Time.deltaTime);
	}

	void HandleCameraRotation()
	{
		if (canMove)
		{
			rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
			rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;
			rotation.x = Mathf.Clamp(rotation.x, -lookXLimit, lookXLimit);
			playerCameraParent.localRotation = Quaternion.Euler(rotation.x, 0, 0);
			transform.eulerAngles = new Vector2(0, rotation.y);
		}
	}
}