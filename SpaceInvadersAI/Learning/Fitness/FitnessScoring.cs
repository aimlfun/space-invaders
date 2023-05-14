using SpaceInvadersAI.AI;
using SpaceInvadersCore.Game;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersAI.Learning.Fitness;

/// <summary>
/// Static library with method(s) to determine the fitness of a player.
/// </summary>
internal static class FitnessScoring
{
    //  █████    ███    █████   █   █   █████    ███     ███             ███     ███     ███    ████     ███    █   █    ████
    //  █         █       █     █   █   █       █   █   █   █           █   █   █   █   █   █   █   █     █     █   █   █
    //  █         █       █     ██  █   █       █       █               █       █       █   █   █   █     █     ██  █   █
    //  ████      █       █     █ █ █   ████     ███     ███             ███    █       █   █   ████      █     █ █ █   █
    //  █         █       █     █  ██   █           █       █               █   █       █   █   █ █       █     █  ██   █  ██
    //  █         █       █     █   █   █       █   █   █   █           █   █   █   █   █   █   █  █      █     █   █   █   █
    //  █        ███      █     █   █   █████    ███     ███             ███     ███     ███    █   █    ███    █   █    ████
    
    /// <summary>
    /// Determines the fitness of a player.
    /// </summary>
    internal static float GetFitness(GameController controller, out string fitnessExplained)
    {
        if (controller.Score == 0)
        {
            fitnessExplained = "0 score => 0 fitness";
            return -1; // no score? => it's an immediate failure
        }

        float fitness = controller.Score * FitnessScoreMultipliers.Settings.ScoreMultiplier // score is quite important e.g. 0..9999x10
                        + controller.NumberOfInvadersKilled * FitnessScoreMultipliers.Settings.InvaderMultiplier
                        + controller.NumberOfSaucersKilled * FitnessScoreMultipliers.Settings.SaucerMultiplier
                        + (controller.Level - 1) * FitnessScoreMultipliers.Settings.LevelMultiplier // the more levels completed the better. Hitting the saucer gets more points, but could end up killing you.
                        + (controller.Shots == 0 ? 0 : FitnessScoreMultipliers.Settings.AccuracyMultiplier * (float)controller.NumberOfInvadersKilled / (float)controller.Shots) // but we need to encourage accuracy too 0..10
                        + controller.KillsAvoided * FitnessScoreMultipliers.Settings.KillsAvoidedMultiplier  // avoiding death is vital to high scores, so we reward it.
                        - controller.NumberOfTimesShieldsWereShotByPlayer * FitnessScoreMultipliers.Settings.ShieldsShotMultiplier // we don't want the player to shoot the shields. 
                        + controller.Lives * FitnessScoreMultipliers.Settings.LivesMultiplier - // how much is a life worth?        
                        - (controller.InvadersReachedBottom ? FitnessScoreMultipliers.Settings.PunishmentForInvadersReachingBottom : 0);

        // the below MUST align to the above, unless you want to be confused by it!

        // provide details as to how one derived it, for debugging purposes.
        fitnessExplained = $"[score] {controller.Score} * {FitnessScoreMultipliers.Settings.ScoreMultiplier} \n" +
            $"+ [invaders] {controller.NumberOfInvadersKilled} * {FitnessScoreMultipliers.Settings.InvaderMultiplier}\n" +
            $"+ [saucers] {controller.NumberOfSaucersKilled} * {FitnessScoreMultipliers.Settings.SaucerMultiplier}\n" +
            $"+ [level] ({controller.Level}-1) * {FitnessScoreMultipliers.Settings.LevelMultiplier}\n" +
            $"+ [accuracy] {(controller.Shots == 0 ? 0 : FitnessScoreMultipliers.Settings.AccuracyMultiplier * (float)controller.NumberOfInvadersKilled / (float)controller.Shots)}" +
            $"+ [kills-avoided] {controller.KillsAvoided} * {FitnessScoreMultipliers.Settings.KillsAvoidedMultiplier}\n" +
            $"- [shields-hit] {controller.NumberOfTimesShieldsWereShotByPlayer} * {FitnessScoreMultipliers.Settings.ShieldsShotMultiplier}\n" +
            $"+ [lives] {controller.Lives} * {FitnessScoreMultipliers.Settings.LivesMultiplier} \n" +
            $"- [invaders-reached-bottom] {controller.InvadersReachedBottom} ? {FitnessScoreMultipliers.Settings.PunishmentForInvadersReachingBottom} : 0 = {fitness}\n";

        return fitness;
    }
}