using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class manualAnimations : MonoBehaviour
{
    private PreGameUIManager _uiManager;
    private VisualElement screenFader;
    private Button mainMenuButton;

    void OnEnable() {
        _uiManager = Object.FindAnyObjectByType<PreGameUIManager>();

        var root = GetComponent<UIDocument>().rootVisualElement;

        mainMenuButton = root.Q<Button>("MainMenu");

        screenFader = root.Q<VisualElement>("ScreenFader");

        if (mainMenuButton != null)
        {
            mainMenuButton.AddToClassList("fade-scale-element");

            mainMenuButton.RegisterCallback<ClickEvent>(OnMainMenuClicked);

            mainMenuButton.AddToClassList("fade-scale-visible");
        }
    }

    void OnDisable()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.UnregisterCallback<ClickEvent>(OnMainMenuClicked);
        }
    }

    private void OnMainMenuClicked(ClickEvent evt)
    {
        if (_uiManager != null)
        {
            _uiManager.ReturnToMainMenu(this.gameObject);
        }
    }
}
