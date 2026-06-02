using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public class titleScreenAnimations : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip interactionSound;

    async void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        // Initial Delay
        await Task.Delay(2000);

        // Outline
        var outline = root.Q<VisualElement>("Outline");

        if (outline != null)
        {
            outline.AddToClassList("outline-end");
        }

        await Task.Delay(750); // Transition Speed

        await Task.Delay(500);

        // Top Curtains
        var courierCurtain = root.Q<VisualElement>("CourierContainer");
        var osCurtain = root.Q<VisualElement>("OSContainer");


        if (courierCurtain != null)
        {
            courierCurtain.AddToClassList("curtain-parent-courier-open");
        }

        await Task.Delay(500);


        if (osCurtain != null)
        {
            osCurtain.AddToClassList("curtain-parent-os-open");
        }

        await Task.Delay(250);

        // Bottom Curtains
        var nominalCurtain = root.Q<VisualElement>("NominalContainer");
        var dateCurtain = root.Q<VisualElement>("DateContainer");

        if (nominalCurtain != null)
        {
            nominalCurtain.AddToClassList("curtain-parent-nominal-open");
        }

        await Task.Delay(500);


        if (dateCurtain != null)
        {
            dateCurtain.AddToClassList("curtain-parent-date-open");
        }

        await Task.Delay(250);

        // Main Title
        var mainTitle = root.Q<VisualElement>("MainTitle");

        if (mainTitle != null)
        {
            mainTitle.AddToClassList("main-title-end");
        }

        await Task.Delay(500);

        // Start Element Curtain
        var startCurtain = root.Q<VisualElement>("StartElements");

        if (startCurtain != null)
        {
            startCurtain.AddToClassList("curtain-parent-start-open");
        }

        await Task.Delay(250);

        // Start Text Pulse
        var startText = root.Q<VisualElement>("Start");

        if (startText != null)
        {
            startText.AddToClassList("start-visible");

            _ = PulseRoutine(startText);
        }
    }

    private void Update()
    {
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool leftClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (spacePressed || leftClickPressed)
        {
            PlayInteractionSound();
        }
    }

    private void PlayInteractionSound()
    {

        if (audioSource != null && interactionSound != null)
        {
            audioSource.PlayOneShot(interactionSound);
        }
    }

    private async Task PulseRoutine(VisualElement element)
    {
        element.RemoveFromClassList("start-dimmed");
        element.AddToClassList("start-visible");

        await Task.Delay(500);

        while (Application.isPlaying)
        {
            await Task.Delay(1000);

            element.RemoveFromClassList("start-visible");
            element.AddToClassList("start-dimmed");

            await Task.Delay(1500);

            element.RemoveFromClassList("start-dimmed");
            element.AddToClassList("start-visible");

            await Task.Delay(500);
        }
    }
}
