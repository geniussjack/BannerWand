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
        private static bool _aiSlowdownLogged = false;

        /// <summary>
        /// Tracks the last desired speed value to detect changes.
        /// </summary>
        private static float _lastDesiredSpeed = 0f;

        /// <summary>
        /// Counter for CalculateBaseSpeed calls (for debugging).
        /// </summary>
        private static int _calculateBaseSpeedCallCount = 0;

        /// <summary>
        /// Counter for ApplyPlayerSpeedBoost calls (for debugging).
        /// </summary>
        private static int _applyPlayerSpeedBoostCallCount = 0;

        /// <summary>
        /// Counter for ShouldApplyPlayerSpeedOverride checks (for debugging).
        /// </summary>
        private static int _shouldApplyCheckCount = 0;

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
                _calculateBaseSpeedCallCount++;

                // Get base speed from default implementation FIRST
                ExplainedNumber baseSpeed = base.CalculateBaseSpeed(
                    mobileParty,
                    includeDescriptions,
                    additionalTroopOnFootCount,
                    additionalTroopOnHorseCount);

                // Log first few calls for debugging
                if (_calculateBaseSpeedCallCount <= 5)
                {
                    string partyName = mobileParty?.Name?.ToString() ?? "null";
                    bool isMainParty = mobileParty == MobileParty.MainParty;
                    ModLogger.Log($"[Movement Speed DEBUG] CalculateBaseSpeed call #{_calculateBaseSpeedCallCount}: Party={partyName}, IsMainParty={isMainParty}, BaseSpeed={baseSpeed.BaseNumber:F2}, ResultSpeed={baseSpeed.ResultNumber:F2}");
                }

                // Early exit for null settings
                if (Settings == null || TargetSettings == null || mobileParty == null)
                {
                    if (_calculateBaseSpeedCallCount <= 5)
                    {
                        ModLogger.Warning($"[Movement Speed DEBUG] Early exit: Settings={Settings != null}, TargetSettings={TargetSettings != null}, mobileParty={mobileParty != null}");
                    }
                    return baseSpeed;
                }

                // Apply player speed override if enabled
                if (ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    ApplyPlayerSpeedBoost(ref baseSpeed);
                }
                // Apply AI slowdown if enabled (only for non-player parties)
                else if (ShouldApplyAiSlowdown(mobileParty))
                {
                    ApplyAiSlowdown(baseSpeed, mobileParty);
                }
                else if (mobileParty == MobileParty.MainParty && _calculateBaseSpeedCallCount <= 5)
                {
                    // Log why speed override is not being applied
                    ModLogger.Log($"[Movement Speed DEBUG] ShouldApplyPlayerSpeedOverride returned false: MovementSpeed={Settings.MovementSpeed}, ApplyToPlayer={TargetSettings.ApplyToPlayer}, Campaign.Current={Campaign.Current != null}");
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
            _applyPlayerSpeedBoostCallCount++;

            // MovementSpeed setting is the FINAL speed value the user wants
            float desiredSpeed = Settings!.MovementSpeed;

            // Get current values before modification
            float originalBase = speed.BaseNumber;
            float originalResult = speed.ResultNumber;
            bool speedChanged = desiredSpeed != _lastDesiredSpeed;

            // Log every call for debugging (first 10 calls, then every 100th call)
            bool shouldLog = _applyPlayerSpeedBoostCallCount <= 10 || _applyPlayerSpeedBoostCallCount % 100 == 0;

            if (shouldLog)
            {
                ModLogger.Log($"[Movement Speed DEBUG] ApplyPlayerSpeedBoost call #{_applyPlayerSpeedBoostCallCount}: Desired={desiredSpeed:F2}, Original Base={originalBase:F2}, Original Result={originalResult:F2}, LastDesired={_lastDesiredSpeed:F2}, SpeedChanged={speedChanged}");
            }

            if (desiredSpeed <= 0f)
            {
                // Disabled - reset tracking
                if (shouldLog)
                {
                    ModLogger.Log($"[Movement Speed DEBUG] Speed disabled (desiredSpeed={desiredSpeed:F2}), resetting tracking");
                }
                _lastDesiredSpeed = 0f;
                return;
            }

            // FIXED: Instead of creating a new ExplainedNumber (which loses all modifiers),
            // we calculate the multiplier needed to achieve the desired speed and apply it as a factor.
            // This preserves all terrain, composition, and other modifiers.
            float currentBaseSpeed = speed.BaseNumber;
            if (currentBaseSpeed > GameConstants.MinBaseSpeedThreshold)
            {
                // Calculate multiplier: desiredSpeed / currentBaseSpeed
                // Then add factor = (multiplier - 1.0) to get the desired result
                float multiplier = desiredSpeed / currentBaseSpeed;
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, SpeedOverrideText);
            }
            else
            {
                // If base speed is too low, set it directly using Add
                float adjustment = desiredSpeed - speed.ResultNumber;
                speed.Add(adjustment, SpeedOverrideText);
            }

            if (shouldLog)
            {
                ModLogger.Log($"[Movement Speed DEBUG] Applied factor to preserve modifiers: Base={currentBaseSpeed:F2}, Desired={desiredSpeed:F2}, Result={speed.ResultNumber:F2}");
            }

            // Track desired speed
            _lastDesiredSpeed = desiredSpeed;

            // Log once for debugging or when speed changes
            if (!_playerSpeedLogged || speedChanged)
            {
                _playerSpeedLogged = true;
                ModLogger.Log($"[Movement Speed] Applied: Original Base={originalBase:F2}, Original Result={originalResult:F2}, Desired={desiredSpeed:F2}, New Base={desiredSpeed:F2}, Final={speed.ResultNumber:F2}");
            }
        }

        /// <summary>
        /// Determines if player speed override should be applied.
        /// </summary>
        private bool ShouldApplyPlayerSpeedOverride(MobileParty mobileParty)
        {
            _shouldApplyCheckCount++;

            bool movementSpeedOk = Settings!.MovementSpeed > 0f;
            bool isMainParty = mobileParty == MobileParty.MainParty;
            bool applyToPlayer = TargetSettings!.ApplyToPlayer;
            bool campaignActive = Campaign.Current != null;

            bool shouldApply = movementSpeedOk && isMainParty && applyToPlayer && campaignActive;

            // Log first 10 checks for debugging
            if (_shouldApplyCheckCount <= 10)
            {
                ModLogger.Log($"[Movement Speed DEBUG] ShouldApplyPlayerSpeedOverride check #{_shouldApplyCheckCount}: MovementSpeed={Settings.MovementSpeed:F2} (>0={movementSpeedOk}), IsMainParty={isMainParty}, ApplyToPlayer={applyToPlayer}, CampaignActive={campaignActive}, Result={shouldApply}");
            }

            return shouldApply;
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

            // Apply to bandits
            if (mobileParty.IsBandit)
            {
                return true;
            }

            // Apply to parties at war with player
            return mobileParty.MapFaction != null && Hero.MainHero?.MapFaction != null && FactionManager.IsAtWarAgainstFaction(mobileParty.MapFaction, Hero.MainHero.MapFaction);
        }

        /// <summary>
        /// Applies AI slowdown to the party speed.
        /// Reduces FINAL speed to 50% (half speed) using multiplicative factor.
        /// </summary>
        private void ApplyAiSlowdown(ExplainedNumber baseSpeed, MobileParty mobileParty)
        {
            float speedBefore = baseSpeed.ResultNumber;

            // AddFactor(-0.5) reduces speed by 50%
            baseSpeed.AddFactor(GameConstants.AiSlowdownFactor, new TextObject("BannerWand AI Slowdown"));

            // Log once for debugging
            if (!_aiSlowdownLogged)
            {
                _aiSlowdownLogged = true;
                string partyName = mobileParty.Name?.ToString() ?? MessageConstants.UnknownPartyName;
                ModLogger.Log($"[AI Slowdown] Applied to {partyName}: {speedBefore:F2} → {baseSpeed.ResultNumber:F2}");
            }
        }
    }
}
