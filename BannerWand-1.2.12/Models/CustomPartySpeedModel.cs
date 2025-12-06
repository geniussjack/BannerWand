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

            // Cap the multiplier to prevent extreme values when base speed is very small
            // Maximum multiplier of 100x prevents distortion while still allowing significant speed boosts
            const float maxMultiplier = 100f;

            if (currentBaseSpeed > GameConstants.MinBaseSpeedThreshold)
            {
                // Calculate multiplier: desiredSpeed / currentBaseSpeed
                // Cap it to prevent extreme values when base speed is very small
                float multiplier = desiredSpeed / currentBaseSpeed;
                if (multiplier > maxMultiplier)
                {
                    // If multiplier would be too large, cap the desired speed instead
                    _ = currentBaseSpeed * maxMultiplier;
                    multiplier = maxMultiplier;
                }
                // Then add factor = (multiplier - 1.0) to get the desired result
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, new TaleWorlds.Localization.TextObject("BannerWand Speed Override"));
            }
            else
            {
                // If base speed is too low, use multiplicative approach with capped multiplier
                // This ensures consistent behavior regardless of base speed value
                float cappedDesiredSpeed = Math.Min(desiredSpeed, currentBaseSpeed * maxMultiplier);
                float multiplier = cappedDesiredSpeed / Math.Max(currentBaseSpeed, GameConstants.MinBaseSpeedThreshold);
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, new TaleWorlds.Localization.TextObject("BannerWand Speed Override"));
            }
        }

    }
}
