using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class SC_TPSController : MonoBehaviour
{
	[Header("Movement Settings")]
	public float walkSpeed = 5f;
	public float aimSpeed = 2.5f;
	public float sprintSpeed = 8f;
	public float crouchSpeed = 2f;
	public float jumpHeight = 1.5f;
	public float gravityMultiplier = 2f;
	public float airControl = 0.5f;
	public float crouchHeight = 1f;
	public float standHeight = 2f;
	public float crouchTransitionSpeed = 5f;

	[Header("Camera Settings")]
	public Camera playerCamera;
	public float lookSpeed = 2f;
	public float lookXLimit = 80f;
	public Vector3 cameraStandOffset = new Vector3(0, 1.6f, -2f);
	public Vector3 cameraCrouchOffset = new Vector3(0, 0.8f, -2f);
	public Vector3 cameraAimOffset = new Vector3(0.5f, 1.4f, -1f);
	public Vector3 cameraCoverOffset = new Vector3(0.3f, 0.7f, -0.8f);
	public float cameraMoveSpeed = 10f;

	[Header("Cover System")]
	public float coverCheckDistance = 0.8f;
	public float coverHeightThreshold = 1.2f;
	public float coverPeekDistance = 1f;
	public LayerMask coverMask;
	public KeyCode coverKey = KeyCode.E;

	[Header("Animation Settings")]
	public float animationSmoothTime = 0.1f;

	[HideInInspector] public bool IsInCover { get; private set; }
	[HideInInspector] public bool canMove = true;
	[HideInInspector] public bool CanPeekLeft { get; private set; }
	[HideInInspector] public bool CanPeekRight { get; private set; }

	private CharacterController controller;
	private Animator animator;
	private Vector2 rotation;
	private float currentHeight;
	private bool isCrouching;
	private bool isSprinting;
	private bool isJumping;
	private bool isGrounded;
	private Vector3 coverNormal;
	private float verticalVelocity;
	private bool isAiming;

	void Start()
	{
		controller = GetComponent<CharacterController>();
		animator = GetComponent<Animator>();
		rotation.y = transform.eulerAngles.y;
		currentHeight = standHeight;

		if (playerCamera == null)
			playerCamera = Camera.main;

		playerCamera.transform.localPosition = cameraStandOffset;
		playerCamera.transform.localRotation = Quaternion.identity;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void Update()
	{
		isGrounded = controller.isGrounded;

		if (isGrounded && verticalVelocity < 0)
		{
			verticalVelocity = -2f;
			isJumping = false;
			animator.SetBool("Grounded", true);
		}

		HandleAim();
		HandleMovement();
		HandleCameraRotation();
		HandleCover();
		HandleJump();
		ApplyGravity();
		UpdateAnimations();
		UpdateCameraPosition();
	}

	// Добавленный метод для определения скорости движения
	float GetTargetSpeed()
	{
		if (IsInCover) return crouchSpeed;
		if (isCrouching) return crouchSpeed;
		if (isSprinting) return sprintSpeed;
		if (isAiming) return aimSpeed;
		return walkSpeed;
	}

	void HandleAim()
	{
		if (Input.GetMouseButtonDown(1))
		{
			isAiming = !isAiming;
			animator.SetBool("Aim", isAiming);
		}
	}

	void UpdateCameraPosition()
	{
		Vector3 targetOffset;

		if (IsInCover)
			targetOffset = cameraCoverOffset;
		else if (isCrouching)
			targetOffset = isAiming ? cameraAimOffset : cameraCrouchOffset;
		else
			targetOffset = isAiming ? cameraAimOffset : cameraStandOffset;

		playerCamera.transform.localPosition = Vector3.Lerp(
			playerCamera.transform.localPosition,
			targetOffset,
			Time.deltaTime * cameraMoveSpeed);
	}

	void HandleMovement()
	{
		if (!canMove) return;

		float speed = GetTargetSpeed();
		Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;

		float controlFactor = isGrounded ? 1f : airControl;

		if (input.magnitude >= 0.1f)
		{
			float targetAngle = Mathf.Atan2(input.x, input.z) * Mathf.Rad2Deg + playerCamera.transform.eulerAngles.y;
			Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
			controller.Move(moveDir.normalized * speed * controlFactor * Time.deltaTime);
		}

		isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && !IsInCover && isGrounded && !isAiming;
	}

	void HandleCameraRotation()
	{
		if (!canMove) return;

		rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
		rotation.x -= Input.GetAxis("Mouse Y") * lookSpeed;
		rotation.x = Mathf.Clamp(rotation.x, -lookXLimit, lookXLimit);

		playerCamera.transform.localRotation = Quaternion.Euler(rotation.x, 0, 0);
		transform.rotation = Quaternion.Euler(0, rotation.y, 0);
	}

	void HandleCover()
	{
		bool coverDetected = Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward,
			out RaycastHit hit, coverCheckDistance, coverMask);

		if (coverDetected)
		{
			coverNormal = hit.normal;
			if (hit.collider.bounds.size.y < coverHeightThreshold && !IsInCover)
			{
				ToggleCrouch(true);
			}

			if (Input.GetKeyDown(coverKey) && !IsInCover)
			{
				EnterCover();
			}
		}

		if (IsInCover && (Input.GetKey(KeyCode.S) || !coverDetected))
		{
			ExitCover();
		}

		if (IsInCover)
		{
			CheckPeekDirections();
		}
	}

	void EnterCover()
	{
		IsInCover = true;
		canMove = false;
		animator.SetBool("InCover", true);
	}

	void ExitCover()
	{
		IsInCover = false;
		canMove = true;
		animator.SetBool("InCover", false);
	}

	void CheckPeekDirections()
	{
		CanPeekLeft = !Physics.Raycast(transform.position, -transform.right, coverPeekDistance, coverMask);
		CanPeekRight = !Physics.Raycast(transform.position, transform.right, coverPeekDistance, coverMask);
	}

	void ApplyGravity()
	{
		verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

		if (verticalVelocity < -20f)
			verticalVelocity = -20f;

		controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
	}

	void HandleJump()
	{
		if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching && !IsInCover && canMove)
		{
			verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * gravityMultiplier * jumpHeight);
			isJumping = true;
			animator.SetTrigger("Jump");
			animator.SetBool("Grounded", false);
		}
	}

	void UpdateAnimations()
	{
		float forward = Input.GetAxis("Vertical");
		float strafe = Input.GetAxis("Horizontal");

		if (isSprinting)
		{
			forward = Mathf.Clamp(forward, 0, 1);
		}

		animator.SetFloat("Forward", forward, animationSmoothTime, Time.deltaTime);
		animator.SetFloat("Strafe", strafe, animationSmoothTime, Time.deltaTime);
		animator.SetBool("Sprint", isSprinting);
		animator.SetBool("Crouch", isCrouching);
	}

	void ToggleCrouch(bool state)
	{
		isCrouching = state;
		currentHeight = isCrouching ? crouchHeight : standHeight;
		StartCoroutine(AdjustHeight());
	}

	IEnumerator AdjustHeight()
	{
		while (Mathf.Abs(controller.height - currentHeight) > 0.01f)
		{
			controller.height = Mathf.Lerp(controller.height, currentHeight, crouchTransitionSpeed * Time.deltaTime);
			yield return null;
		}
		controller.height = currentHeight;
	}
}