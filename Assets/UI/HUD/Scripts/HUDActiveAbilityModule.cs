using UnityEngine;
using UnityEngine.UIElements;

public class HUDActiveAbilityModule
{
    private VisualElement empElement;
    private VisualElement overdriveElement;
    private VisualElement ampedElement;
    private VisualElement slowElement;

    public HUDActiveAbilityModule(VisualElement container)
    {
        if (container == null) return;

        empElement = container.Q<VisualElement>("EMP");
        overdriveElement = container.Q<VisualElement>("Overdrive");
        ampedElement = container.Q<VisualElement>("Amped");
        slowElement = container.Q<VisualElement>("Slow");

        if (empElement != null) empElement.style.opacity = 0f;
        if (overdriveElement != null) overdriveElement.style.opacity = 0f;
        if (ampedElement != null) ampedElement.style.opacity = 0f;
        if (slowElement != null) slowElement.style.opacity = 0f;
    }

    public void updateDisplay(bool empActive, bool overdriveActive, bool ampedActive, bool slowActive)
    {
        if (empElement != null) empElement.style.opacity = empActive ? 1f : 0f;
        if (overdriveElement != null) overdriveElement.style.opacity = overdriveActive ? 1f : 0f;
        if (ampedElement != null) ampedElement.style.opacity = ampedActive ? 1f : 0f;
        if (slowElement != null) slowElement.style.opacity = slowActive ? 1f : 0f;
    }
}
