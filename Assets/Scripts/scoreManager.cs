public static class scoreManager
{
    public static int finalScoreNumerical { get; set; } = 0; // Score = packagesDelivered * 2000 + enemiesDestroyed * 500 + activeAbilitiesUsed * 200 + timeSurvivedSeconds * 100
    public static int packagesDelivered { get; set; } = 0;
    public static int enemiesDestroyed { get; set; } = 0;
    public static int activeAbilitiesUsed { get; set; } = 0;
    public static float timeSurvivedSeconds { get; set; } = 0f;

    public static void calculateFinalScore()
    {
        finalScoreNumerical = (packagesDelivered * 2000) + (enemiesDestroyed * 500) + (activeAbilitiesUsed * 200) + ((int)timeSurvivedSeconds * 100);

        if (GameSelection.SelectedPassive == "XP MULTIPLIER")
        {
            finalScoreNumerical = UnityEngine.Mathf.RoundToInt(finalScoreNumerical * 1.5f);
        }
    }

    public static void resetScores()
    {
        finalScoreNumerical = 0;
        packagesDelivered = 0;
        enemiesDestroyed = 0;
        activeAbilitiesUsed = 0;
        timeSurvivedSeconds = 0f;
    }
}
