using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    async void OnEnable()
    {
        scoreManager.finalScoreNumerical = 100001; // TESTING - REMOVE ASAP

        var root = GetComponent<UIDocument>().rootVisualElement;

        var scoreLetter = root.Q<VisualElement>("ScoreLetter");
        var scoreNumerical = root.Q<Label>("ScoreNumerical");

        if (scoreNumerical != null)
        {
            scoreNumerical.text = "0";
        }

        updateGradeImage(scoreLetter, 0);

        int targetScore = scoreManager.finalScoreNumerical;

        Task rollUpTask = rollUpScoreText(scoreNumerical, scoreLetter, targetScore, scoreRollUpDuration);

        var topBar = root.Q<VisualElement>("TopBar");
        var bottomBar = root.Q<VisualElement>("BottomBar");
        var selectionBox = root.Q<VisualElement>("ScoringTextBox");
        var selectionText = root.Q<VisualElement>("ScoringText");

        // Initial Delay
        await Task.Delay(2000);

        topBar.AddToClassList("top-bar-expanded");
        bottomBar.AddToClassList("bottom-bar-expanded");

        await Task.Delay(300);

        selectionBox.AddToClassList("scoring-text-box-expanded");

        await Task.Delay(250);

        selectionText.AddToClassList("text-visible");
        selectionBox.AddToClassList("scoring-text-box-retracted");

        await rollUpTask;
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
}
