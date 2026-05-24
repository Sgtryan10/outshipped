using UnityEngine;
using UnityEngine.UIElements;

public class HUDAmmoModule
{
    private Label turretTypeLabel;
    private Label magazineLabel;
    private Label reserveLabel;

    public HUDAmmoModule(VisualElement container)
    {
        if (container == null) return;

        turretTypeLabel = container.Q<Label>("TurretType");
        magazineLabel = container.Q<Label>("Magazine");
        reserveLabel = container.Q<Label>("Reserve");
    }

    public void setTurretType(string turretType) {
        if (turretType != null) turretTypeLabel.text = turretType;
    }

    public void updateDisplay(int magazine, int reserve)
    {
        if (magazineLabel != null) magazineLabel.text = magazine.ToString();
        if (reserveLabel != null) reserveLabel.text = reserve.ToString();
    }
}
