using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("UI GameObjects")]
    [SerializeField] private GameObject titleScreenGO;
    [SerializeField] private GameObject mainMenuGO;

    [Header("Settings")]
    [SerializeField] private int initialDelayMs = 2000;

    private bool _hasTransitioned = false;

    async void Start()
    {
        if (titleScreenGO != null) titleScreenGO.SetActive(true);
        if (mainMenuGO != null) mainMenuGO.SetActive(false);

        await Task.Delay(initialDelayMs);
    }

    public void OnSubmit(InputValue value)
    {
        if (!_hasTransitioned && value.isPressed)
        {
            _ = TransitionSequence();
        }
    }

    private async Task TransitionSequence()
    {
        _hasTransitioned = true;

        var titleDoc = titleScreenGO.GetComponent<UIDocument>();
        var titleRoot = titleDoc.rootVisualElement;

        var menuDoc = mainMenuGO.GetComponent<UIDocument>();
        var menuRoot = menuDoc.rootVisualElement;

        mainMenuGO.SetActive(true);

        titleRoot.AddToClassList("fade-out");

        await Task.Delay(1000);

        titleScreenGO.SetActive(false);
        Debug.Log("titleScreen to mainMenu transition");
    }
}
