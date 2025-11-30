#nullable enable
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party speed model that handles player movement speed override and AI slowdown.
    /// Extends <see cref="DefaultPartySpeedCalculatingModel"/> to modify party movement speeds.
    /// </summary>
    public class CustomPartySpeedModel : DefaultPartySpeedCalculatingModel
    {
        private CheatSettings? Settings => CheatSettings.Instance;
        private CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Calculates the base movement speed for a party.
        /// Overrides player speed if enabled, applies AI slowdown for hostile parties.
        /// </summary>
        public override ExplainedNumber CalculateBaseSpeed(
            MobileParty mobileParty,
            bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0,
            int additionalTroopOnHorseCount = 0)
        {
            try
            {
                // Early exit for null settings
                if (Settings == null || TargetSettings == null || mobileParty == null)
                {
                    return base.CalculateBaseSpeed(mobileParty, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);
                }

                // Check if player speed override is enabled
                if (ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    // Create new ExplainedNumber with custom Base value
                    ExplainedNumber customSpeed = new(Settings.MovementSpeed, includeDescriptions);

                    // Log for debugging (only for player, only once per session to avoid spam)
                    if (mobileParty == MobileParty.MainParty)
                    {
                        ModLogger.Debug($"Movement Speed override applied: Base +{Settings.MovementSpeed} (Player Party)");
                    }

                    return customSpeed;
                }

                // Get base speed from default implementation
                ExplainedNumber baseSpeed = base.CalculateBaseSpeed(
                    mobileParty,
                    includeDescriptions,
                    additionalTroopOnFootCount,
                    additionalTroopOnHorseCount);

                // Apply AI slowdown if enabled
                if (ShouldApplyAiSlowdown(mobileParty))
                {
                    ApplyAiSlowdown(baseSpeed, mobileParty);
                }

                return baseSpeed;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartySpeedModel] Exception in CalculateBaseSpeed: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.CalculateBaseSpeed(mobileParty, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);
            }
        }

        /// <summary>
        /// Determines if player speed override should be applied.
        /// </summary>
        private bool ShouldApplyPlayerSpeedOverride(MobileParty mobileParty)
        {
            return Settings!.MovementSpeed > 0f &&
                   mobileParty == MobileParty.MainParty &&
                   TargetSettings!.ApplyToPlayer;
        }

        /// <summary>
        /// Determines if AI slowdown should be applied to the party.
        /// </summary>
        private bool ShouldApplyAiSlowdown(MobileParty mobileParty)
        {
            if (!Settings!.SlowAIMovementSpeed || mobileParty == MobileParty.MainParty)
            {
                return false;
            }

            // TEMPORARY: Log party type
            string partyName = mobileParty.Name?.ToString() ?? "Unknown";
            bool isBandit = mobileParty.IsBandit;
            bool hasFaction = mobileParty.MapFaction != null;

            if (hasFaction && Hero.MainHero?.MapFaction != null)
            {
                bool isAtWar = FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, Hero.MainHero.MapFaction);

                // Log only non-bandits to see what's being checked
                if (!isBandit)
                {
                    ModLogger.Log($"[AI Slowdown Check] {partyName}: Faction={mobileParty.MapFaction?.Name}, AtWar={isAtWar}");
                }

                return isAtWar;
            }

            return isBandit;
        }

        /// <summary>
        /// Applies AI slowdown to the party speed.
        /// Reduces FINAL speed to 50% (half speed) using multiplicative factor.
        /// </summary>
        private void ApplyAiSlowdown(ExplainedNumber baseSpeed, MobileParty mobileParty)
        {
            string partyName = mobileParty.Name?.ToString() ?? "Unknown";
            float speedBefore = baseSpeed.ResultNumber;

            // Try method 1: AddFactor
            baseSpeed.AddFactor(-0.5f, new TaleWorlds.Localization.TextObject("AI Slowdown"));
            float speedAfterFactor = baseSpeed.ResultNumber;

            // Log detailed info
            ModLogger.Log($"[AI Slowdown] {partyName}: Before={speedBefore:F2}, After AddFactor(-0.5)={speedAfterFactor:F2}, Expected={speedBefore * 0.5f:F2}");
        }
    }
}
