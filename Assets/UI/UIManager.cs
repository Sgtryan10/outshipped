using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public struct MenuMapping
    {
        public string buttonName;
        public GameObject menuGO;
    }

    [Header("Main References")]
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject mainMenu;

    [Header("Menu Transitions")]
    [SerializeField] private List<MenuMapping> menuMappings;

    private bool _hasTransitionedFromTitle = false;

    async void Start()
    {
        if (titleScreen != null) titleScreen.SetActive(true);
        if (mainMenu != null) mainMenu.SetActive(false);

        foreach (var map in menuMappings)
        {
            if (map.menuGO != null) map.menuGO.SetActive(false);
        }
    }

    public void OnSubmit(InputValue value)
    {
        if (!_hasTransitionedFromTitle && value.isPressed)
        {
            _ = StartGameSequence();
        }
    }

    public async Task StartGameSequence()
    {
        _hasTransitionedFromTitle = true;

        await TransitionEffect(titleScreen, mainMenu);

        SetupMainMenuEvents();
    }

    private void SetupMainMenuEvents()
    {
        var root = mainMenu.GetComponent<UIDocument>().rootVisualElement;

        foreach (var mapping in menuMappings)
        {
            Button btn = root.Q<Button>(mapping.buttonName);
            if (btn != null)
            {
                var targetMenu = mapping.menuGO;

                btn.clicked += () => _ = TransitionEffect(mainMenu, targetMenu);
            }
        }
    }

    private async Task TransitionEffect(GameObject from, GameObject to)
    {
        if (from == null || to == null) return;

        var fromRoot = from.GetComponent<UIDocument>().rootVisualElement;

        to.SetActive(true);

        var toRoot = to.GetComponent<UIDocument>().rootVisualElement;

        toRoot.AddToClassList("fade-out");
        fromRoot.AddToClassList("fade-out");

        await Task.Yield();

        toRoot.RemoveFromClassList("fade-out");

        await Task.Delay(750);

        from.SetActive(false);

        fromRoot.RemoveFromClassList("fade-out");
    }
}
