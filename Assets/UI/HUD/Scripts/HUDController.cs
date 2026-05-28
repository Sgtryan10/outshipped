using UnityEngine;
using UnityEngine.UIElements;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    private UIDocument uiDoc;
    private VisualElement root;

    private CancellationTokenSource popupTokenSource;

    private HUDHealthModule healthModule;
    private HUDAmmoModule ammoModule;
    private HUDArmorModule armorModule;
    private HUDCourierAndPassiveModule courierAndPassiveModule;
    private HUDPackagesModule packagesModule;
    private HUDActiveAbilityModule activeAbilityModule;
    private HUDPopupModule popupModule;

    private VisualElement screenFadeOverlay;

    [Header("Editor Testing")]
    public int testMaxHealth = 100;
    [Range(0, 10000)] public int testHealth = 100;

    public string testTurretType = "TURRET";
    [Range(0, 10000)] public int testMagazine = 30;
    [Range(0, 10000)] public int testReserve = 90;

    [Range(0, 5)] public int testArmorStacks = 5;

    public string testCourier = "COURIER";
    public string testPassive = "PASSIVE";

    [Range(0, 10000)] public int testPackagesDelivered = 0;

    public bool testEMP = false;
    public bool testOverdrive = false;
    public bool testAmped = false;
    public bool testSlow = false;

    public bool testShowPopup = false;
    public string testPopupTitle = "POPUP";
    public string testPopupDescription = "POPUP DESCRIPTION";
    public Color testPopupColor = new Color(1f, 0f, 0f, 0.3f);
    public float testPopupDuration = 3.0f;

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        if (uiDoc == null) uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        initializeModules();

        var visualRoot = uiDoc.rootVisualElement.Q<VisualElement>("Root");
        if (visualRoot != null)
        {
            visualRoot.RemoveFromClassList("hud-hidden");
        }

        // Editor Testing Stats
        healthModule?.setMaxHealth(testMaxHealth);
        healthModule?.updateDisplay(testHealth);

        ammoModule?.setTurretType(testTurretType);
        ammoModule?.updateDisplay(testMagazine, testReserve);

        armorModule?.updateDisplay(testArmorStacks);

        courierAndPassiveModule?.setCourier(testCourier);
        courierAndPassiveModule?.setPassive(testPassive);

        packagesModule?.updateDisplay(testPackagesDelivered);

        activeAbilityModule?.updateDisplay(testEMP, testOverdrive, testAmped, testSlow);

        if (testShowPopup)
        {
            popupModule?.Show(testPopupTitle, testPopupDescription, testPopupColor);
        }
        else
        {
            popupModule?.Hide();
        }
    }

    private void Awake()
    {
        uiDoc = GetComponent<UIDocument>();
    }

    private async void OnEnable()
    {
        if (uiDoc == null) uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        // Initial Delay
        await Task.Delay(2000);

        if (this == null || uiDoc == null || uiDoc.rootVisualElement == null) return;

        root = uiDoc.rootVisualElement.Q<VisualElement>("Root");
        if (root == null) return;

        initializeModules();

        if (Application.isPlaying)
        {
            // Runtime Stats
            setMaxHealth(testMaxHealth);
            updateHealth(testHealth);

            setTurretType(testTurretType);
            updateAmmo(testMagazine, testReserve);

            updateArmor(testArmorStacks);

            setCourier(testCourier);
            setPassive(testPassive);

            updatePackagesDelivered(testPackagesDelivered);

            updateActiveAbility(testEMP, testOverdrive, testAmped, testSlow);

            if (testShowPopup)
            {
                TriggerPopup(testPopupTitle, testPopupDescription, testPopupColor, testPopupDuration);
            }
            else
            {
                DismissPopup();
            }
        }

        await Task.Delay(100);
        if (this == null || root == null) return;

        root.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50), Length.Percent(50)));
        root.RemoveFromClassList("hud-hidden");
    }

    private void OnDisable()
    {
        CancelActivePopupTimer();
    }

    private void initializeModules()
    {
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        var visualRoot = uiDoc.rootVisualElement.Q<VisualElement>("Root");
        if (visualRoot == null) return;

        screenFadeOverlay = visualRoot.Q<VisualElement>("ScreenFadeOverlay");

        healthModule = new HUDHealthModule(visualRoot.Q<VisualElement>("HealthContainer"));
        ammoModule = new HUDAmmoModule(visualRoot.Q<VisualElement>("AmmoContainer"));
        armorModule = new HUDArmorModule(visualRoot.Q<VisualElement>("ArmorContainer"));
        courierAndPassiveModule = new HUDCourierAndPassiveModule(visualRoot.Q<VisualElement>("LeftContainer"), visualRoot.Q<VisualElement>("RightContainer"));
        packagesModule = new HUDPackagesModule(visualRoot.Q<VisualElement>("TopContainer"));
        activeAbilityModule = new HUDActiveAbilityModule(visualRoot.Q<VisualElement>("ActiveContainer"));
        popupModule = new HUDPopupModule(visualRoot.Q<VisualElement>("PopupContainer"));
    }

    // Health Module Calls
    public void setMaxHealth(int maxHealth)
    {
        if (!Application.isPlaying) return;
        testMaxHealth = maxHealth;
        healthModule?.setMaxHealth(maxHealth);
    }

    public void updateHealth(int health)
    {
        if (!Application.isPlaying) return;
        testHealth = health;
        healthModule?.updateDisplay(health);
    }


    // Ammo Module Calls
    public void setTurretType(string turretType)
    {
        if (!Application.isPlaying) return;
        testTurretType = turretType;
        ammoModule?.setTurretType(turretType);
    }

    public void updateAmmo(int magazine, int reserve)
    {
        if (!Application.isPlaying) return;
        testMagazine = magazine;
        testReserve = reserve;
        ammoModule?.updateDisplay(magazine, testReserve);
    }

    // Armor Module Calls
    public void updateArmor(int armorStacks)
    {
        if (!Application.isPlaying) return;
        testArmorStacks = armorStacks;
        armorModule?.updateDisplay(armorStacks);
    }

    // Courier And Passive Module Calls
    public void setCourier(string courier)
    {
        if (!Application.isPlaying) return;
        testCourier = courier;
        courierAndPassiveModule?.setCourier(courier);
    }

    public void setPassive(string passive)
    {
        if (!Application.isPlaying) return;
        testPassive = passive;
        courierAndPassiveModule?.setPassive(passive);
    }

    // Packages Module Calls
    public void updatePackagesDelivered(int packagesDelivered)
    {
        if (!Application.isPlaying) return;
        testPackagesDelivered = packagesDelivered;
        packagesModule?.updateDisplay(packagesDelivered);
    }

    // Active Ability Module Calls
    public void updateActiveAbility(bool emp, bool overdrive, bool amped, bool slow)
    {
        if (!Application.isPlaying) return;
        testEMP = emp;
        testOverdrive = overdrive;
        testAmped = amped;
        testSlow = slow;
        activeAbilityModule?.updateDisplay(emp, overdrive, amped, slow);
    }

    // Popup Module Calls
    public void TriggerPopup(string title, string description, Color boxColor, float durationSeconds)
    {
        if (!Application.isPlaying) return;

        CancelActivePopupTimer();

        testShowPopup = true;
        testPopupTitle = title;
        testPopupDescription = description;
        testPopupColor = boxColor;
        testPopupDuration = durationSeconds;

        popupModule?.Show(title, description, boxColor);

        popupTokenSource = new CancellationTokenSource();
        StartPopupTimer(durationSeconds, popupTokenSource.Token);
    }

    private async void StartPopupTimer(float delayInSeconds, CancellationToken token)
    {
        try
        {
            int delayMs = Mathf.RoundToInt(delayInSeconds * 1000f);
            await Task.Delay(delayMs, token);

            if (!token.IsCancellationRequested)
            {
                DismissPopup();
            }
        }
        catch (TaskCanceledException)
        {

        }
    }

    private void CancelActivePopupTimer()
    {
        if (popupTokenSource != null)
        {
            popupTokenSource.Cancel();
            popupTokenSource.Dispose();
            popupTokenSource = null;
        }
    }

    public void DismissPopup()
    {
        if (!Application.isPlaying) return;
        testShowPopup = false;

        CancelActivePopupTimer();
        popupModule?.Hide();
    }

    // Game End Transition Call
    public async void GameEndTransition()
    {
        if (!Application.isPlaying) return;

        if (root != null)
        {
            root.AddToClassList("hud-hidden");
        }

        if (screenFadeOverlay != null)
        {
            screenFadeOverlay.AddToClassList("fade-black");
        }

        await Task.Delay(600);

        if (this == null) return;

        scoreManager.finalScoreNumerical = 100001; // TESTING - CHANGE SO THIS ACTUALLY REFLECTS REAL VALUE
        SceneManager.LoadScene("PostGame");
    }
}
