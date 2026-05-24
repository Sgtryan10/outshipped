using UnityEngine;
using UnityEngine.UIElements;

public class HUDPackagesModule
{
    private Label packagesLabel;

    public HUDPackagesModule(VisualElement container)
    {
        if (container == null) return;

        packagesLabel = container.Q<Label>("TopPackages");
    }

    public void updateDisplay(int packagesDelivered)
    {
        if (packagesLabel != null) packagesLabel.text = packagesDelivered.ToString();
    }
}
