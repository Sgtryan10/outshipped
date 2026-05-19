using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class selectionScreenAnimations : MonoBehaviour
{
    private VisualElement[] columns;
    private VisualElement topBar;
    private VisualElement bottomBar;
    private VisualElement fadeOverlay;

    async void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        fadeOverlay = root.Q<VisualElement>("FadeOverlay");
        if (fadeOverlay != null)
        {
            fadeOverlay.pickingMode = PickingMode.Ignore;
        }

        // Initial Delay
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

        var workhorse = root.Q<VisualElement>("WorkhorseContainer");
        var cruiser = root.Q<VisualElement>("CruiserContainer");
        var freighter = root.Q<VisualElement>("FreighterContainer");
        var monarch = root.Q<VisualElement>("MonarchContainer");

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
        }
    }

    private async void OnColumnClicked(ClickEvent evt)
    {
        VisualElement clickedColumn = evt.currentTarget as VisualElement;

        if (clickedColumn == null) return;

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
    }
}
