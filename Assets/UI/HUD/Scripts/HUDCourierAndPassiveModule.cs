using UnityEngine;
using UnityEngine.UIElements;

public class HUDCourierAndPassiveModule
{
    private Label courierLabel;
    private Label passiveLabel;

    public HUDCourierAndPassiveModule(VisualElement leftContainer, VisualElement rightContainer)
    {
        if (leftContainer == null || rightContainer == null) return;

        courierLabel = leftContainer.Q<Label>("Courier");
        passiveLabel = rightContainer.Q<Label>("Passive");
    }

    public void setCourier(string courier)
    {
        if (courier != null) courierLabel.text = courier;
    }

    public void setPassive(string passive)
    {
        if (passive != null) passiveLabel.text = passive;
    }
}
