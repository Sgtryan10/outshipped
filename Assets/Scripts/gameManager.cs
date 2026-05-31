using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class gameManager : MonoBehaviour
{
    public static gameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private HudController hudController;

    [Header("Player Health & Armor Settings")]
    [SerializeField] private int maxPlayerHealth = 100;
    private int currentPlayerHealth;
    private int currentArmorStacks = 0;

    [Header("Weapon & Ammo Settings")]
    // [SerializeField] private string initialTurretType = "STANDARD";
    [SerializeField] private int maxMagazineSize = 30;
    // [SerializeField] private int startingReserveAmmo = 90;

    private string currentTurretType;
    private int currentMagazine;
    private int currentReserve;

    private float fireRate;
    private float reloadTime;
    private float nextFireTime = 0f;
    private bool isReloading = false;

    private bool empActive = false;
    private bool overdriveActive = false;
    private bool ampedActive = false;
    private bool slowActive = false;

    public float damageMultiplier { get; private set; } = 1f;
    public float speedMultiplier { get; private set; } = 1f;

    private string storedAbility = "";

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        scoreManager.resetScores();
        isGameOver = false;
        Time.timeScale = 1f;

        currentPlayerHealth = maxPlayerHealth;
        currentArmorStacks = 0;

        currentTurretType = GameSelection.SelectedTurretType;

        maxMagazineSize = GameSelection.SelectedMagazine;
        currentMagazine = GameSelection.SelectedMagazine;
        currentReserve = GameSelection.SelectedReserve;

        fireRate = GameSelection.SelectedFireRate;
        reloadTime = GameSelection.SelectedReloadTime;

        if (hudController != null)
        {
            hudController.setMaxHealth(maxPlayerHealth);
            hudController.updateHealth(currentPlayerHealth);
            hudController.updateArmor(currentArmorStacks);

            hudController.setTurretType(currentTurretType);
            hudController.updateAmmo(currentMagazine, currentReserve);

            hudController.setCourier(GameSelection.SelectedCourier);
            hudController.setPassive(GameSelection.SelectedPassive);

            hudController.updatePackagesDelivered(0);
            hudController.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);

            hudController.DismissPopup();
        }
    }

    void Update()
    {
        if (isGameOver) return;
        scoreManager.timeSurvivedSeconds += Time.deltaTime;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            UseStoredAbility();
        }

        bool wantsToShoot = false;
        if (Mouse.current != null)
        {
            if (currentTurretType == "AUTOMATIC" || currentTurretType == "RAPID-FIRE")
            {
                wantsToShoot = Mouse.current.leftButton.isPressed;
            }
            else
            {
                wantsToShoot = Mouse.current.leftButton.wasPressedThisFrame;
            }
        }

        if (wantsToShoot && Time.time >= nextFireTime && !isReloading)
        {
            nextFireTime = Time.time + fireRate;
            bool hasAmmo = UseAmmo(1);

            if (hasAmmo)
            {
                // TODO: Trigger projectile/shooting logic here (Multiply base damage by damageMultiplier)
            }
            else
            {
                ReloadWeapon();
            }
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ReloadWeapon();
        }
    }

    public void StoreAbility(string abilityName)
    {
        if (isGameOver) return;
        storedAbility = abilityName;

        if (hudController != null)
        {
            switch (abilityName)
            {
                case "AMPED":
                    hudController.TriggerPopup("AMPED TURRET", "Press 'E' for 150% Damage (20s)", new Color(255f, 95f, 0f, 0.30f), 3.0f);
                    break;
                case "OVERDRIVE":
                    hudController.TriggerPopup("OVERDRIVE ACQUIRED", "Press 'E' for 150% Speed (20s)", new Color(255f, 95f, 0f, 0.30f), 3.0f);
                    break;
                case "EMP":
                    hudController.TriggerPopup("EMP ONLINE", "Press 'E' to discharge EMP pulse", new Color(255f, 95f, 0f, 0.30f), 3.0f);
                    break;
                case "SLOW":
                    hudController.TriggerPopup("SLOW FIELD READY", "Press 'E' to create a slowing field (20s)", new Color(255f, 95f, 0f, 0.30f), 3.0f);
                    break;
            }
        }
    }

    private void UseStoredAbility()
    {
        if (string.IsNullOrEmpty(storedAbility)) return;

        OnActiveAbilityUsed();

        switch (storedAbility)
        {
            case "AMPED":
                StartCoroutine(AmpedRoutine());
                break;
            case "OVERDRIVE":
                StartCoroutine(OverdriveRoutine());
                break;
            case "EMP":
                StartCoroutine(EmpRoutine());
                break;
            case "SLOW":
                StartCoroutine(SlowRoutine());
                break;
        }

        storedAbility = "";
    }

    private IEnumerator AmpedRoutine()
    {
        ampedActive = true;
        damageMultiplier = 1.5f;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);

        yield return new WaitForSeconds(20f);

        damageMultiplier = 1f;
        ampedActive = false;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);
    }

    private IEnumerator OverdriveRoutine()
    {
        overdriveActive = true;
        speedMultiplier = 1.5f;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);

        yield return new WaitForSeconds(20f);

        speedMultiplier = 1f;
        overdriveActive = false;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);
    }

    private IEnumerator EmpRoutine()
    {
        empActive = true;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);

        // TODO: EMP logic here

        yield return new WaitForSeconds(20f);

        empActive = false;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);
    }

    private IEnumerator SlowRoutine()
    {
        slowActive = true;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);

        // TODO: Slow logic here

        yield return new WaitForSeconds(20f);

        slowActive = false;
        hudController?.updateActiveAbility(empActive, overdriveActive, ampedActive, slowActive);
    }

    // Health and Armor Management
    public void TakeDamage(int damageAmount)
    {
        if (isGameOver) return;

        if (currentArmorStacks > 0)
        {
            currentArmorStacks--;
            hudController?.updateArmor(currentArmorStacks);
            return;
        }

        currentPlayerHealth -= damageAmount;
        currentPlayerHealth = Mathf.Max(currentPlayerHealth, 0);
        hudController?.updateHealth(currentPlayerHealth);

        if (currentPlayerHealth <= 0) TriggerGameOver();
    }

    public void Heal(int healAmount)
    {
        if (isGameOver) return;
        currentPlayerHealth = Mathf.Min(currentPlayerHealth + healAmount, maxPlayerHealth);
        hudController?.updateHealth(currentPlayerHealth);
    }

    public void AddArmor(int stacks)
    {
        if (isGameOver) return;
        currentArmorStacks += stacks;
        hudController?.updateArmor(currentArmorStacks);

        hudController?.TriggerPopup("ARMOR REINFORCED", "Armor plating nullifies one damage instance", new Color(51f, 51f, 51f, 0.30f), 3.0f);
    }

    public void AddAmmo(int amount)
    {
        if (isGameOver) return;
        currentReserve += amount;
        hudController?.updateAmmo(currentMagazine, currentReserve);
    }

    // Turret Management
    public void SetTurretType(string newType)
    {
        if (isGameOver) return;
        currentTurretType = newType;
        hudController?.setTurretType(currentTurretType);
    }

    public bool UseAmmo(int amount = 1)
    {
        if (isGameOver || currentMagazine <= 0) return false;
        currentMagazine = Mathf.Max(currentMagazine - amount, 0);
        hudController?.updateAmmo(currentMagazine, currentReserve);
        return true;
    }

    public void ReloadWeapon()
    {
        if (isGameOver || isReloading || currentMagazine == maxMagazineSize) return;

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        yield return new WaitForSeconds(reloadTime);

        currentMagazine = maxMagazineSize;
        hudController?.updateAmmo(currentMagazine, currentReserve);

        isReloading = false;
    }

    // Loop Control
    public void OnPackageDelivered()
    {
        if (isGameOver) return;

            int packagesToAdd = 1;

            if (GameSelection.SelectedPassive == "DELIVERY DUPLICATION")
            {
                if (Random.value <= 0.5f)
                {
                    packagesToAdd = 2;
                    hudController?.TriggerPopup("DELIVERY DUPLICATED", "Bonus delivery processed", new Color(51f, 51f, 51f, 0.30f), 3.0f);
                }
            }

            scoreManager.packagesDelivered += packagesToAdd;
            hudController?.updatePackagesDelivered(scoreManager.packagesDelivered);

            if (GameSelection.SelectedPassive == "HEAL ON DELIVERY")
            {
                int totalHeal = Mathf.RoundToInt(maxPlayerHealth * 0.25f) * packagesToAdd;
                Heal(totalHeal);
            }
    }

    public void OnEnemyDestroyed()
    {
        if (isGameOver) return;

        scoreManager.enemiesDestroyed++;

        if (GameSelection.SelectedPassive == "LIFESTEAL ON KILL")
        {
            Heal(Mathf.RoundToInt(maxPlayerHealth * 0.30f));
        }
    }

    public void OnActiveAbilityUsed() => scoreManager.activeAbilitiesUsed++;

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        scoreManager.calculateFinalScore();

        if (GameSelection.SelectedPassive == "XP MULTIPLIER")
        {
            scoreManager.finalScoreNumerical = Mathf.RoundToInt(scoreManager.finalScoreNumerical * 1.5f);
        }

        hudController?.GameEndTransition();
    }
}
