using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class gameManager : MonoBehaviour
{
    public static gameManager Instance { get; private set; }

    [System.Serializable]
    public struct TurretFireSFX
    {
        public string turretType;
        public AudioClip fireSound;
    }

    [System.Serializable]
    public struct TurretReloadSFX
    {
        public string turretType;
        public AudioClip reloadSound;
    }

    [Header("References")]
    [SerializeField] private HudController hudController;
    [SerializeField] private CarController playerCar;
    [SerializeField] private TurretController turretController;
    [SerializeField] private CinemachineCamera vCam;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip ampedPickupSFX;
    [SerializeField] private AudioClip overdrivePickupSFX;
    [SerializeField] private AudioClip empPickupSFX;
    [SerializeField] private AudioClip slowPickupSFX;
    [SerializeField] private AudioClip armorPickupSFX;
    [SerializeField] private AudioClip abilityUseSFX;
    [SerializeField] private AudioClip empBlastSFX;

    [SerializeField] private List<TurretFireSFX> turretFireSounds;
    [SerializeField] private AudioClip defaultFireSFX;

    [SerializeField] private List<TurretReloadSFX> turretReloadSounds;
    [SerializeField] private AudioClip defaultReloadSFX;

    [SerializeField] private List<AudioClip> damageSFXList;

    [Header("Player Health & Armor Settings")]
    [SerializeField] private int maxPlayerHealth = 100;
    private int currentPlayerHealth;
    private int currentArmorStacks = 0;

    [Header("Weapon & Ammo Settings")]
    [SerializeField] private int maxMagazineSize = 30;

    [Header("Camera & FOV Settings")]
    [SerializeField] private float overdriveFOV = 80f;
    private float baseFOV = 60f;
    [SerializeField] private float fovTransitionSpeed = 100f;

    [Header("Vignette Settings")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float activeVignetteIntensity = 0.45f;
    [SerializeField] private float activeVignetteSmoothness = 0.5f;
    [SerializeField] private float vignetteTransitionDuration = 0.5f;

    [Header("Ability Vignette Colors")]
    [SerializeField] private Color ampedVignetteColor = new Color(1f, 0.37254902f, 0f);
    [SerializeField] private Color overdriveVignetteColor = new Color(0.960784314f, 0.764705882f, 0.278431373f);
    [SerializeField] private Color empVignetteColor = new Color(0.454901961f, 0.721568627f, 0.937254902f);
    [SerializeField] private Color slowVignetteColor = new Color(0.435294118f, 0.482352941f, 0.968627451f);

    [Header("Ability Visual Effects")]
    [SerializeField] private ParticleSystem slowFieldParticles;
    [SerializeField] private ParticleSystem empFieldEffect;

    private Vignette vignette;
    private Color baseVignetteColor;
    private float baseVignetteIntensity;
    private float baseVignetteSmoothness;
    private Coroutine vignetteTransitionCoroutine;

    private string currentTurretType;
    private int currentMagazine;
    private int currentReserve;
    private int currentBaseDamage;
    private float currentRange;
    public float CurrentRange => currentRange;
    private Color currentTracerColor;
    public Color CurrentTracerColor => currentTracerColor;

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

    private bool wantsToShoot;

    private float originalMaxSpeed;
    private float originalAcceleration;

    private string storedAbility = "";

    private bool isGameOver = false;

    private Dictionary<string, AudioClip> fireSoundLookup = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> reloadSoundLookup = new Dictionary<string, AudioClip>();

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

        InitializeFireSounds();
        InitializeReloadSounds();
    }

    private void InitializeFireSounds()
    {
        fireSoundLookup.Clear();
        if (turretFireSounds == null) return;

        foreach (var entry in turretFireSounds)
        {
            if (!string.IsNullOrEmpty(entry.turretType) && !fireSoundLookup.ContainsKey(entry.turretType))
            {
                fireSoundLookup.Add(entry.turretType, entry.fireSound);
            }
        }
    }

    private void InitializeReloadSounds()
    {
        reloadSoundLookup.Clear();
        if (turretReloadSounds == null) return;

        foreach (var entry in turretReloadSounds)
        {
            if (!string.IsNullOrEmpty(entry.turretType) && !reloadSoundLookup.ContainsKey(entry.turretType))
            {
                reloadSoundLookup.Add(entry.turretType, entry.reloadSound);
            }
        }
    }

    void Start()
    {
        scoreManager.resetScores();
        isGameOver = false;
        Time.timeScale = 1f;

        if (playerCar == null)
            playerCar = FindAnyObjectByType<CarController>();

        if (playerCar != null)
        {
            originalMaxSpeed = playerCar.MaxSpeed;
            originalAcceleration = playerCar.Acceleration;
        }

        if (turretController == null)
            turretController = FindAnyObjectByType<TurretController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (vCam != null)
        {
            baseFOV = vCam.Lens.FieldOfView;
        }

        if (globalVolume != null && globalVolume.profile.TryGet<Vignette>(out vignette))
        {
            baseVignetteColor = vignette.color.value;
            baseVignetteIntensity = vignette.intensity.value;
            baseVignetteSmoothness = vignette.smoothness.value;

            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
        }

        currentPlayerHealth = maxPlayerHealth;
        currentArmorStacks = 0;

        currentTurretType = GameSelection.SelectedTurretType;

        maxMagazineSize = GameSelection.SelectedMagazine;
        currentMagazine = GameSelection.SelectedMagazine;
        currentReserve = GameSelection.SelectedReserve;

        fireRate = GameSelection.SelectedFireRate;
        reloadTime = GameSelection.SelectedReloadTime;
        currentBaseDamage = GameSelection.SelectedDamage;
        currentRange = GameSelection.SelectedRange;
        currentTracerColor = GameSelection.SelectedTracerColor;

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

            turretController.SetBaseDamage(currentBaseDamage);
            turretController.SetMaxRange(currentRange);
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
                turretController?.Fire(damageMultiplier);
                PlayFiringSound();
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

    private void PlayFiringSound()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = defaultFireSFX;

        if (!string.IsNullOrEmpty(currentTurretType) && fireSoundLookup.TryGetValue(currentTurretType, out AudioClip typeSpecificClip))
        {
            if (typeSpecificClip != null)
            {
                clipToPlay = typeSpecificClip;
            }
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void PlayReloadSound()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = defaultReloadSFX;

        if (!string.IsNullOrEmpty(currentTurretType) && reloadSoundLookup.TryGetValue(currentTurretType, out AudioClip typeSpecificClip))
        {
            if (typeSpecificClip != null)
            {
                clipToPlay = typeSpecificClip;
            }
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    public void StoreAbility(string abilityName)
    {
        if (isGameOver) return;
        storedAbility = abilityName;

        hudController?.updateActiveAbility(
            abilityName == "EMP",
            abilityName == "OVERDRIVE",
            abilityName == "AMPED",
            abilityName == "SLOW"
        );

        PlayPickupSound(abilityName);

        if (hudController != null)
        {
            switch (abilityName)
            {
                case "AMPED":
                    hudController.TriggerPopup("AMPED TURRET", "Press 'E' for 150% Damage (20s).", new Color(1f, 0.37254902f, 0f), 3.0f);
                    break;
                case "OVERDRIVE":
                    hudController.TriggerPopup("OVERDRIVE ACQUIRED", "Press 'E' for 150% Speed (20s).", new Color(1f, 0.37254902f, 0f), 3.0f);
                    break;
                case "EMP":
                    hudController.TriggerPopup("EMP ONLINE", "Press 'E' to discharge EMP pulse.", new Color(1f, 0.37254902f, 0f), 3.0f);
                    break;
                case "SLOW":
                    hudController.TriggerPopup("SLOW FIELD READY", "Press 'E' to create a slowing field (20s).", new Color(1f, 0.37254902f, 0f), 3.0f);
                    break;
            }
        }
    }

    private void PlayPickupSound(string abilityName)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = null;

        switch (abilityName)
        {
            case "AMPED":
                clipToPlay = ampedPickupSFX;
                break;
            case "OVERDRIVE":
                clipToPlay = overdrivePickupSFX;
                break;
            case "EMP":
                clipToPlay = empPickupSFX;
                break;
            case "SLOW":
                clipToPlay = slowPickupSFX;
                break;
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    private void UseStoredAbility()
    {
        if (string.IsNullOrEmpty(storedAbility)) return;

        if (audioSource != null && abilityUseSFX != null)
        {
            audioSource.PlayOneShot(abilityUseSFX);
        }

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

    private void StartVignetteTransition(Color targetColor, float targetIntensity, float targetSmoothness)
    {
        if (vignette == null) return;

        if (vignetteTransitionCoroutine != null)
        {
            StopCoroutine(vignetteTransitionCoroutine);
        }
        vignetteTransitionCoroutine = StartCoroutine(TransitionVignetteRoutine(targetColor, targetIntensity, targetSmoothness));
    }

    private IEnumerator TransitionVignetteRoutine(Color targetColor, float targetIntensity, float targetSmoothness)
    {
        Color startColor = vignette.color.value;
        float startIntensity = vignette.intensity.value;
        float startSmoothness = vignette.smoothness.value;
        float elapsed = 0f;

        while (elapsed < vignetteTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vignetteTransitionDuration;

            vignette.color.value = Color.Lerp(startColor, targetColor, t);
            vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            vignette.smoothness.value = Mathf.Lerp(startSmoothness, targetSmoothness, t);

            yield return null;
        }

        vignette.color.value = targetColor;
        vignette.intensity.value = targetIntensity;
        vignette.smoothness.value = targetSmoothness;
    }

    private IEnumerator AmpedRoutine()
    {
        ampedActive = true;
        damageMultiplier = 1.5f;
        hudController?.updateActiveAbility(false, false, false, false);

        StartVignetteTransition(ampedVignetteColor, activeVignetteIntensity, activeVignetteSmoothness);

        yield return new WaitForSeconds(20f);

        damageMultiplier = 1f;
        ampedActive = false;

        StartVignetteTransition(baseVignetteColor, baseVignetteIntensity, baseVignetteSmoothness);
    }

    private IEnumerator OverdriveRoutine()
    {
        overdriveActive = true;
        speedMultiplier = 1.5f;

        if (playerCar != null)
        {
            playerCar.SetOverdriveSpeeds(originalMaxSpeed * 1.5f, originalAcceleration * 1.5f);
        }

        StartVignetteTransition(overdriveVignetteColor, activeVignetteIntensity, activeVignetteSmoothness);

        if (vCam != null)
        {
            while (!Mathf.Approximately(vCam.Lens.FieldOfView, overdriveFOV))
            {
                vCam.Lens.FieldOfView = Mathf.MoveTowards(vCam.Lens.FieldOfView, overdriveFOV, fovTransitionSpeed * Time.deltaTime);
                yield return null;
            }
        }

        hudController?.updateActiveAbility(false, false, false, false);
        yield return new WaitForSeconds(20f);

        speedMultiplier = 1f;
        overdriveActive = false;

        if (playerCar != null)
        {
            playerCar.SetOverdriveSpeeds(originalMaxSpeed, originalAcceleration);
        }

        StartVignetteTransition(baseVignetteColor, baseVignetteIntensity, baseVignetteSmoothness);

        if (vCam != null)
        {
            while (!Mathf.Approximately(vCam.Lens.FieldOfView, baseFOV))
            {
                vCam.Lens.FieldOfView = Mathf.MoveTowards(vCam.Lens.FieldOfView, baseFOV, fovTransitionSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    private IEnumerator EmpRoutine()
    {
        empActive = true;
        hudController?.updateActiveAbility(false, false, false, false);

        StartVignetteTransition(empVignetteColor, activeVignetteIntensity, activeVignetteSmoothness);

        if (empBlastSFX != null)
        {
            audioSource.PlayOneShot(empBlastSFX);
        }

        yield return new WaitForSeconds(1.5f);
        if (empFieldEffect != null)
        {
            empFieldEffect.Play();
        }

        yield return new WaitForSeconds(1f);
        empActive = false;

        StartVignetteTransition(baseVignetteColor, baseVignetteIntensity, baseVignetteSmoothness);
    }

    private IEnumerator SlowRoutine()
    {
        slowActive = true;
        hudController?.updateActiveAbility(false, false, false, false);

        StartVignetteTransition(slowVignetteColor, activeVignetteIntensity, activeVignetteSmoothness);

        if (slowFieldParticles != null)
        {
            slowFieldParticles.Play();
        }

        yield return new WaitForSeconds(20f);
        slowActive = false;

        if (slowFieldParticles != null)
        {
            slowFieldParticles.Stop();
        }

        StartVignetteTransition(baseVignetteColor, baseVignetteIntensity, baseVignetteSmoothness);
    }

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

        if (audioSource != null && armorPickupSFX != null)
        {
            audioSource.PlayOneShot(armorPickupSFX);
        }

        hudController?.TriggerPopup("ARMOR REINFORCED", "Armor plating nullifies one damage instance.", new Color(0.2f, 0.2f, 0.2f), 3.0f);
    }

    public void AddAmmo(int amount)
    {
        if (isGameOver) return;
        currentReserve += amount;
        hudController?.updateAmmo(currentMagazine, currentReserve);
    }

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
        PlayReloadSound();
        yield return new WaitForSeconds(reloadTime);
        currentMagazine = maxMagazineSize;
        hudController?.updateAmmo(currentMagazine, currentReserve);
        isReloading = false;
    }

    public void TriggerPackagePickupPopup()
    {
        if (isGameOver) return;

        hudController?.TriggerPopup("PACKAGE SECURED", "Proceed to the nearest Depot.", new Color(0f, 0.8f, 0.6f), 3.0f);
    }

    public void OnPackageDelivered()
    {
        if (isGameOver) return;

        int packagesToAdd = 1;

        if (GameSelection.SelectedPassive == "DELIVERY DUPLICATION")
        {
            if (Random.value <= 0.5f)
            {
                packagesToAdd = 2;
                hudController?.TriggerPopup("DELIVERY DUPLICATED", "Bonus delivery processed.", new Color(0.2f, 0.2f, 0.2f), 3.0f);

                scoreManager.packagesDelivered += packagesToAdd;
                hudController?.updatePackagesDelivered(scoreManager.packagesDelivered);
                if (GameSelection.SelectedPassive == "HEAL ON DELIVERY")
                {
                    Heal(Mathf.RoundToInt(maxPlayerHealth * 0.25f) * packagesToAdd);
                }
                return;
            }
        }

        hudController?.TriggerPopup("DELIVERY COMPLETE", "Package delivered successfully.", new Color(0.2f, 0.8f, 0.2f), 3.0f);

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

    public void PlayRandomDamageSound()
    {
        if (audioSource == null || damageSFXList == null || damageSFXList.Count == 0) return;

        int randomIndex = Random.Range(0, damageSFXList.Count);
        AudioClip randomClip = damageSFXList[randomIndex];

        if (randomClip != null)
        {
            audioSource.PlayOneShot(randomClip);
        }
    }

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
