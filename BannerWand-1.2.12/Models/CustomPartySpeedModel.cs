#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Patches;
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
    /// <remarks>
    /// <para>
    /// This model works together with MobilePartySpeedPatch. When MobilePartySpeedPatch is active,
    /// this model skips its speed modification to prevent conflicts. The patch adds a fixed speed bonus
    /// directly to MobileParty.SpeedExplained, ensuring constant speed without fluctuations.
    /// </para>
    /// </remarks>
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

                // NOTE: Speed bonus is now applied via MobilePartySpeedPatch, which patches
                // MobileParty.SpeedExplained directly. This ensures the bonus is constant and doesn't fluctuate.
                // We skip applying speed here if the patch is active to avoid conflicts.
                if (MobilePartySpeedPatch.IsActive())
                {
                    return baseSpeed;
                }

                // Check if player speed override is enabled (only for player's main party)
                if (ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    float speedBefore = baseSpeed.ResultNumber;
                    ApplyPlayerSpeedBoost(ref baseSpeed, Settings.MovementSpeed);
                    float speedAfter = baseSpeed.ResultNumber;
                    if (Math.Abs(speedBefore - speedAfter) > 0.01f)
                    {
                        ModLogger.Debug($"Movement Speed: Applied to {mobileParty.Name} - Speed changed from {speedBefore:F2} to {speedAfter:F2}");
                    }
                }
                // Check for NPC speed override (only for non-player parties)
                else if (ShouldApplyNPCSpeedOverride(mobileParty))
                {
                    float speedBefore = baseSpeed.ResultNumber;
                    ApplyPlayerSpeedBoost(ref baseSpeed, Settings.NPCMovementSpeed);
                    float speedAfter = baseSpeed.ResultNumber;
                    if (Math.Abs(speedBefore - speedAfter) > 0.01f)
                    {
                        ModLogger.Debug($"NPC Movement Speed: Applied to {mobileParty.Name} - Speed changed from {speedBefore:F2} to {speedAfter:F2}");
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
        /// Determines if NPC speed override should be applied.
        /// Applies only to non-player parties when NPCMovementSpeed > 0.
        /// Player's party should use Player speed override instead.
        /// </summary>
        private bool ShouldApplyNPCSpeedOverride(MobileParty mobileParty)
        {
            // Early exit if settings are null
            // Do NOT apply NPC speed to player's main party - player should use Player speed instead
            return Settings?.NPCMovementSpeed > 0f && 
                   Campaign.Current != null &&
                   mobileParty != MobileParty.MainParty;
        }

        /// <summary>
        /// Applies player speed boost by calculating a multiplier factor.
        /// Preserves all terrain, composition, and other modifiers.
        /// </summary>
        private void ApplyPlayerSpeedBoost(ref ExplainedNumber speed, float desiredSpeed)
        {
            // desiredSpeed is the FINAL speed value the user wants
            // Settings is already validated in ShouldApplyPlayerSpeedOverride() or ShouldApplyNPCSpeedOverride()
            float currentBaseSpeed = speed.BaseNumber;

            // Cap the multiplier to prevent extreme values when base speed is very small
            // Maximum multiplier of 100x prevents distortion while still allowing significant speed boosts
            const float maxMultiplier = GameConstants.MaxSpeedMultiplier;

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
                float factorToAdd = multiplier - GameConstants.MultiplierFactorBase;
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
