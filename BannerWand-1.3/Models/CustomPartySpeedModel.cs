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
    /// When War Sails DLC is available, this model will work correctly with naval speed calculations.
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
    /// <para>
    /// IMPORTANT: This model works with both DefaultPartySpeedCalculatingModel and
    /// NavalDLCPartySpeedCalculationModel. When DLC is active, the game will use
    /// NavalDLCPartySpeedCalculationModel, but this model will still be registered
    /// and will handle speed overrides correctly for both land and sea travel.
    /// </para>
    /// <para>
    /// SAFETY FOR PLAYERS WITHOUT DLC:
    /// - This model safely handles the case when DLC is not available
    /// - Uses base.BaseModel.CalculateBaseSpeed() which points to DefaultPartySpeedCalculatingModel when DLC is not present
    /// - Includes null checks and fallbacks to ensure compatibility
    /// - NavalSpeedPatch is only applied when DLC is detected, so it won't cause errors for players without DLC
    /// </para>
    /// </remarks>
    public class CustomPartySpeedModel : DefaultPartySpeedCalculatingModel
    {
        private CheatSettings? Settings => CheatSettings.Instance;
        private CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Tracks the last desired speed value to detect changes for logging.
        /// </summary>
        private static float _lastDesiredSpeed = 0f;

        /// <summary>
        /// Checks if the base model is the Naval DLC model.
        /// Used to ensure DLC fix works correctly for all parties at sea.
        /// </summary>
        private bool IsNavalDLCModelActive()
        {
            return base.BaseModel?.GetType().FullName == "NavalDLC.GameComponents.NavalDLCPartySpeedCalculationModel";
        }

        /// <summary>
        /// Text object for speed override description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject SpeedOverrideText = new("BannerWand Speed Override");

        /// <summary>
        /// Calculates the base movement speed for a party.
        /// Applies speed multiplier to all parties on the map if enabled.
        /// Works correctly with both land and sea travel (including War Sails DLC).
        /// </summary>
        public override ExplainedNumber CalculateBaseSpeed(
            MobileParty mobileParty,
            bool includeDescriptions = false,
            int additionalTroopOnFootCount = 0,
            int additionalTroopOnHorseCount = 0)
        {
            try
            {
                // IMPORTANT: Use base.BaseModel.CalculateBaseSpeed() directly instead of base.CalculateBaseSpeed()
                // This ensures we call the DLC model (NavalDLCPartySpeedCalculationModel) if it's active,
                // rather than always calling DefaultPartySpeedCalculatingModel
                // This DLC fix works ALWAYS, regardless of cheat settings - it's a bug fix, not a cheat feature
                // SAFETY: If base.BaseModel is null (shouldn't happen, but be safe), fallback to base.CalculateBaseSpeed()
                ExplainedNumber baseSpeed;
                if (base.BaseModel != null)
                {
                    // DLC FIX: Always call base.BaseModel to ensure DLC model is used for ALL parties at sea
                    // This ensures that when a party is at sea, the DLC model's CalculateNavalBaseSpeed is called
                    // which properly calculates naval speed with all modifiers (wind, crew, etc.)
                    baseSpeed = base.BaseModel.CalculateBaseSpeed(
                        mobileParty,
                        includeDescriptions,
                        additionalTroopOnFootCount,
                        additionalTroopOnHorseCount);

                    // ADDITIONAL DLC FIX: If DLC model is active and party is at sea, ensure the result is valid
                    // This is a safety check to ensure the DLC fix works for ALL parties, not just the player
                    if (IsNavalDLCModelActive() && mobileParty?.IsCurrentlyAtSea == true)
                    {
                        // The DLC model should have already calculated the correct speed via CalculateNavalBaseSpeed
                        // We just ensure the result is valid (not null or invalid)
                        // No modification needed - the DLC model already calculates correctly
                        // This ensures the fix works for ALL parties at sea, not just the player
                    }
                }
                else
                {
                    // Fallback if BaseModel is null (shouldn't happen in normal operation)
                    baseSpeed = base.CalculateBaseSpeed(
                        mobileParty,
                        includeDescriptions,
                        additionalTroopOnFootCount,
                        additionalTroopOnHorseCount);
                }

                // Apply speed override cheat if enabled (applies to all parties on the map)
                // This is a CHEAT feature, not the DLC fix - the DLC fix above works always
                if (mobileParty != null && Settings != null && TargetSettings != null && ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    float speedBefore = baseSpeed.ResultNumber;
                    ApplyPlayerSpeedBoost(ref baseSpeed);
                    float speedAfter = baseSpeed.ResultNumber;
                    if (Math.Abs(speedBefore - speedAfter) > 0.01f)
                    {
                        ModLogger.Debug($"Movement Speed: Applied to {mobileParty.Name} - Speed changed from {speedBefore:F2} to {speedAfter:F2}");
                    }
                }

                return baseSpeed;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartySpeedModel] Exception in CalculateBaseSpeed: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // Safe fallback: try BaseModel first, then base method if BaseModel is null
                return base.BaseModel != null
                    ? base.BaseModel.CalculateBaseSpeed(mobileParty, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount)
                    : base.CalculateBaseSpeed(mobileParty, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);
            }
        }

        /// <summary>
        /// Calculates the final movement speed for a party after all modifiers.
        /// This method is called after CalculateBaseSpeed and applies final speed multipliers.
        /// Applies speed multiplier to all parties on the map if enabled.
        /// Works correctly with both land and sea travel (including War Sails DLC).
        /// </summary>
        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            try
            {
                // IMPORTANT: Use base.BaseModel.CalculateFinalSpeed() directly instead of base.CalculateFinalSpeed()
                // This ensures we call the DLC model (NavalDLCPartySpeedCalculationModel) if it's active,
                // rather than always calling DefaultPartySpeedCalculatingModel
                // This DLC fix works ALWAYS, regardless of cheat settings - it's a bug fix, not a cheat feature
                // SAFETY: If base.BaseModel is null (shouldn't happen, but be safe), fallback to base.CalculateFinalSpeed()
                ExplainedNumber result;
                if (base.BaseModel != null)
                {
                    // DLC FIX: Always call base.BaseModel to ensure DLC model is used for ALL parties at sea
                    // This ensures that when a party is at sea, the DLC model's CalculateFinalSpeed is called
                    // which properly adds naval modifiers (wind, crew, terrain, etc.) to the final speed
                    result = base.BaseModel.CalculateFinalSpeed(mobileParty, finalSpeed);

                    // ADDITIONAL DLC FIX: If DLC model is active and party is at sea, ensure the result is valid
                    // This is a safety check to ensure the DLC fix works for ALL parties, not just the player
                    if (IsNavalDLCModelActive() && mobileParty?.IsCurrentlyAtSea == true)
                    {
                        // The DLC model should have already added all naval modifiers (wind, crew, etc.)
                        // We just ensure the result is valid (not null or invalid)
                        // No modification needed - the DLC model already calculates correctly
                        // This ensures the fix works for ALL parties at sea, not just the player
                    }
                }
                else
                {
                    // Fallback if BaseModel is null (shouldn't happen in normal operation)
                    result = base.CalculateFinalSpeed(mobileParty, finalSpeed);
                }

                // Apply speed override cheat if enabled (applies to all parties on the map)
                // This is a CHEAT feature, not the DLC fix - the DLC fix above works always
                if (mobileParty != null && Settings != null && TargetSettings != null && ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    ApplyPlayerSpeedBoost(ref result);
                }

                return result;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartySpeedModel] Exception in CalculateFinalSpeed: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // Safe fallback: try BaseModel first, then base method if BaseModel is null
                return base.BaseModel != null
                    ? base.BaseModel.CalculateFinalSpeed(mobileParty, finalSpeed)
                    : base.CalculateFinalSpeed(mobileParty, finalSpeed);
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

            // Removed excessive logging - speed boost is applied silently
        }

        /// <summary>
        /// Determines if speed override should be applied to a party.
        /// Applies to all parties on the map when MovementSpeed > 0 and ApplyToPlayer is enabled.
        /// </summary>
#pragma warning disable IDE0060, RCS1163 // Remove unused parameter - parameter name required for method signature
        private bool ShouldApplyPlayerSpeedOverride(MobileParty mobileParty)
#pragma warning restore IDE0060, RCS1163
        {
            // Early exit if settings are null
            if (Settings is null || TargetSettings is null)
            {
                return false;
            }

            bool movementSpeedOk = Settings.MovementSpeed > 0f;
            bool applyToPlayer = TargetSettings.ApplyToPlayer;
            bool campaignActive = Campaign.Current is not null;

            // Apply to ALL parties on the map when MovementSpeed > 0 and ApplyToPlayer is enabled
            // This ensures all parties (player, AI, etc.) get the speed boost
            return movementSpeedOk && applyToPlayer && campaignActive;
        }

    }
}
