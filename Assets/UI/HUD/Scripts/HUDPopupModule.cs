using UnityEngine;
using UnityEngine.UIElements;

public class HUDPopupModule
{
    private VisualElement container;
    private VisualElement popupBox;
    private Label popupLabel;
    private Label descriptionLabel;

    public HUDPopupModule(VisualElement container)
    {
        this.container = container;
        if (container == null) return;

        popupBox = container.Q<VisualElement>("PopupBox");
        popupLabel = container.Q<Label>("Popup");
        descriptionLabel = container.Q<Label>("PopupDescription");

        container.AddToClassList("popup-container");

        HideImmediate();
    }

    public void Show(string title, string description, Color boxColor)
    {
        if (container == null) return;

        if (popupLabel != null) popupLabel.text = title;
        if (descriptionLabel != null) descriptionLabel.text = description;
        if (popupBox != null)
        {
            boxColor.a = 0.3f;

            popupBox.style.unityBackgroundImageTintColor = new StyleColor(boxColor);

            popupBox.style.backgroundColor = StyleKeyword.Null;
        }

        container.RemoveFromClassList("popup-hidden");
    }

    public void Hide()
    {
        if (container == null) return;

        container.AddToClassList("popup-hidden");
    }

    private void HideImmediate()
    {
        container.AddToClassList("popup-hidden");
    }
}
