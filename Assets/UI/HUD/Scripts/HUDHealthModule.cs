using UnityEngine;
using UnityEngine.UIElements;

public class HUDHealthModule
{
    private VisualElement healthBar;
    private Label healthLabel;
    private int maxHealth = 100;

    public HUDHealthModule(VisualElement container)
    {
        if (container == null) return;

        healthBar = container.Q<VisualElement>("HealthBar");
        healthLabel = container.Q<Label>("Health");

        if (healthBar != null)
        {
            healthBar.style.transformOrigin = new StyleTransformOrigin(
                new TransformOrigin(Length.Percent(0), Length.Percent(50))
            );
        }
    }

    public void setMaxHealth(int maxHealth) {
        this.maxHealth = Mathf.Max(1, maxHealth);
    }

    public void updateDisplay(int currentHealth)
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthLabel != null)
        {
            healthLabel.text = currentHealth.ToString();
        }

        if (healthBar != null)
        {
            float healthRatio = (float)currentHealth / maxHealth;
            healthBar.style.scale = new StyleScale(new Scale(new Vector2(healthRatio, 1f)));
        }
    }
}
