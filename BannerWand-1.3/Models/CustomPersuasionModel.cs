using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.GameComponents;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom persuasion model that makes all persuasion checks result in critical success for player.
    /// Extends <see cref="DefaultPersuasionModel"/> to add cheat functionality without Harmony patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all persuasion calculations.
    /// </para>
    /// <para>
    /// Cheat feature provided:
    /// - Persuasion Always Succeed: Forces all persuasion attempts to result in CRITICAL SUCCESS
    /// - Critical success means all 4 persuasion slots turn green immediately (instant win)
    /// </para>
    /// <para>
    /// Persuasion system in Bannerlord:
    /// - Used in dialogue to convince NPCs (recruit lords, resolve quests, etc.)
    /// - Each persuasion has 4 attempts (4 slots)
    /// - Critical Success instantly fills all slots green and completes persuasion
    /// - Regular Success fills one slot green
    /// - Failure/Critical Failure fills red slots
    /// </para>
    /// <para>
    /// Two methods are overridden for maximum compatibility:
    /// - <see cref="GetChances"/> - Primary method, forces 100% critical success chance
    /// - <see cref="GetDifficulty"/> - Fallback method, returns 0 for easiest difficulty
    /// </para>
    /// </remarks>
    public class CustomPersuasionModel : DefaultPersuasionModel
    {
        /// <summary>
        /// Perfect success chance (100%).
        /// </summary>
        private const float PerfectSuccessChance = 1f;

        /// <summary>
        /// No failure chance (0%).
        /// </summary>
        private const float NoFailureChance = 0f;

        /// <summary>
        /// Easiest persuasion difficulty (0).
        /// </summary>
        private const float EasiestDifficulty = 0f;

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Gets the probability chances for all persuasion outcomes, with cheat override for critical success.
        /// Overrides <see cref="DefaultPersuasionModel.GetChances"/>.
        /// </summary>
        /// <param name="optionArgs">Arguments for the persuasion option (traits, difficulty, characters).</param>
        /// <param name="successChance">Output: Probability of regular success (0-1). Set to 1.0 for guaranteed success.</param>
        /// <param name="critSuccessChance">Output: Probability of critical success (0-1). Set to 1.0 for guaranteed critical success.</param>
        /// <param name="critFailChance">Output: Probability of critical failure (0-1). Set to 0.0 to prevent critical failure.</param>
        /// <param name="failChance">Output: Probability of regular failure (0-1). Set to 0.0 to prevent failure.</param>
        /// <param name="difficultyMultiplier">Difficulty multiplier based on game settings (affects base calculations).</param>
        /// <remarks>
        /// <para>
        /// This is the PRIMARY method for implementing "Persuasion Always Succeed" cheat.
        /// When enabled, it forces CRITICAL SUCCESS on every persuasion attempt.
        /// </para>
        /// <para>
        /// Persuasion outcome types:
        /// - Critical Success (all 4 slots green instantly): Completes persuasion immediately
        /// - Success (one green slot): Partial progress toward persuasion completion
        /// - Failure (one red slot): No progress, wastes an attempt
        /// - Critical Failure (dark red slot): Negative progress, wastes an attempt
        /// </para>
        /// <para>
        /// Cheat strategy:
        /// - Set critSuccessChance to 100% (1.0)
        /// - Set successChance to 100% (1.0) as fallback
        /// - Set failChance to 0%
        /// - Set critFailChance to 0%
        /// - Result: All 4 persuasion slots turn green immediately on first attempt
        /// </para>
        /// <para>
        /// This matches WeMod's behavior where all slots become green instantly.
        /// </para>
        /// </remarks>
        public override void GetChances(
            PersuasionOptionArgs optionArgs,
            out float successChance,
            out float critSuccessChance,
            out float critFailChance,
            out float failChance,
            float difficultyMultiplier)
        {
            try
            {                // Check if cheat should be applied
                if (ShouldApplyPersuasionCheat())
                {
                    // Force CRITICAL SUCCESS
                    successChance = PerfectSuccessChance;
                    critSuccessChance = PerfectSuccessChance;
                    critFailChance = NoFailureChance;
                    failChance = NoFailureChance;
                    return;
                }

                // Use default chance calculation when cheat disabled
                base.GetChances(
                    optionArgs,
                    out successChance,
                    out critSuccessChance,
                    out critFailChance,
                    out failChance,
                    difficultyMultiplier);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPersuasionModel] Error in GetChances: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");

                // Fallback to safe defaults
                successChance = 0.5f;
                critSuccessChance = 0f;
                critFailChance = 0f;
                failChance = 0.5f;
            }
        }

        /// <summary>
        /// Gets the difficulty value for a persuasion attempt, with cheat override for automatic success.
        /// Overrides <see cref="DefaultPersuasionModel.GetDifficulty"/>.
        /// </summary>
        /// <param name="difficulty">The base difficulty level from the dialogue/quest.</param>
        /// <returns>
        /// The difficulty value (0 = easiest/automatic success, higher values = harder).
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is a FALLBACK for older Bannerlord versions or edge cases.
        /// The primary cheat implementation is in <see cref="GetChances"/>.
        /// </para>
        /// <para>
        /// Difficulty override strategy:
        /// - Normal persuasion: Difficulty ranges from easy (0.1-0.3) to very hard (0.7-1.0)
        /// - Cheat enabled: Always return 0 for easiest possible difficulty
        /// - Base implementation not called when cheat active (no need for default calculation)
        /// </para>
        /// <para>
        /// Why return 0:
        /// - Bannerlord's persuasion system treats 0 as easiest difficulty
        /// - Increases success chances when combined with GetChances override
        /// - Provides backward compatibility with older game versions
        /// </para>
        /// </remarks>
        public override float GetDifficulty(PersuasionDifficulty difficulty)
        {
            try
            {
                return ShouldApplyPersuasionCheat() ? EasiestDifficulty : base.GetDifficulty(difficulty);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPersuasionModel] Error in GetDifficulty: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return 1f;
            }
        }

        /// <summary>
        /// Determines if the persuasion cheat should be applied.
        /// </summary>
        /// <returns>
        /// True if the persuasion cheat should be applied, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Persuasion cheat requires:
        /// 1. Settings are properly initialized
        /// 2. PersuasionAlwaysSucceed setting is enabled
        /// 3. ApplyToPlayer target setting is enabled
        /// </para>
        /// <para>
        /// This centralized check ensures consistent behavior across both
        /// GetChances and GetDifficulty methods.
        /// </para>
        /// </remarks>
        private bool ShouldApplyPersuasionCheat()
        {
            // Early return for null or unconfigured settings
            if (Settings == null || TargetSettings == null)
            {
                return false;
            }

            // Apply cheat for player persuasion attempts
            return Settings.PersuasionAlwaysSucceed && TargetSettings.ApplyToPlayer;
        }
    }
}
