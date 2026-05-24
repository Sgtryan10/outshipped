using UnityEngine;
using UnityEngine.UIElements;

public class HUDArmorModule
{
    private VisualElement[] armorBars = new VisualElement[5];

    public HUDArmorModule(VisualElement container)
    {
        if (container == null) return;

        for (int i = 0; i < 5; i++)
        {
            armorBars[i] = container.Q<VisualElement>($"Armor{i + 1}");
        }
    }

    public void updateDisplay(int armorStacks)
    {
        armorStacks = Mathf.Clamp(armorStacks, 0, 5);

        for (int i = 0; i < armorBars.Length; i++)
        {
            if (armorBars[i] == null) continue;

            if (i < armorStacks)
            {
                armorBars[i].style.visibility = Visibility.Visible;
            }
            else
            {
                armorBars[i].style.visibility = Visibility.Hidden;
            }
        }
    }
}
