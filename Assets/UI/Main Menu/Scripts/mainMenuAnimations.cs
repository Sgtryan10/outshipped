using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class mainMenuAnimations : MonoBehaviour
{
    public int staggerDelayMs = 500;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip hoverSound;

    async void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        var allButtons = root.Query<Button>().ToList();
        foreach (var btn in allButtons)
        {
            btn.RegisterCallback<ClickEvent>(OnButtonClick);
            btn.RegisterCallback<MouseEnterEvent>(OnButtonHover);
        }

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

        // Buttons Animation
        foreach (var button in container.Children())
        {
            button.RemoveFromClassList("fade-start-button");
            button.AddToClassList("fade-end-button");

            await Task.Delay(staggerDelayMs);
        }
    }

    private void OnButtonClick(ClickEvent evt)
    {
        PlaySound(clickSound);
    }

    private void OnButtonHover(MouseEnterEvent evt)
    {
        PlaySound(hoverSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
