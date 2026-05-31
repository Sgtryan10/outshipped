using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class scoringAnimations : MonoBehaviour
{
    [System.Serializable]
    public struct GradeBound
    {
        public string gradeName;
        public int minimumScore;
        public VectorImage image;
    }

    [Header("Score Configuration")]
    [Tooltip("Highest to Lowest")]
    [SerializeField] private List<GradeBound> gradeBounds;

    [SerializeField] private float scoreRollUpDuration = 1.5f;

    private VisualElement screenFader;
    private Button mainMenuButton;

    async void OnEnable()
    {
        // TEMPORARY MOCK DATA FOR TESTING
        scoreManager.finalScoreNumerical = 100001;
        scoreManager.packagesDelivered = 42;
        scoreManager.enemiesDestroyed = 117;
        scoreManager.activeAbilitiesUsed = 9;
        scoreManager.timeSurvivedSeconds = 385f;

        var root = GetComponent<UIDocument>().rootVisualElement;

        var scoreLetter = root.Q<VisualElement>("ScoreLetter");
        var scoreNumerical = root.Q<Label>("ScoreNumerical");

        var topBar = root.Q<VisualElement>("TopBar");
        var bottomBar = root.Q<VisualElement>("BottomBar");
        var selectionBox = root.Q<VisualElement>("ScoringTextBox");
        var selectionText = root.Q<VisualElement>("ScoringText");

        var scoringScoreContainer = root.Q<VisualElement>("ScoringScoreContainer");
        var mainMenuButton = root.Q<Button>("MainMenu");

        var scoringBoxContainer = root.Q<VisualElement>("ScoringBoxContainer");
        var packagesDeliveredLabel = root.Q<VisualElement>("PackagesDeliveredLabel");
        var packagesDeliveredText = root.Q<Label>("PackagesDelivered");
        var enemiesDestroyedLabel = root.Q<VisualElement>("EnemiesDestroyedLabel");
        var enemiesDestroyedText = root.Q<Label>("EnemiesDestroyed");
        var activeAbilitiesUsedLabel = root.Q<VisualElement>("ActiveAbilitiesUsedLabel");
        var activeAbilitiesUsedText = root.Q<Label>("ActiveAbilitiesUsed");
        var timeSurvivedLabel = root.Q<VisualElement>("TimeSurvivedLabel");
        var timeSurvivedText = root.Q<Label>("TimeSurvived");

        if (scoreNumerical != null) scoreNumerical.text = "0";
        if (packagesDeliveredText != null) packagesDeliveredText.text = "0";
        if (enemiesDestroyedText != null) enemiesDestroyedText.text = "0";
        if (activeAbilitiesUsedText != null) activeAbilitiesUsedText.text = "0";
        if (timeSurvivedText != null) timeSurvivedText.text = "00:00";

        screenFader = root.Q<VisualElement>("ScreenFader");

        updateGradeImage(scoreLetter, 0);

        if (mainMenuButton != null)
        {
            mainMenuButton.AddToClassList("fade-scale-element");
            mainMenuButton.AddToClassList("fade-scale-hidden");
            mainMenuButton.clicked -= OnMainMenuClicked;
            mainMenuButton.clicked += OnMainMenuClicked;
        }

        List<VisualElement> animatedElements = new List<VisualElement>()
        {
            scoringScoreContainer,
            scoringBoxContainer,
            packagesDeliveredLabel,
            enemiesDestroyedLabel,
            activeAbilitiesUsedLabel,
            timeSurvivedLabel
        };

        foreach (var element in animatedElements)
        {
            if (element == null) continue;
            element.AddToClassList("fade-scale-element");
            element.AddToClassList("fade-scale-hidden");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.AddToClassList("fade-scale-element");
            mainMenuButton.AddToClassList("fade-scale-hidden");
        }

        // Initial Delay
        await Task.Delay(2000);

        if (topBar != null) topBar.AddToClassList("top-bar-expanded");
        if (bottomBar != null) bottomBar.AddToClassList("bottom-bar-expanded");
        await Task.Delay(300);

        if (selectionBox != null) selectionBox.AddToClassList("scoring-text-box-expanded");
        await Task.Delay(250);

        if (selectionText != null) selectionText.AddToClassList("text-visible");
        if (selectionBox != null) selectionBox.AddToClassList("scoring-text-box-retracted");
        await Task.Delay(300);

        foreach (var element in animatedElements)
        {
            if (element == null) continue;
            element.RemoveFromClassList("fade-scale-hidden");
            element.AddToClassList("fade-scale-visible");
            await Task.Delay(80);
        }

        await Task.Delay(200);

        List<Task> rollUpTasks = new List<Task>()
        {
            rollUpScoreText(scoreNumerical, scoreLetter, scoreManager.finalScoreNumerical, scoreRollUpDuration),
            rollUpGenericText(packagesDeliveredText, scoreManager.packagesDelivered, scoreRollUpDuration),
            rollUpGenericText(enemiesDestroyedText, scoreManager.enemiesDestroyed, scoreRollUpDuration),
            rollUpGenericText(activeAbilitiesUsedText, scoreManager.activeAbilitiesUsed, scoreRollUpDuration),
            rollUpTimeText(timeSurvivedText, scoreManager.timeSurvivedSeconds, scoreRollUpDuration)
        };

        await Task.WhenAll(rollUpTasks);

        await Task.Delay(1000);

        if (mainMenuButton != null)
        {
            mainMenuButton.RemoveFromClassList("fade-scale-hidden");
            mainMenuButton.AddToClassList("fade-scale-visible");
        }
    }

    private async Task rollUpScoreText(Label scoreLabel, VisualElement scoreLetter, int targetScore, float duration)
    {
        if (scoreLabel == null || targetScore <= 0) return;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            int currentDisplayScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, progress));
            scoreLabel.text = currentDisplayScore.ToString();

            updateGradeImage(scoreLetter, currentDisplayScore);

            await Task.Yield();
        }

        scoreLabel.text = targetScore.ToString();
        updateGradeImage(scoreLetter, targetScore);
    }

    private async Task rollUpGenericText(Label label, int targetValue, float duration)
    {
        if (label == null || targetValue <= 0) return;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            int currentDisplayValue = Mathf.RoundToInt(Mathf.Lerp(0, targetValue, progress));
            label.text = currentDisplayValue.ToString();

            await Task.Yield();
        }

        label.text = targetValue.ToString();
    }

    private async Task rollUpTimeText(Label label, float targetSeconds, float duration)
    {
        if (label == null || targetSeconds <= 0) return;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            float currentSeconds = Mathf.Lerp(0f, targetSeconds, progress);
            int minutes = Mathf.FloorToInt(currentSeconds / 60f);
            int seconds = Mathf.FloorToInt(currentSeconds % 60f);

            label.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            await Task.Yield();
        }

        int finalMinutes = Mathf.FloorToInt(targetSeconds / 60f);
        int finalSeconds = Mathf.FloorToInt(targetSeconds % 60f);
        label.text = string.Format("{0:00}:{1:00}", finalMinutes, finalSeconds);
    }

    private void updateGradeImage(VisualElement scoreLetter, int currentScore)
    {
        if (scoreLetter == null || gradeBounds == null || gradeBounds.Count == 0) return;

        VectorImage assignedImage = null;

        foreach (var bound in gradeBounds)
        {
            if (currentScore >= bound.minimumScore)
            {
                assignedImage = bound.image;
                break;
            }
        }

        if (assignedImage == null)
        {
            assignedImage = gradeBounds[gradeBounds.Count - 1].image;
        }

        scoreLetter.style.backgroundImage = new StyleBackground(assignedImage);
    }

    void OnDisable()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.clicked -= OnMainMenuClicked;
        }
    }

    private async void OnMainMenuClicked()
    {
        if (screenFader != null)
        {
            screenFader.pickingMode = PickingMode.Position;

            screenFader.RemoveFromClassList("fader-hidden");
            screenFader.AddToClassList("fader-visible");
        }

        await Task.Delay(600);

        SceneManager.LoadScene("preGame");
    }
}
