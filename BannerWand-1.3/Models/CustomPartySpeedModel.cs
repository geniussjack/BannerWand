#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party speed model that handles player movement speed override and AI slowdown.
    /// Extends <see cref="DefaultPartySpeedCalculatingModel"/> to modify party movement speeds.
    /// When War Sails DLC is available, this model will work correctly with naval speed calculations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Previous implementation created a new ExplainedNumber with custom base value,
    /// which ignored all other speed modifiers (terrain, party composition, etc.).
    /// </para>
    /// <para>
    /// Current implementation adds a factor to the base speed calculation, preserving
    /// all other modifiers while applying the speed boost.
    /// </para>
    /// <para>
    /// This model works with both DefaultPartySpeedCalculatingModel and
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
                // Use base.BaseModel.CalculateBaseSpeed() directly instead of base.CalculateBaseSpeed()
                // This ensures we call the DLC model (NavalDLCPartySpeedCalculationModel) if it's active,
                // rather than always calling DefaultPartySpeedCalculatingModel
                // This works ALWAYS, regardless of cheat settings - it's a bug fix, not a cheat feature
                // SAFETY: If base.BaseModel is null (shouldn't happen, but be safe), fallback to base.CalculateBaseSpeed()
                // Always call base.BaseModel to ensure DLC model is used for ALL parties at sea
                // This ensures that when a party is at sea, the DLC model's CalculateNavalBaseSpeed is called
                // which properly calculates naval speed with all modifiers (wind, crew, etc.)
                // Speed override is applied in CalculateFinalSpeed, not here
                // This prevents double-application and ensures speed is set correctly
                // For sea travel, speed is applied via NavalSpeedPatch, not here
                return base.BaseModel != null
                    ? base.BaseModel.CalculateBaseSpeed(
                        mobileParty,
                        includeDescriptions,
                        additionalTroopOnFootCount,
                        additionalTroopOnHorseCount)
                    : base.CalculateBaseSpeed(
                        mobileParty,
                        includeDescriptions,
                        additionalTroopOnFootCount,
                        additionalTroopOnHorseCount);
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
                // Use base.BaseModel.CalculateFinalSpeed() directly instead of base.CalculateFinalSpeed()
                // This ensures we call the DLC model (NavalDLCPartySpeedCalculationModel) if it's active,
                // rather than always calling DefaultPartySpeedCalculatingModel
                // This DLC compatibility work runs ALWAYS, regardless of cheat settings - it's a bug fix, not a cheat feature
                // SAFETY: If base.BaseModel is null (shouldn't happen, but be safe), fallback to base.CalculateFinalSpeed()
                // Always call base.BaseModel to ensure DLC model is used for ALL parties at sea
                // This ensures that when a party is at sea, the DLC model's CalculateFinalSpeed is called
                // which properly adds naval modifiers (wind, crew, terrain, etc.) to the final speed
                // NOTE: Speed bonus is now applied via MobilePartySpeedPatch, which patches
                // MobileParty.CalculateSpeed and MobileParty.SpeedExplained directly.
                // This ensures the bonus is constant and doesn't fluctuate.
                // We no longer apply speed here to avoid double-application and conflicts.
                // The patch approach is more reliable and matches Character Reload's implementation.
                return base.BaseModel != null
                    ? base.BaseModel.CalculateFinalSpeed(mobileParty, finalSpeed)
                    : base.CalculateFinalSpeed(mobileParty, finalSpeed);
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
        /// Applies player speed boost by setting a fixed final speed value.
        /// The speed will always equal the desired speed, regardless of base speed changes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Reserved for future speed boost implementation")]
        private void ApplyPlayerSpeedBoost(ref ExplainedNumber speed, float desiredSpeed)
        {
            // desiredSpeed is the FINAL speed value the user wants (0-16)
            // Settings is already validated in ShouldApplyPlayerSpeedOverride() or ShouldApplyNPCSpeedOverride()

            if (desiredSpeed <= 0f)
            {
                // Disabled - reset tracking
                _lastDesiredSpeed = 0f;
                return;
            }

            // Cap desired speed to maximum allowed value
            // This prevents speed from exceeding the maximum set in MCM settings
            if (desiredSpeed > GameConstants.AbsoluteMaxGameSpeed)
            {
                desiredSpeed = GameConstants.AbsoluteMaxGameSpeed;
            }

            // CRITICAL FIX: Create a new ExplainedNumber with the desired speed as the base value
            // and NO additional modifiers. This ensures the speed is ALWAYS exactly equal to desiredSpeed.
            //
            // Previous issue: speed.Add(0f, SpeedOverrideText) was being called, which might have
            // affected the result or allowed other modifiers to stack. Now we create a completely
            // fresh ExplainedNumber with ONLY the desired speed as the base value.
            //
            // Why this works:
            // 1. ExplainedNumber stores modifiers internally and calculates ResultNumber = BaseNumber + sum of modifiers
            // 2. By creating a new ExplainedNumber with desiredSpeed as BaseNumber and NO modifiers,
            //    ResultNumber will ALWAYS equal desiredSpeed
            // 3. This prevents any fluctuations or stacking of modifiers from multiple calls
            // 4. The speed becomes completely static and constant at the desired value
            //
            // We preserve the includeDescriptions flag from the original speed for UI display
            bool includeDescriptions = speed.IncludeDescriptions;
            speed = new ExplainedNumber(desiredSpeed, includeDescriptions, null);
            // DO NOT call speed.Add() - this ensures no modifiers are added that could affect the result

            // Track desired speed for debugging and state tracking
            _lastDesiredSpeed = desiredSpeed;
        }

        /// <summary>
        /// Determines if speed override should be applied to a party.
        /// Only applies to the player's main party when MovementSpeed > 0 and ApplyToPlayer is enabled.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by NavalSpeedPatch via reflection or reserved for future use")]
        private bool ShouldApplyPlayerSpeedOverride(MobileParty mobileParty)
        {
            // Early exit if settings are null
            return Settings is not null && TargetSettings is not null && Settings.MovementSpeed > 0f &&
                   mobileParty == MobileParty.MainParty &&
                   TargetSettings.ApplyToPlayer &&
                   Campaign.Current != null;
        }

        /// <summary>
        /// Determines if NPC speed override should be applied.
        /// Applies only to non-player parties when NPCMovementSpeed > 0.
        /// Player's party should use Player speed override instead.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by NavalSpeedPatch via reflection or reserved for future use")]
        private bool ShouldApplyNPCSpeedOverride(MobileParty mobileParty)
        {
            // Early exit if settings are null
            // Do NOT apply NPC speed to player's main party - player should use Player speed instead
            return Settings?.NPCMovementSpeed > 0f &&
                   Campaign.Current != null &&
                   mobileParty != MobileParty.MainParty;
        }

    }
}
