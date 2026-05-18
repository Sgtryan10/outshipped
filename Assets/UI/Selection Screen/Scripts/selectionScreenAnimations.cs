using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class selectionScreenAnimations : MonoBehaviour
{
    async void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        // Initial Delay
        await Task.Delay(2000);

        // Top and Bottom Bar
        var topBar = root.Q<VisualElement>("TopBar");
        var bottomBar = root.Q<VisualElement>("BottomBar");

        topBar.AddToClassList("top-bar-expanded");
        bottomBar.AddToClassList("bottom-bar-expanded");

        await Task.Delay(300);

        // Courier + Courier Box Fancy Animation
        var courierBox = root.Q<VisualElement>("CourierBox");
        var courier = root.Q<VisualElement>("Courier");

        courierBox.AddToClassList("courier-box-expanded");

        await Task.Delay(250);

        courier.AddToClassList("courier-visible");
        courierBox.AddToClassList("courier-box-retracted");

        await Task.Delay(200);

        // Box Fade
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
    }
}
