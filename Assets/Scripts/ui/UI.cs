using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class UI : MonoBehaviour
{
    [Header("Weapon Display")]
    public Image primaryWeaponImage;
    public Image secondaryWeaponImage;
    public RectTransform activeWeaponHighlight;
    public RectTransform ammoBar; // Теперь используем RectTransform вместо Image
    public TMP_Text ammoText;
    public float highlightMoveSpeed = 10f;

    [Header("Ultimate Display")]
    public Image ultimateIcon;
    public Color ultimateReadyColor = Color.yellow;
    public Color ultimateNotReadyColor = Color.gray;
    public float pulseSpeed = 2f;

    [Header("Inventory Items")]
    public Image colaCrateIcon;
    public TMP_Text busPartsText;

    [Header("Health Display")]
    public RectTransform healthBar; // RectTransform для шкалы здоровья
    public TMP_Text healthText;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;
    public Image healthBarImage; // Для изменения цвета
	public GameObject qestUi;


	private SC_WeaponManager weaponManager;
    private Inventory inventory;
    private PlayerHealth playerHealth;
    private Vector3 targetHighlightPosition;
    private Vector2 ammoBarOriginalSize;
    private Vector2 healthBarOriginalSize;

    void Start()
    {
		qestUi.active = false;
		weaponManager = FindObjectOfType<SC_WeaponManager>();
        inventory = FindObjectOfType<Inventory>();
        playerHealth = FindObjectOfType<PlayerHealth>();

        colaCrateIcon.gameObject.SetActive(false);
        ultimateIcon.gameObject.SetActive(false);

        // Сохраняем оригинальные размеры для шкал
        if (ammoBar != null) ammoBarOriginalSize = ammoBar.sizeDelta;
        if (healthBar != null) healthBarOriginalSize = healthBar.sizeDelta;

        if (weaponManager != null && activeWeaponHighlight != null)
        {
            targetHighlightPosition = weaponManager.selectedWeapon == weaponManager.primaryWeapon ?
                primaryWeaponImage.transform.position : secondaryWeaponImage.transform.position;
            activeWeaponHighlight.position = targetHighlightPosition;
        }
    }

    void Update()
    {
        UpdateWeaponDisplay();
        UpdateUltimateDisplay();
        UpdateInventoryDisplay();
        UpdateHealthDisplay();
      
    }
   
    void UpdateWeaponDisplay()
    {
        if (weaponManager == null || weaponManager.selectedWeapon == null) return;

        UpdateWeaponHighlight();

        if (weaponManager.selectedWeapon.ammoType == Inventory.RIFLE_AMMO)
        {
            float ammoPercent = (float)weaponManager.selectedWeapon.currentBulletsInMagazine /
                              weaponManager.selectedWeapon.magazineSize;

            // Обновляем шкалу патронов через масштабирование
            if (ammoBar != null)
            {
                ammoBar.sizeDelta = new Vector2(ammoBarOriginalSize.x * ammoPercent, ammoBarOriginalSize.y);
            }

            ammoText.text = $"{weaponManager.selectedWeapon.currentBulletsInMagazine}/{inventory.items[Inventory.RIFLE_AMMO]}";
        }
        else
        {
            if (ammoBar != null) ammoBar.sizeDelta = ammoBarOriginalSize;
            ammoText.text = "";
        }
    }

    void UpdateWeaponHighlight()
    {
        if (activeWeaponHighlight == null) return;

        targetHighlightPosition = weaponManager.selectedWeapon == weaponManager.primaryWeapon ?
            primaryWeaponImage.transform.position : secondaryWeaponImage.transform.position;

        activeWeaponHighlight.position = Vector3.Lerp(
            activeWeaponHighlight.position,
            targetHighlightPosition,
            Time.deltaTime * highlightMoveSpeed);
    }

    void UpdateUltimateDisplay()
    {
        if (inventory == null || ultimateIcon == null) return;

        bool ultimateReady = inventory.ultimatePoints >= inventory.maxUltimate;
        ultimateIcon.gameObject.SetActive(ultimateReady);

        if (ultimateReady)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 0.3f) + 0.7f;
            ultimateIcon.color = ultimateReadyColor * pulse;
        }
        else
        {
            ultimateIcon.color = ultimateNotReadyColor;
        }
    }

    void UpdateInventoryDisplay()
    {
        if (inventory == null) return;

        colaCrateIcon.gameObject.SetActive(inventory.items[Inventory.COLA_CRATE] > 0);
        busPartsText.text = $"{inventory.items[Inventory.BUS_PART]}";
    }

    void UpdateHealthDisplay()
    {
        if (playerHealth == null || healthBar == null) return;

        float healthPercent = (float)playerHealth.currentHealth / playerHealth.maxHealth;

        // Обновляем шкалу здоровья через масштабирование
        healthBar.sizeDelta = new Vector2(healthBarOriginalSize.x, healthBarOriginalSize.y * healthPercent);

        // Обновляем цвет
        if (healthBarImage != null)
        {
            healthBarImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            
            // Эффект при низком здоровье
            if (healthPercent < lowHealthThreshold)
            {
                float pulse = Mathf.PingPong(Time.time * pulseSpeed, 0.2f) + 0.8f;
                healthBarImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent) * pulse;
            }
        }

        healthText.text = $"{playerHealth.currentHealth}/{playerHealth.maxHealth}";
    }
}