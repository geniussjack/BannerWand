#nullable enable
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party speed model that handles player movement speed override and AI slowdown.
    /// Extends <see cref="DefaultPartySpeedCalculatingModel"/> to modify party movement speeds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FIXED: Previous implementation created a new ExplainedNumber with custom base value,
    /// which ignored all other speed modifiers (terrain, party composition, etc.).
    /// </para>
    /// <para>
    /// New implementation adds a factor to the base speed calculation, preserving
    /// all other modifiers while applying the speed boost.
    /// </para>
    /// </remarks>
    public class CustomPartySpeedModel : DefaultPartySpeedCalculatingModel
    {
        private CheatSettings? Settings => CheatSettings.Instance;
        private CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;
        private static bool _playerSpeedLogged = false;

        /// <summary>
        /// Tracks the last desired speed value to detect changes for logging.
        /// </summary>
        private static float _lastDesiredSpeed = 0f;

        /// <summary>
        /// Text object for speed override description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject SpeedOverrideText = new("BannerWand Speed Override");

        /// <summary>
        /// Calculates the base movement speed for a party.
        /// Applies player speed multiplier if enabled, applies AI slowdown for hostile parties.
        /// </summary>
        public override ExplainedNumber CalculateBaseSpeed(
            MobileParty mobileParty,
            bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0,
            int additionalTroopOnHorseCount = 0)
        {
            try
            {
                // Get base speed from default implementation FIRST
                ExplainedNumber baseSpeed = base.CalculateBaseSpeed(
                    mobileParty,
                    includeDescriptions,
                    additionalTroopOnFootCount,
                    additionalTroopOnHorseCount);

                // Early exit for null settings
                if (Settings == null || TargetSettings == null || mobileParty == null)
                {
                    return baseSpeed;
                }

                // Apply player speed override if enabled
                if (ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    ApplyPlayerSpeedBoost(ref baseSpeed);
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
        /// Applies player speed boost by directly setting the base speed value.
        /// Uses reflection to set BaseNumber property directly, preserving all modifiers.
        /// </summary>
        private void ApplyPlayerSpeedBoost(ref ExplainedNumber speed)
        {
            // MovementSpeed setting is the FINAL speed value the user wants
            // Settings is already validated in ShouldApplyPlayerSpeedOverride()
            float desiredSpeed = Settings?.MovementSpeed ?? 0f;
            bool speedChanged = desiredSpeed != _lastDesiredSpeed;

            if (desiredSpeed <= 0f)
            {
                // Disabled - reset tracking
                _lastDesiredSpeed = 0f;
                return;
            }

            // FIXED: Instead of creating a new ExplainedNumber (which loses all modifiers),
            // we calculate the multiplier needed to achieve the desired speed and apply it as a factor.
            // This preserves all terrain, composition, and other modifiers.
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
                    desiredSpeed = currentBaseSpeed * maxMultiplier;
                    multiplier = maxMultiplier;
                }
                // Then add factor = (multiplier - 1.0) to get the desired result
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, SpeedOverrideText);
            }
            else
            {
                // If base speed is too low, use multiplicative approach with capped multiplier
                // This ensures consistent behavior regardless of base speed value
                float cappedDesiredSpeed = Math.Min(desiredSpeed, currentBaseSpeed * maxMultiplier);
                float multiplier = cappedDesiredSpeed / Math.Max(currentBaseSpeed, GameConstants.MinBaseSpeedThreshold);
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, SpeedOverrideText);
            }

            // Track desired speed
            _lastDesiredSpeed = desiredSpeed;

            // Log once for debugging or when speed changes
            if (!_playerSpeedLogged || speedChanged)
            {
                _playerSpeedLogged = true;
                ModLogger.Log($"[Movement Speed] Applied: Desired={desiredSpeed:F2}, Final={speed.ResultNumber:F2}");
            }
        }

        /// <summary>
        /// Determines if player speed override should be applied.
        /// </summary>
        private bool ShouldApplyPlayerSpeedOverride(MobileParty mobileParty)
        {
            // Early exit if settings are null
            if (Settings is null || TargetSettings is null)
            {
                return false;
            }

            bool movementSpeedOk = Settings.MovementSpeed > 0f;
            bool isMainParty = mobileParty == MobileParty.MainParty;
            bool applyToPlayer = TargetSettings.ApplyToPlayer;
            bool campaignActive = Campaign.Current is not null;

            return movementSpeedOk && isMainParty && applyToPlayer && campaignActive;
        }

    }
}
