using System.Text.Json;

namespace SpaceInvadersAI.Learning.Fitness;

/// <summary>
/// These determine how we rate the AI, aka the fitness score.
/// </summary>
internal class FitnessScoreMultipliers
{
    //  █████    ███    █████   █   █   █████    ███     ███            █   █   █   █   █       █████    ███    ████    █        ███    █████   ████     ███
    //  █         █       █     █   █   █       █   █   █   █           ██ ██   █   █   █         █       █     █   █   █         █     █       █   █   █   █
    //  █         █       █     ██  █   █       █       █               █ █ █   █   █   █         █       █     █   █   █         █     █       █   █   █
    //  ████      █       █     █ █ █   ████     ███     ███            █ █ █   █   █   █         █       █     ████    █         █     ████    ████     ███
    //  █         █       █     █  ██   █           █       █           █   █   █   █   █         █       █     █       █         █     █       █ █         █
    //  █         █       █     █   █   █       █   █   █   █           █   █   █   █   █         █       █     █       █         █     █       █  █    █   █
    //  █        ███      █     █   █   █████    ███     ███            █   █    ███    █████     █      ███    █       █████    ███    █████   █   █    ███
    
    /// <summary>
    /// Singleton containing the settings for the fitness scores.
    /// </summary>
    internal static FitnessScoreMultipliers Settings = new();

    #region MULTIPLIERS CONFIGURED VIA THE UI
    // Encouraging it to select the best AI player is important.
    // The tricky part is determining what really matters. Ultimately the game is about the score. But scoring a point that gets you
    // killed isn't a good style, so we try to apply encouragement in the form of "kills avoided" (which means they were within proximity
    // of a bullet, and if they had not moved, they would have been killed.
    // In the early rounds, having more lives should be beneficial. 
    // Measuring kills vs. shots rewards accuracy, ideally because every shot counts.

    /// <summary>
    /// Each scored point is multiplied by this value. Ultimately the goal is to complete as many levels as possible, and of course you
    /// cannot do it without score increasing. In theory 990 points is the maximum score per level, but if you hit saucers, you can get
    /// even more. Players can hit several saucers, and exceed 990 
    /// </summary>
    public float ScoreMultiplier { get; set; } = 1 / 990f;

    /// <summary>
    /// The more levels, the better. It's hard to hit that pesky last alien, so we reward players who get there.
    /// </summary>
    public float LevelMultiplier { get; set; } = 100;

    /// <summary>
    /// We count invaders destroyed.
    /// </summary>
    public float InvaderMultiplier { get; set; } = 10;

    /// <summary>
    /// What extra points it gets for a saucer.
    /// </summary>
    public float SaucerMultiplier { get; set; } = 0.5f;

    /// <summary>
    /// The more accurate at shooting aliens the better. One wants to avoid wasting shots, esp. as the aliens get lower.
    /// </summary>
    public float AccuracyMultiplier { get; set; } = 1;

    /// <summary>
    /// Avoiding kills gets you bonus points. The player will get targeted, so being able to avoid death is essential to get to the next level.
    /// </summary>
    public float KillsAvoidedMultiplier { get; set; } = 0.1f;

    /// <summary>
    /// Shooting your shields may seem dumb, but the AI doesn't actually need them. They can actually be a hindrance, so we have a choice to reward or punish the AI.
    /// Note the "indicator" of where the shields are (+1 vs -1) enables the AI to know it is under a shield, and can avoid shooting it. But in reality the
    /// shield may offer no protection because the invaders have destroyed it. So the AI needs to learn over time when there is a hole or not.
    /// </summary>
    public float ShieldsShotMultiplier { get; set; } = -0.1f;

    /// <summary>
    /// For every life you have at the end of the game, you get rewarded. This is only really useful in the early levels.
    /// </summary>
    public float LivesMultiplier { get; set; } = 0; // 0.33f;
    
    /// <summary>
    /// Allow punishment to discourage AI choosing to let aliens reach the bottom.
    /// </summary>
    public float PunishmentForInvadersReachingBottom { get; set;} = 0;
    #endregion

    /// <summary>
    /// Serialises the settings to JSON for saving to disk.
    /// </summary>
    /// <returns></returns>
    private static string Serialise()
    {
        // create "Settings" object as JSON and return it
        return JsonSerializer.Serialize(Settings);
    }

    /// <summary>
    /// De-serialises the settings from JSON for loading from disk.
    /// </summary>
    /// <param name="JSONAsString"></param>
    private static void Deserialise(string JSONAsString)
    {
        // create "Settings" object from JSON and return it
        FitnessScoreMultipliers? settings = JsonSerializer.Deserialize<FitnessScoreMultipliers>(JSONAsString);

        // de-serialise can result in a null object, so only assign if it's not null
        if (settings is not null) Settings = settings;
    }

    /// <summary>
    /// Loads the settings from a file.
    /// </summary>
    /// <param name="filename"></param>
    internal static void Load(string filename)
    {
        if (File.Exists(filename))
        {
            Deserialise(File.ReadAllText(filename));
        }
    }

    /// <summary>
    /// Saves the settings to a file.
    /// </summary>
    /// <param name="filename"></param>
    internal static void Save(string filename)
    {
        File.WriteAllText(filename, Serialise());
    }
}