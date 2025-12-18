#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom siege event model that enables instant siege equipment construction for player and NPC parties.
    /// Extends <see cref="DefaultSiegeEventModel"/> to add cheat functionality without Harmony patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all siege calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Instant Siege Construction: Makes siege equipment build instantly for player and targeted NPCs (affects both attacker AND defender sides)
    /// - Slow AI Sieges: Reduces enemy siege construction speed to 10% (when AI slowdown enabled)
    /// </para>
    /// <para>
    /// Siege equipment in Bannerlord:
    /// - Battering rams, siege towers, trebuchets, etc.
    /// - Normally take hours/days to construct
    /// - This cheat makes them instant for targeted parties
    /// </para>
    /// </remarks>
    public class CustomSiegeEventModel : DefaultSiegeEventModel
    {
        // Constants moved to GameConstants for consistency

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Gets the construction progress per hour for siege engines with cheat overrides.
        /// Overrides <see cref="DefaultSiegeEventModel.GetConstructionProgressPerHour"/>.
        /// </summary>
        /// <param name="siegeEngineType">The type of siege engine being constructed (ram, tower, trebuchet, etc.).</param>
        /// <param name="siegeEvent">The ongoing siege event containing attacker and defender information.</param>
        /// <param name="siegeEventSide">The side constructing the engine (attacker or defender).</param>
        /// <returns>
        /// The hourly construction progress (normal is 0.1-1.0, instant is 999,999).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Construction progress calculation:
        /// - Base progress: 0.1 to 1.0 per hour (takes many hours to complete)
        /// - Engines require 100-200 total progress to complete
        /// - Cheat sets: 999,999 per hour for instant completion
        /// - AI slowdown: 0.1x multiplier (10% of normal speed)
        /// </para>
        /// <para>
        /// Implementation strategy:
        /// 1. Get base progress from default model
        /// 2. If instant construction enabled and leader party is targeted, return huge value (affects BOTH attacker and defender sides)
        /// 3. If AI slowdown enabled, reduce non-targeted parties to 10%
        /// 4. Otherwise return base value
        /// </para>
        /// <para>
        /// Performance: Called hourly during active sieges (not performance-critical).
        /// </para>
        /// </remarks>
        public override float GetConstructionProgressPerHour(SiegeEngineType siegeEngineType, SiegeEvent siegeEvent, ISiegeEventSide siegeEventSide)
        {
            try
            {
                float baseProgress = base.GetConstructionProgressPerHour(siegeEngineType, siegeEvent, siegeEventSide);

                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings is null || targetSettings is null)
                {
                    return baseProgress;
                }

                // Apply instant siege construction when enabled
                if (settings.InstantSiegeConstruction && ShouldApplyInstantConstructionToSiege(siegeEvent))
                {
                    return GameConstants.InstantSiegeConstructionProgress;
                }

                return baseProgress;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSiegeEventModel] Error in GetConstructionProgressPerHour: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return 0f;
            }
        }

        /// <summary>
        /// Determines if instant siege construction should be applied to the siege event.
        /// </summary>
        /// <param name="siegeEvent">The siege event to check. Cannot be null.</param>
        /// <returns>
        /// True if instant construction should be applied, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method checks if either the attacker or defender in the siege qualifies for the cheat.
        /// It considers:
        /// </para>
        /// <para>
        /// 1. Attacker side: If the besieging party is the player or a targeted NPC
        /// 2. Defender side: If the besieged settlement is owned by the player or a targeted NPC
        /// </para>
        /// <para>
        /// This ensures that both offensive and defensive sieges can benefit from instant construction.
        /// </para>
        /// </remarks>
        private bool ShouldApplyInstantConstructionToSiege(SiegeEvent siegeEvent)
        {
            // Check attacker side
            if (ShouldApplyInstantConstructionToAttacker(siegeEvent))
            {
                return true;
            }

            // Check defender side
            return ShouldApplyInstantConstructionToDefender(siegeEvent);
        }

        /// <summary>
        /// Determines if instant construction should be applied to the attacking side.
        /// </summary>
        /// <param name="siegeEvent">The siege event to check. Cannot be null.</param>
        /// <returns>
        /// True if the attacker should have instant construction, false otherwise.
        /// </returns>
        /// <remarks>
        /// Checks if the besieging party leader is the player or a targeted NPC.
        /// </remarks>
        private bool ShouldApplyInstantConstructionToAttacker(SiegeEvent siegeEvent)
        {
            MobileParty? besiegerParty = siegeEvent.BesiegerCamp?.LeaderParty;

            if (besiegerParty == null)
            {
                return false;
            }

            // Early exit if settings are null
            CheatTargetSettings? targetSettings = TargetSettings;
            if (targetSettings is null)
            {
                return false;
            }

            // Check if attacker is player
            if (besiegerParty == MobileParty.MainParty && targetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if attacker is a targeted NPC
            return besiegerParty != MobileParty.MainParty &&
                targetSettings.HasAnyNPCTargetEnabled() &&
                Utils.TargetFilter.ShouldApplyCheatToParty(besiegerParty);
        }

        /// <summary>
        /// Determines if instant construction should be applied to the defending side.
        /// </summary>
        /// <param name="siegeEvent">The siege event to check. Cannot be null.</param>
        /// <returns>
        /// True if the defender should have instant construction, false otherwise.
        /// </returns>
        /// <remarks>
        /// Checks if the besieged settlement is owned by the player or a targeted NPC clan.
        /// </remarks>
        private bool ShouldApplyInstantConstructionToDefender(SiegeEvent siegeEvent)
        {
            Settlement defenderSettlement = siegeEvent.BesiegedSettlement;

            if (defenderSettlement == null)
            {
                return false;
            }

            // Early exit if settings are null
            CheatTargetSettings? targetSettings = TargetSettings;
            if (targetSettings is null)
            {
                return false;
            }

            // Check if defender is player's settlement
            if (defenderSettlement.OwnerClan == Clan.PlayerClan && targetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if defender is a targeted NPC's settlement
            return defenderSettlement.OwnerClan != null &&
                defenderSettlement.OwnerClan != Clan.PlayerClan &&
                targetSettings.HasAnyNPCTargetEnabled() &&
                Utils.TargetFilter.ShouldApplyCheatToClan(defenderSettlement.OwnerClan);
        }

    }
}
