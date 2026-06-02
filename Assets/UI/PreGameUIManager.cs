using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PreGameUIManager : MonoBehaviour
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
    private bool _isTransitioning = false;

    async void Start()
    {
        if (titleScreen != null) titleScreen.SetActive(true);
        if (mainMenu != null) mainMenu.SetActive(false);

        foreach (var map in menuMappings)
        {
            if (map.menuGO != null) map.menuGO.SetActive(false);
        }

        await Task.Yield();
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
    }

    public void ReturnToMainMenu(GameObject currentSubMenu)
    {
        _ = TransitionEffect(currentSubMenu, mainMenu);
    }

    private void SetupMainMenuEvents()
    {
        var uiDoc = mainMenu.GetComponent<UIDocument>();
        if (uiDoc == null) return;

        var root = uiDoc.rootVisualElement;
        if (root == null) return;

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
        if (_isTransitioning) return;
        if (from == null || to == null) return;

        _isTransitioning = true;

        try
        {
            var fromPanelRoot = from.GetComponent<UIDocument>().rootVisualElement;
            var fromRoot = fromPanelRoot.Q("Root") ?? fromPanelRoot;

            to.SetActive(true);

            if (to == mainMenu)
            {
                SetupMainMenuEvents();
            }

            var toPanelRoot = to.GetComponent<UIDocument>().rootVisualElement;
            var toRoot = toPanelRoot.Q("Root") ?? toPanelRoot;
            toRoot.RemoveFromClassList("fade-out");

            var fromUiDoc = from.GetComponent<UIDocument>();
            var toUiDoc = to.GetComponent<UIDocument>();

            float baseSortOrder = fromUiDoc.sortingOrder;
            toUiDoc.sortingOrder = baseSortOrder - 1f;

            await Task.Yield();

            fromRoot.AddToClassList("fade-out");

            await Task.Delay(750);

            from.SetActive(false);
            fromRoot.RemoveFromClassList("fade-out");

            toUiDoc.sortingOrder = baseSortOrder;
        }
        finally
        {
            _isTransitioning = false;
        }
    }
}
