using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace BannerWandRetro.Models
{
    /// <summary>
    /// Custom XP model that multiplies skill XP gains for player and targeted NPCs.
    /// Extends <see cref="GenericXpModel"/> to add cheat functionality without Harmony patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all XP calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Unlimited Skill XP: Returns 999,999x multiplier (effectively instant leveling)
    /// - Skill XP Multiplier: Returns configured multiplier value (2x, 5x, etc.)
    /// </para>
    /// <para>
    /// Target support:
    /// - Applies to player if ApplyToPlayer enabled
    /// - Applies to player clan members if ApplyToPlayerClanMembers enabled
    /// - Applies to other NPCs based on target settings (vassals, kingdom members, etc.)
    /// </para>
    /// <para>
    /// XP system in Bannerlord:
    /// - Skills gain XP through various gameplay activities
    /// - This model's GetXpMultiplier is called whenever XP is awarded
    /// - Multiplier scales the XP gain (1.0 = normal, 2.0 = double, etc.)
    /// </para>
    /// <para>
    /// IMPORTANT: This model may not exist in all Bannerlord versions.
    /// The game engine gracefully handles registration failures (see <see cref="Core.SubModule.RegisterCustomModels"/>).
    /// For versions without this model, <see cref="Behaviors.SkillXPCheatBehavior"/> provides a fallback.
    /// </para>
    /// </remarks>
    public class CustomGenericXpModel : GenericXpModel
    {
        // Constants moved to GameConstants for consistency

        /// <summary>
        /// The default XP multiplier (no boost).
        /// </summary>
        private const float DefaultMultiplier = 1.0f;

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Gets the XP multiplier for a hero with cheat overrides applied.
        /// Overrides the abstract method from <see cref="GenericXpModel"/>.
        /// </summary>
        /// <param name="hero">The hero gaining XP. Cannot be null.</param>
        /// <returns>
        /// The XP multiplier (1.0 = normal, higher values = faster leveling).
        /// Returns 999 for unlimited XP mode, configured value for multiplier mode, or 1.0 for normal.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Multiplier logic:
        /// 1. Check if hero should receive cheat via TargetFilter
        /// 2. If Unlimited Skill XP enabled: Return 999x (rapid skill progression)
        /// 3. If Skill XP Multiplier &gt; 1: Return configured multiplier
        /// 4. Otherwise: Return 1.0 (normal XP gain)
        /// </para>
        /// <para>
        /// Why 999 for unlimited:
        /// - Typical skill XP gains: 1-100 per action
        /// - Skills require hundreds of thousands of XP to max
        /// - 999x multiplier provides very fast but visible progression
        /// - Balanced value that feels rewarding without being instant
        /// </para>
        /// <para>
        /// Performance: Called frequently during gameplay (every XP-earning action).
        /// Optimized with early returns and cached setting accesses.
        /// </para>
        /// </remarks>
        public override float GetXpMultiplier(Hero hero)
        {
            try
            {                // Early exit for null parameters
                if (hero == null)
                {
                    return DefaultMultiplier;
                }

                // Early exit for null or unconfigured settings
                if (Settings == null || TargetSettings == null)
                {
                    return DefaultMultiplier;
                }

                // Check if this hero should receive XP cheats
                if (!TargetFilter.ShouldApplyCheat(hero))
                {
                    return DefaultMultiplier;
                }

                // Apply skill XP multiplier cheats based on settings priority
                float multiplier = Settings.UnlimitedSkillXP
                    ? GameConstants.UnlimitedXpMultiplier
                    : Settings.SkillXPMultiplier > DefaultMultiplier ? Settings.SkillXPMultiplier : DefaultMultiplier;

                return multiplier;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomGenericXpModel] Error in GetXpMultiplier: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return DefaultMultiplier;
            }
        }
    }
}
