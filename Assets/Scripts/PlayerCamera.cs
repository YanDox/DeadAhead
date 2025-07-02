using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class PlayerCamera : MonoBehaviour
{
	[Header("Camera Settings")]
	public Transform player;
	public Vector3 rightShoulderOffset = new Vector3(1.5f, 1.7f, -2.5f);
	public Vector3 leftShoulderOffset = new Vector3(-1.5f, 1.7f, -2.5f);
	public float rotationSpeed = 3f;
	public float cameraSmoothness = 5f;
	public float collisionOffset = 0.3f;
	public float minDistance = 1.0f;
	public LayerMask collisionLayers;

	[Header("Shoulder Switching")]
	public bool rightShoulder = true;
	public float shoulderSwitchSpeed = 5f;
	public KeyCode switchShoulderKey = KeyCode.V;

	[Header("Aiming Settings")]
	public float aimFOV = 40f;
	public float normalFOV = 60f;
	public float aimSpeed = 10f;
	public Vector3 aimPositionOffset = new Vector3(0, 0, -1f);
	private bool isAiming = false;
	private Vector3 originalRightOffset;
	private Vector3 originalLeftOffset;

	[Header("Combat Settings")]
	public float recoilAmount = 0.5f;
	public float recoilRecoverySpeed = 2f;
	public float maxRecoilAngle = 10f;
	private float currentRecoil;

	[Header("Crosshair Settings")]
	public Sprite crosshairSprite;
	public float crosshairSize = 24f;
	public Color normalColor = new Color(1, 1, 1, 0.8f);
	public Color enemyColor = Color.red;
	public float colorChangeSpeed = 10f;
	public float crosshairExpandAmount = 10f;
	public float crosshairRecoverySpeed = 5f;
	private float currentCrosshairSize;

	[Header("Target Detection")]
	public float detectionRange = 50f;
	public float detectionRadius = 0.5f;
	public LayerMask enemyLayer;
	public bool debugMode = true;

	private float mouseX;
	private float mouseY;
	private float targetDistance;
	private Vector3 currentOffset;
	private Canvas crosshairCanvas;
	private Image crosshairImage;
	private bool isTargetingEnemy;
	private Camera cam;

	public bool IsAiming { get { return isAiming; } }
	public Camera CameraComponent { get { return cam; } }

	void Awake()
	{
		cam = GetComponent<Camera>();
		originalRightOffset = rightShoulderOffset;
		originalLeftOffset = leftShoulderOffset;
	}

	void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		InitializeCamera();
		CreateCrosshair();
		currentCrosshairSize = crosshairSize;
	}

	void InitializeCamera()
	{
		Vector3 angles = transform.eulerAngles;
		mouseX = angles.y;
		mouseY = angles.x;
		currentOffset = rightShoulder ? rightShoulderOffset : leftShoulderOffset;
		targetDistance = currentOffset.magnitude;
		cam.fieldOfView = normalFOV;
	}

	void Update()
	{
		HandleAiming();
		if (isAiming) HandleShoulderSwitch();
		DetectEnemy();
		UpdateCrosshair();
		UpdateRecoil();
	}

	void LateUpdate()
	{
		UpdateCameraPosition();
	}

	void HandleAiming()
	{
		if (Input.GetMouseButtonDown(1))
		{
			isAiming = !isAiming;

			if (isAiming)
			{
				rightShoulderOffset = originalRightOffset + aimPositionOffset;
				leftShoulderOffset = originalLeftOffset + aimPositionOffset;
			}
			else
			{
				rightShoulderOffset = originalRightOffset;
				leftShoulderOffset = originalLeftOffset;
			}
		}

		cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, isAiming ? aimFOV : normalFOV, aimSpeed * Time.deltaTime);
	}

	void HandleShoulderSwitch()
	{
		if (Input.GetKeyDown(switchShoulderKey))
		{
			rightShoulder = !rightShoulder;
		}
	}

	void UpdateRecoil()
	{
		currentRecoil = Mathf.Lerp(currentRecoil, 0, recoilRecoverySpeed * Time.deltaTime);
		currentCrosshairSize = Mathf.Lerp(currentCrosshairSize, crosshairSize, crosshairRecoverySpeed * Time.deltaTime);
	}

	public void ApplyRecoil(float customRecoil = -1f)
	{
		float recoil = customRecoil > 0 ? customRecoil : recoilAmount;
		currentRecoil += recoil;
		currentCrosshairSize += crosshairExpandAmount;
		currentRecoil = Mathf.Clamp(currentRecoil, 0, maxRecoilAngle);
	}

	void DetectEnemy()
	{
		Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
		RaycastHit hit;

		isTargetingEnemy = Physics.SphereCast(
			centerRay,
			detectionRadius,
			out hit,
			detectionRange,
			enemyLayer);

		if (debugMode)
		{
			Debug.DrawRay(centerRay.origin, centerRay.direction * detectionRange,
				isTargetingEnemy ? Color.green : Color.red, 0.1f);
		}
	}

	void UpdateCrosshair()
	{
		if (crosshairImage == null) return;

		Color targetColor = isTargetingEnemy ? enemyColor : normalColor;
		crosshairImage.color = Color.Lerp(
			crosshairImage.color,
			targetColor,
			colorChangeSpeed * Time.deltaTime);

		crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(
			crosshairImage.rectTransform.sizeDelta,
			new Vector2(currentCrosshairSize, currentCrosshairSize),
			crosshairRecoverySpeed * Time.deltaTime);
	}

	void UpdateCameraPosition()
	{
		mouseX += Input.GetAxis("Mouse X") * rotationSpeed;
		mouseY -= (Input.GetAxis("Mouse Y") * rotationSpeed) + currentRecoil;
		mouseY = Mathf.Clamp(mouseY, -30f, 70f);

		currentOffset = Vector3.Lerp(
			currentOffset,
			rightShoulder ? rightShoulderOffset : leftShoulderOffset,
			shoulderSwitchSpeed * Time.deltaTime);

		Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);

		if (isAiming)
		{
			player.rotation = Quaternion.Euler(0, mouseX, 0);
		}

		Vector3 targetPosition = player.position + rotation * currentOffset;

		HandleCollision(targetPosition);

		transform.position = Vector3.Lerp(
			transform.position,
			player.position + rotation * currentOffset.normalized * targetDistance,
			cameraSmoothness * Time.deltaTime);

		transform.LookAt(player.position + Vector3.up * 1.5f);
	}

	void HandleCollision(Vector3 targetPos)
	{
		Vector3 direction = targetPos - player.position;
		float distance = direction.magnitude;
		RaycastHit hit;

		if (Physics.SphereCast(
			player.position,
			collisionOffset,
			direction.normalized,
			out hit,
			distance,
			collisionLayers))
		{
			targetDistance = Mathf.Clamp(hit.distance - collisionOffset * 2, minDistance, distance);
		}
		else
		{
			targetDistance = distance;
		}
	}

	void CreateCrosshair()
	{
		crosshairCanvas = new GameObject("CrosshairCanvas").AddComponent<Canvas>();
		crosshairCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
		crosshairCanvas.sortingOrder = 1000;

		GameObject crosshairGO = new GameObject("Crosshair");
		crosshairGO.transform.SetParent(crosshairCanvas.transform);

		crosshairImage = crosshairGO.AddComponent<Image>();
		crosshairImage.rectTransform.sizeDelta = new Vector2(crosshairSize, crosshairSize);
		crosshairImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		crosshairImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		crosshairImage.rectTransform.anchoredPosition = Vector2.zero;
		crosshairImage.type = Image.Type.Simple;
		crosshairImage.preserveAspect = true;
		crosshairImage.color = normalColor;

		if (crosshairSprite != null)
		{
			crosshairImage.sprite = crosshairSprite;
		}
		else
		{
			CreateDefaultCrosshair();
		}
	}

	void CreateDefaultCrosshair()
	{
		int size = 64;
		Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
		Color[] pixels = new Color[size * size];
		Color crossColor = Color.white;

		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				bool isCross = (Mathf.Abs(x - size / 2) < 3 && y > size / 4 && y < 3 * size / 4) ||
							  (Mathf.Abs(y - size / 2) < 3 && x > size / 4 && x < 3 * size / 4);
				pixels[y * size + x] = isCross ? crossColor : Color.clear;
			}
		}

		tex.SetPixels(pixels);
		tex.Apply();
		crosshairImage.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
	}

	void OnDestroy()
	{
		if (crosshairCanvas != null)
		{
			Destroy(crosshairCanvas.gameObject);
		}
	}

	public Ray GetCenterRay()
	{
		return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
	}
	

	void OnDrawGizmosSelected()
	{
		if (player != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(player.position, transform.position);
			Gizmos.DrawWireSphere(transform.position, collisionOffset);
		}
	}
}