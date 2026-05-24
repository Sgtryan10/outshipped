using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public class HudController : MonoBehaviour
{
    private UIDocument uiDoc;
    private VisualElement root;

    private HUDHealthModule healthModule;
    private HUDAmmoModule ammoModule;
    private HUDArmorModule armorModule;
    private HUDCourierAndPassiveModule courierAndPassiveModule;
    private HUDPackagesModule packagesModule;
    private HUDActiveAbilityModule activeAbilityModule;

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
        }

        await Task.Delay(100);
        if (this == null || root == null) return;

        root.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50), Length.Percent(50)));
        root.RemoveFromClassList("hud-hidden");
    }

    private void initializeModules()
    {
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        var visualRoot = uiDoc.rootVisualElement.Q<VisualElement>("Root");
        if (visualRoot == null) return;

        healthModule = new HUDHealthModule(visualRoot.Q<VisualElement>("HealthContainer"));
        ammoModule = new HUDAmmoModule(visualRoot.Q<VisualElement>("AmmoContainer"));
        armorModule = new HUDArmorModule(visualRoot.Q<VisualElement>("ArmorContainer"));
        courierAndPassiveModule = new HUDCourierAndPassiveModule(visualRoot.Q<VisualElement>("LeftContainer"), visualRoot.Q<VisualElement>("RightContainer"));
        packagesModule = new HUDPackagesModule(visualRoot.Q<VisualElement>("TopContainer"));
        activeAbilityModule = new HUDActiveAbilityModule(visualRoot.Q<VisualElement>("ActiveContainer")); // Initialized here
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
}
