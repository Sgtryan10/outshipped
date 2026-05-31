using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;

public class settingsAnimations : MonoBehaviour
{
    private PreGameUIManager _uiManager;
    private VisualElement _resolutionContainer;
    private VisualElement _qualityContainer;

    private int _selectedWidth = 1920;
    private int _selectedHeight = 1080;
    private int _selectedQualityIndex = 3;

    void OnEnable()
    {
        _uiManager = Object.FindAnyObjectByType<PreGameUIManager>();

        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;

        _resolutionContainer = root.Q<VisualElement>("ResolutionContainer");
        _qualityContainer = root.Q<VisualElement>("QualityContainer");
        var miscContainer = root.Q<VisualElement>("MiscContainer");

        if (_resolutionContainer != null)
        {
            foreach (var child in _resolutionContainer.Children())
            {
                if (child is Button btn)
                {
                    btn.RegisterCallback<ClickEvent>(OnResolutionClick);
                }
            }
        }

        if (_qualityContainer != null)
        {
            foreach (var child in _qualityContainer.Children())
            {
                if (child is Button btn)
                {
                    btn.RegisterCallback<ClickEvent>(OnQualityClick);
                }
            }
        }

        if (miscContainer != null)
        {
            var applyBtn = miscContainer.Q<Button>(className: "apply");
            applyBtn?.RegisterCallback<ClickEvent>(evt => ApplySettings());

            var backBtn = miscContainer.Q<Button>(className: "back");
            backBtn?.RegisterCallback<ClickEvent>(evt => GoBack());
        }

        UpdateResolutionSelectionVisuals();
        UpdateQualitySelectionVisuals();
    }

    void OnDisable()
    {
        if (_resolutionContainer != null)
        {
            foreach (var child in _resolutionContainer.Children())
            {
                if (child is Button btn) btn.UnregisterCallback<ClickEvent>(OnResolutionClick);
            }
        }

        if (_qualityContainer != null)
        {
            foreach (var child in _qualityContainer.Children())
            {
                if (child is Button btn) btn.UnregisterCallback<ClickEvent>(OnQualityClick);
            }
        }
    }

    private void OnResolutionClick(ClickEvent evt)
    {
        if (evt.currentTarget is Button btn)
        {
            string[] dimensions = btn.name.Split('x');
            if (dimensions.Length == 2 && int.TryParse(dimensions[0], out int w) && int.TryParse(dimensions[1], out int h))
            {
                _selectedWidth = w;
                _selectedHeight = h;
                Debug.Log($"Resolution staged: {w}x{h}");
                UpdateResolutionSelectionVisuals();
            }
        }
    }

    private void OnQualityClick(ClickEvent evt)
    {
        if (evt.currentTarget is Button btn)
        {
            switch (btn.name)
            {
                case "VeryLow":  _selectedQualityIndex = 0; break;
                case "Low":      _selectedQualityIndex = 1; break;
                case "Medium":   _selectedQualityIndex = 2; break;
                case "High":     _selectedQualityIndex = 3; break;
                case "VeryHigh": _selectedQualityIndex = 4; break;
                case "Ultra":    _selectedQualityIndex = 5; break;
            }
            UpdateQualitySelectionVisuals();
        }
    }

    private void UpdateResolutionSelectionVisuals()
    {
        if (_resolutionContainer == null) return;

        string targetName = $"{_selectedWidth}x{_selectedHeight}";
        foreach (var child in _resolutionContainer.Children())
        {
            if (child is Button btn)
            {
                if (btn.name == targetName)
                    btn.AddToClassList("selected");
                else
                    btn.RemoveFromClassList("selected");
            }
        }
    }

    private void UpdateQualitySelectionVisuals()
    {
        if (_qualityContainer == null) return;

        string targetName = _selectedQualityIndex switch
        {
            0 => "VeryLow",
            1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "VeryHigh",
            5 => "Ultra",
            _ => "High"
        };

        foreach (var child in _qualityContainer.Children())
        {
            if (child is Button btn)
            {
                if (btn.name == targetName)
                    btn.AddToClassList("selected");
                else
                    btn.RemoveFromClassList("selected");
            }
        }
    }

    private void ApplySettings()
    {
        Screen.SetResolution(_selectedWidth, _selectedHeight, Screen.fullScreen);
        QualitySettings.SetQualityLevel(_selectedQualityIndex, true);
    }

    private void GoBack()
    {
        if (_uiManager != null)
        {
            _uiManager.ReturnToMainMenu(this.gameObject);
        }
    }
}
