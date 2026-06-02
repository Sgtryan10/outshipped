using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public static class GameSelection
{
    public static string SelectedCourier { get; set; } = "WORKHORSE";
    public static string SelectedPassive { get; set; } = "HEAL ON DELIVERY";
    public static int SelectedMagazine { get; set; } = 30;
    public static int SelectedReserve { get; set; } = 30;
    public static string SelectedTurretType { get; set; } = "AUTOMATIC";
    public static float SelectedFireRate { get; set; } = 0.2f;
    public static float SelectedReloadTime { get; set; } = 1.5f;
}

public class selectionScreenAnimations : MonoBehaviour
{
    private VisualElement[] columns;
    private VisualElement topBar;
    private VisualElement bottomBar;
    private VisualElement fadeOverlay;

    private VisualElement workhorse;
    private VisualElement cruiser;
    private VisualElement freighter;
    private VisualElement monarch;

    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip hoverSound;

    async void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        var allButtons = root.Query<Button>().ToList();
        foreach (var btn in allButtons)
        {
            btn.RegisterCallback<ClickEvent>(OnButtonClick);
            btn.RegisterCallback<MouseEnterEvent>(OnButtonHover);
        }

        fadeOverlay = root.Q<VisualElement>("FadeOverlay");
        if (fadeOverlay != null)
        {
            fadeOverlay.pickingMode = PickingMode.Ignore;
        }

        await Task.Delay(2000);

        topBar = root.Q<VisualElement>("TopBar");
        bottomBar = root.Q<VisualElement>("BottomBar");

        topBar.AddToClassList("top-bar-expanded");
        bottomBar.AddToClassList("bottom-bar-expanded");

        await Task.Delay(300);

        var courierBox = root.Q<VisualElement>("CourierBox");
        var courier = root.Q<VisualElement>("Courier");

        courierBox.AddToClassList("courier-box-expanded");

        await Task.Delay(250);

        courier.AddToClassList("courier-visible");
        courierBox.AddToClassList("courier-box-retracted");

        await Task.Delay(200);

        workhorse = root.Q<VisualElement>("WorkhorseContainer");
        cruiser = root.Q<VisualElement>("CruiserContainer");
        freighter = root.Q<VisualElement>("FreighterContainer");
        monarch = root.Q<VisualElement>("MonarchContainer");

        workhorse.RemoveFromClassList("column-hidden");
        await Task.Delay(200);

        cruiser.RemoveFromClassList("column-hidden");
        await Task.Delay(200);

        freighter.RemoveFromClassList("column-hidden");
        await Task.Delay(200);

        monarch.RemoveFromClassList("column-hidden");

        columns = new VisualElement[] { workhorse, cruiser, freighter, monarch };

        foreach (var col in columns)
        {
            col.RegisterCallback<ClickEvent>(OnColumnClicked);
            col.RegisterCallback<MouseEnterEvent>(OnColumnHover);
        }
    }

    private async void OnColumnClicked(ClickEvent evt)
    {
        VisualElement clickedColumn = evt.currentTarget as VisualElement;

        if (clickedColumn == null) return;

        PlaySound(clickSound);

        if (clickedColumn == workhorse)
        {
            GameSelection.SelectedCourier = "WORKHORSE";
            GameSelection.SelectedPassive = "HEAL ON DELIVERY";
            GameSelection.SelectedMagazine = 30;
            GameSelection.SelectedReserve = 30;
            GameSelection.SelectedTurretType = "AUTOMATIC";
            GameSelection.SelectedFireRate = 0.2f;
            GameSelection.SelectedReloadTime = 1.5f;
        }
        else if (clickedColumn == cruiser)
        {
            GameSelection.SelectedCourier = "CRUISER";
            GameSelection.SelectedPassive = "LIFESTEAL ON KILL";
            GameSelection.SelectedMagazine = 10;
            GameSelection.SelectedReserve = 10;
            GameSelection.SelectedTurretType = "BUCKSHOT";
            GameSelection.SelectedFireRate = 0.6f;
            GameSelection.SelectedReloadTime = 2.0f;
        }
        else if (clickedColumn == freighter)
        {
            GameSelection.SelectedCourier = "FREIGHTER";
            GameSelection.SelectedPassive = "DELIVERY DUPLICATION";
            GameSelection.SelectedMagazine = 5;
            GameSelection.SelectedReserve = 5;
            GameSelection.SelectedTurretType = "PIERCING";
            GameSelection.SelectedFireRate = 1.0f;
            GameSelection.SelectedReloadTime = 3.0f;
        }
        else if (clickedColumn == monarch)
        {
            GameSelection.SelectedCourier = "MONARCH";
            GameSelection.SelectedPassive = "XP MULTIPLIER";
            GameSelection.SelectedMagazine = 45;
            GameSelection.SelectedReserve = 45;
            GameSelection.SelectedTurretType = "RAPID-FIRE";
            GameSelection.SelectedFireRate = 0.08f;
            GameSelection.SelectedReloadTime = 1.2f;
        }

        clickedColumn.parent?.RemoveFromClassList("allow-hover");

        topBar?.RemoveFromClassList("top-bar-expanded");
        bottomBar?.RemoveFromClassList("bottom-bar-expanded");

        foreach (var col in columns)
        {
            if (col == clickedColumn)
            {
                col.RemoveFromClassList("column-collapsed");
                col.AddToClassList("column-expanded");
            }
            else
            {
                col.RemoveFromClassList("column-expanded");
                col.AddToClassList("column-collapsed");
            }
        }

        await Task.Delay(2000);

        if (fadeOverlay != null)
        {
            fadeOverlay.pickingMode = PickingMode.Position;
            fadeOverlay.AddToClassList("fade-overlay-active");
        }

        await Task.Delay(1000);

        SceneManager.LoadScene("InGame");
    }

    private void OnColumnHover(MouseEnterEvent evt)
    {
        if (columns != null && evt.currentTarget is VisualElement col)
        {
            if (col.parent != null && col.parent.ClassListContains("allow-hover"))
            {
                PlaySound(hoverSound);
            }
        }
    }

    private void OnButtonClick(ClickEvent evt)
    {
        PlaySound(clickSound);
    }

    private void OnButtonHover(MouseEnterEvent evt)
    {
        PlaySound(hoverSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
