public static class scoreManager
{
    public static int finalScoreNumerical { get; set; } = 0; // Score = packagesDelivered * 2000 + enemiesDestroyed * 500 + activeAbilitiesUsed * 200 + timeSurvivedSeconds * 100
    public static int packagesDelivered { get; set; } = 0;
    public static int enemiesDestroyed { get; set; } = 0;
    public static int activeAbilitiesUsed { get; set; } = 0;
    public static float timeSurvivedSeconds { get; set; } = 0f;
}
