using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom food consumption model that can disable food consumption for player and NPC parties.
    /// Extends <see cref="DefaultMobilePartyFoodConsumptionModel"/> to add cheat functionality without Harmony patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>.
    /// However, this feature is also backed up by <see cref="Behaviors.FoodCheatBehavior"/> which
    /// periodically adds food as a safety net.
    /// </para>
    /// <para>
    /// Cheat feature provided:
    /// - Unlimited Food: Prevents player and targeted NPC parties from consuming food
    /// </para>
    /// <para>
    /// Food mechanics in Bannerlord:
    /// - Parties consume food daily based on troop count
    /// - Running out of food causes morale penalties and troop desertions
    /// - Different food types provide variety bonuses
    /// - This cheat completely bypasses the food system
    /// </para>
    /// <para>
    /// Why this approach is safe:
    /// - Returns false (party doesn't consume food) rather than modifying consumption amount
    /// - Leverages official API method designed for this purpose
    /// - Game engine handles all edge cases (garrisoned troops, no troops, etc.)
    /// </para>
    /// </remarks>
    public class CustomMobilePartyFoodConsumptionModel : DefaultMobilePartyFoodConsumptionModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Determines whether a party consumes food, with cheat override for unlimited food.
        /// Overrides <see cref="DefaultMobilePartyFoodConsumptionModel.DoesPartyConsumeFood"/>.
        /// </summary>
        /// <param name="mobileParty">The party to check. Cannot be null.</param>
        /// <returns>
        /// False if party should not consume food (cheat enabled for this party),
        /// otherwise returns the base implementation result.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Decision flow:
        /// 1. Call base implementation to get default behavior
        /// 2. If base says "no consumption", respect that (garrisoned, besieged, etc.)
        /// 3. If unlimited food enabled and this is player/NPC party, override to "no consumption"
        /// 4. Otherwise return base result
        /// </para>
        /// <para>
        /// Why call base first:
        /// - Respects special game states (garrisoned parties don't consume food)
        /// - Ensures compatibility with game mechanics
        /// - Only overrides when necessary
        /// </para>
        /// <para>
        /// Performance: Called during daily party updates.
        /// Very lightweight - just boolean checks and one method call.
        /// </para>
        /// </remarks>
        public override bool DoesPartyConsumeFood(MobileParty mobileParty)
        {
            try
            {
                bool baseResult = base.DoesPartyConsumeFood(mobileParty);

                // Early return if party naturally doesn't consume food
                if (!baseResult)
                {
                    return false;
                }

                // Safe null checks
                CheatSettings settings = Settings;
                CheatTargetSettings targetSettings = TargetSettings;

                if (settings == null || targetSettings == null || mobileParty == null)
                {
                    return baseResult;
                }

                // Check if unlimited food cheat is enabled and applies to this party
                return (!settings.UnlimitedFood || !ShouldApplyUnlimitedFoodToParty(mobileParty)) && baseResult;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomMobilePartyFoodConsumptionModel] Error in DoesPartyConsumeFood: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Determines if unlimited food should be applied to the party.
        /// </summary>
        private bool ShouldApplyUnlimitedFoodToParty(MobileParty mobileParty)
        {
            // Check if this is player's party
            if (mobileParty == MobileParty.MainParty && TargetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if this is a targeted NPC party
            return mobileParty != MobileParty.MainParty &&
                   TargetSettings.HasAnyNPCTargetEnabled() &&
                   TargetFilter.ShouldApplyCheatToParty(mobileParty);
        }
    }
}
