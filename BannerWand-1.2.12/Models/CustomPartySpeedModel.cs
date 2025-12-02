#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWandRetro.Models
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

                // Get base speed from default implementation FIRST to preserve all modifiers
                ExplainedNumber baseSpeed = base.CalculateBaseSpeed(
                    mobileParty,
                    includeDescriptions,
                    additionalTroopOnFootCount,
                    additionalTroopOnHorseCount);

                // Check if player speed override is enabled
                if (ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    ApplyPlayerSpeedBoost(ref baseSpeed);

                    // Log for debugging (only for player, only once per session to avoid spam)
                    if (mobileParty == MobileParty.MainParty)
                    {
                        ModLogger.Debug($"Movement Speed override applied: Result={baseSpeed.ResultNumber:F2} (Player Party)");
                    }
                }

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
                   TargetSettings!.ApplyToPlayer &&
                   Campaign.Current != null;
        }

        /// <summary>
        /// Applies player speed boost by calculating a multiplier factor.
        /// Preserves all terrain, composition, and other modifiers.
        /// </summary>
        private void ApplyPlayerSpeedBoost(ref ExplainedNumber speed)
        {
            float desiredSpeed = Settings!.MovementSpeed;
            float currentBaseSpeed = speed.BaseNumber;

            if (currentBaseSpeed > GameConstants.MinBaseSpeedThreshold)
            {
                // Calculate multiplier: desiredSpeed / currentBaseSpeed
                // Then add factor = (multiplier - 1.0) to get the desired result
                float multiplier = desiredSpeed / currentBaseSpeed;
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, new TaleWorlds.Localization.TextObject("BannerWand Speed Override"));
            }
            else
            {
                // If base speed is too low, set it directly using Add
                float adjustment = desiredSpeed - speed.ResultNumber;
                speed.Add(adjustment, new TaleWorlds.Localization.TextObject("BannerWand Speed Override"));
            }
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
            string partyName = mobileParty.Name?.ToString() ?? MessageConstants.UnknownPartyName;
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
            string partyName = mobileParty.Name?.ToString() ?? MessageConstants.UnknownPartyName;
            float speedBefore = baseSpeed.ResultNumber;

            // Apply slowdown factor (reduces speed to 50%)
            baseSpeed.AddFactor(GameConstants.AiSlowdownFactor, new TaleWorlds.Localization.TextObject("AI Slowdown"));
            float speedAfterFactor = baseSpeed.ResultNumber;

            // Log detailed info
            float expectedSpeed = speedBefore * (1.0f + GameConstants.AiSlowdownFactor);
            ModLogger.Log($"[AI Slowdown] {partyName}: Before={speedBefore:F2}, After={speedAfterFactor:F2}, Expected={expectedSpeed:F2}");
        }
    }
}
