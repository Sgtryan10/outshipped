using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class mainMenuAnimations : MonoBehaviour
{
    public int staggerDelayMs = 500;

    async void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        var divider = root.Q<VisualElement>("Divider");
        var container = root.Q<VisualElement>("ButtonContainer");

        // Initial Delay
        await Task.Delay(1250);

        // Title Image
        var logoCurtain = root.Q<VisualElement>("TitleImageContainer");


        if (logoCurtain != null)
        {
            logoCurtain.AddToClassList("curtain-parent-open");
        }

        await Task.Delay(500);

        // Divider
        if (divider != null)
        {
            divider.AddToClassList("divider-end");
        }

        if (container == null) return;

        await Task.Delay(500);

        // Buttons
        foreach (var button in container.Children())
        {
            button.RemoveFromClassList("fade-start");
            button.AddToClassList("fade-end");

            await Task.Delay(staggerDelayMs);
        }
    }
}
