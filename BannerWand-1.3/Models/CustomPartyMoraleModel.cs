#nullable enable
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party morale calculation model that sets Base morale to +999 for player party.
    /// Extends <see cref="DefaultPartyMoraleModel"/> to add cheat functionality without Harmony patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all morale calculations.
    /// </para>
    /// <para>
    /// NEW IMPLEMENTATION (v2.0.1):
    /// Instead of adding a modifier to reach 100 morale, this version sets the Base value to +999.
    /// This ensures the morale display shows "Base +999" as requested.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Max Morale: Sets Base to +999 for player party (shows "Base +999" in UI)
    /// - Low Enemy Morale: Sets enemy parties to 10 morale when one-hit kills is enabled
    /// </para>
    /// <para>
    /// Morale effects in Bannerlord:
    /// - High morale (75+): Combat bonuses, faster movement
    /// - Normal morale (50-75): No bonuses or penalties
    /// - Low morale (&lt;50): Combat penalties, risk of desertion, slower movement
    /// </para>
    /// </remarks>
    public class CustomPartyMoraleModel : DefaultPartyMoraleModel
    {
        // Constants moved to GameConstants for consistency

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance!;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        /// <summary>
        /// Gets the effective morale for a mobile party with cheat overrides applied.
        /// Overrides <see cref="DefaultPartyMoraleModel.GetEffectivePartyMorale"/>.
        /// </summary>
        /// <param name="mobileParty">The party whose morale to calculate. Cannot be null.</param>
        /// <param name="includeDescription">
        /// Whether to include detailed explanations in the result.
        /// When true, the returned ExplainedNumber contains human-readable descriptions of each modifier.
        /// </param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the final morale value and breakdown of modifiers.
        /// The value represents party morale on a scale from 0 (minimum) to 100+ (with cheat enabled).
        /// </returns>
        /// <remarks>
        /// <para>
        /// NEW IMPLEMENTATION:
        /// Instead of calling base method and adding modifier, we now:
        /// 1. Check if Max Morale is enabled for this party
        /// 2. If yes: Return new ExplainedNumber with Base set to +999
        /// 3. If no: Call base implementation for normal morale calculation
        /// </para>
        /// <para>
        /// This approach ensures the morale UI shows "Base +999" instead of adding a modifier.
        /// </para>
        /// <para>
        /// Performance: Called frequently during campaign updates.
        /// Optimized with early returns and cached setting accesses.
        /// </para>
        /// </remarks>
        public override ExplainedNumber GetEffectivePartyMorale(MobileParty mobileParty, bool includeDescription = false)
        {
            try
            {                // Early exit for null or unconfigured settings
                if (Settings == null || TargetSettings == null || mobileParty == null)
                {
                    return base.GetEffectivePartyMorale(mobileParty, includeDescription);
                }

                // Apply max morale to player's party and targeted NPC parties
                if (Settings.MaxMorale)
                {
                    bool shouldApplyMaxMorale = ShouldApplyMaxMoraleToParty(mobileParty);

                    if (shouldApplyMaxMorale)
                    {
                        ExplainedNumber maxMorale = new(GameConstants.MaxMoraleBaseValue, includeDescription);

                        if (mobileParty == MobileParty.MainParty)
                        {
                            ModLogger.Debug($"Max Morale applied: Base +{GameConstants.MaxMoraleBaseValue} (Player Party)");
                        }

                        return maxMorale;
                    }
                }

                // Get base morale from default implementation
                ExplainedNumber baseMorale = base.GetEffectivePartyMorale(mobileParty, includeDescription);

                // Apply low morale to enemy parties
                if (Settings.OneHitKills && mobileParty != MobileParty.MainParty)
                {
                    bool shouldApplyLowMorale = ShouldApplyLowMoraleToParty(mobileParty);

                    if (shouldApplyLowMorale)
                    {
                        ApplyMoraleAdjustment(baseMorale, GameConstants.LowEnemyMoraleValue);
                    }
                }

                return baseMorale;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyMoraleModel] Error in GetEffectivePartyMorale: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return new ExplainedNumber(50f);
            }
        }

        /// <summary>
        /// Determines if maximum morale should be applied to the specified party.
        /// </summary>
        /// <param name="mobileParty">The party to check. Cannot be null.</param>
        /// <returns>
        /// True if the party should receive maximum morale, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method centralizes the logic for determining if a party qualifies for max morale.
        /// It checks two main conditions:
        /// </para>
        /// <para>
        /// 1. Player party: If this is the main player party and ApplyToPlayer is enabled
        /// 2. NPC parties: If this is a targeted NPC party (based on TargetFilter settings)
        /// </para>
        /// </remarks>
        private bool ShouldApplyMaxMoraleToParty(MobileParty mobileParty)
        {
            // Check if this is player party
            // ApplyToPlayer must be enabled for player party to receive the cheat
            if (mobileParty == MobileParty.MainParty && TargetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if this is a targeted NPC party
            // HasAnyNPCTargetEnabled checks if any NPC target options are enabled (companions, vassals, etc.)
            // ShouldApplyCheatToParty checks if the specific party matches the target criteria
            return mobileParty != MobileParty.MainParty &&
                TargetSettings.HasAnyNPCTargetEnabled() &&
                TargetFilter.ShouldApplyCheatToParty(mobileParty);
        }

        /// <summary>
        /// Determines if low morale should be applied to the specified party.
        /// </summary>
        /// <param name="mobileParty">The party to check. Cannot be null.</param>
        /// <returns>
        /// True if the party should receive low morale (enemy party at war with player), false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method checks if the party is an enemy of the player by verifying:
        /// 1. The party is not the main player party
        /// 2. Both parties have valid factions
        /// 3. The party's faction is at war with the player's faction
        /// </para>
        /// <para>
        /// Low morale synergizes with One-Hit Kills cheat to make combat easier:
        /// - Low morale gives combat penalties to enemies
        /// - Makes enemy troops route/flee faster in battle
        /// - Combined with One-Hit Kills, results in very quick victories
        /// </para>
        /// </remarks>
        private bool ShouldApplyLowMoraleToParty(MobileParty mobileParty)
        {
            MobileParty? mainParty = MobileParty.MainParty;

            // Verify both parties have valid factions
            if (mainParty == null || mobileParty.MapFaction == null || mainParty.MapFaction == null)
            {
                return false;
            }

            // Check if this party is at war with player
            // IsAtWarWith returns true if the two factions are currently in a state of war
            bool isAtWar = mobileParty.MapFaction.IsAtWarWith(mainParty.MapFaction);

            return isAtWar;
        }

        /// <summary>
        /// Applies a morale adjustment to set the morale to the target value.
        /// </summary>
        /// <param name="currentMorale">The current morale ExplainedNumber to modify.</param>
        /// <param name="targetMorale">The target morale value to achieve.</param>
        /// <remarks>
        /// <para>
        /// This method calculates the difference between the target morale and current morale,
        /// then adds that difference to the ExplainedNumber. This effectively sets the morale
        /// to the exact target value while preserving the ExplainedNumber's modifier tracking.
        /// </para>
        /// <para>
        /// Example: If current morale is 65 and target is 100, this adds +35 to reach 100.
        /// If current morale is 80 and target is 10, this adds -70 to reach 10.
        /// </para>
        /// </remarks>
        private void ApplyMoraleAdjustment(ExplainedNumber currentMorale, float targetMorale)
        {
            // Calculate the adjustment needed to reach target morale
            float moraleAdjustment = targetMorale - currentMorale.ResultNumber;

            // Add the adjustment to the ExplainedNumber
            // Passing null as the second parameter means this modifier won't have a description
            currentMorale.Add(moraleAdjustment, null);
        }
    }
}
