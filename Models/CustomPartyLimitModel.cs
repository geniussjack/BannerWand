#nullable enable
// System namespaces
using System;
// Project namespaces
using BannerWand.Settings;
using BannerWand.Utils;
// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party limit model that adds bonuses to party size limit and garrison capacity.
    /// Extends <see cref="DefaultPartySizeLimitModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all party size limit calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Party Size Limit Bonus: Adds bonus to party size limit for player's main party only
    /// - Garrison Capacity Bonus: Adds bonus to the maximum troops a settlement's garrison can hold
    /// </para>
    /// <para>
    /// Base game behavior:
    /// - Party size limit is calculated from clan tier, steward skill, perks, and buildings
    /// - Formula: Base (25 + Clan Tier * 15) + Steward Skill + Perks + Buildings
    /// </para>
    /// <para>
    /// Cheat behavior:
    /// - If PartySizeLimit > 0 and ApplyToPlayer, adds the bonus to player's main party
    /// - Does not affect clan parties or NPC parties
    /// </para>
    /// </remarks>
    public class CustomPartyLimitModel : DefaultPartySizeLimitModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Text object for party size bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject PartySizeBonusText = new("BannerWand Party Size Bonus");

        /// <summary>
        /// Text object for garrison capacity bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject GarrisonCapacityBonusText = new("BannerWand Garrison Capacity Bonus");

        /// <summary>
        /// Calculates the party size limit for a party.
        /// Adds bonus to player's main party if enabled.
        /// </summary>
        /// <param name="party">The party to calculate limit for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the party size limit value.
        /// Base value from clan tier, steward, perks, and buildings, plus configured bonus if enabled.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates base limit from clan tier (25 + tier * 15)
        /// - Adds bonuses from steward skill, perks, and buildings
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If PartySizeLimit > 0 and ApplyToPlayer, adds the bonus to player's main party
        /// - Only applies to MobileParty.MainParty (player's party)
        /// - Does not affect clan parties or NPC parties
        /// </para>
        /// <para>
        /// The base method now takes a <see cref="PartyBase"/> instead of a <see cref="MobileParty"/> -
        /// this override narrows to <see cref="PartyBase.MobileParty"/> to keep the same targeting as
        /// before.
        /// </para>
        /// </remarks>
        public override ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
        {
            try
            {
                // Get base limit from default implementation
                ExplainedNumber baseLimit = base.GetPartyMemberSizeLimit(party, includeDescriptions);

                // Early exit if settings not available or party is null
                if (Settings == null || TargetSettings == null || party?.MobileParty == null)
                {
                    return baseLimit;
                }

                // Only apply to player's main party
                if (party.MobileParty != MobileParty.MainParty)
                {
                    return baseLimit;
                }

                // Check if should apply to player
                if (!TargetSettings.ApplyToPlayer)
                {
                    return baseLimit;
                }

                // Add bonus if enabled
                if (Settings.PartySizeLimit > 0)
                {
                    int bonus = Settings.PartySizeLimit;
                    baseLimit.Add(bonus, PartySizeBonusText);
                    ModLogger.DebugThrottled($"[CustomPartyLimitModel] Applied party size bonus +{bonus} to player's party: {baseLimit.ResultNumber:F0}");
                }

                return baseLimit;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyLimitModel] Error in GetPartyMemberSizeLimit: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.GetPartyMemberSizeLimit(party, includeDescriptions);
            }
        }

        /// <summary>
        /// Calculates the maximum number of troops a settlement's garrison can hold.
        /// Adds the configured bonus if enabled and the settlement qualifies.
        /// </summary>
        /// <param name="settlement">The settlement to calculate garrison capacity for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the garrison size limit.
        /// Base value from settlement type/level/buildings, plus configured bonus if enabled.
        /// </returns>
        /// <remarks>
        /// Applies to player-owned settlements and targeted NPC settlements, matching the pattern
        /// used by the other Settlements-category cheats (garrison recruitment, loyalty, etc.) via
        /// <see cref="SettlementCheatHelper"/>.
        /// </remarks>
        public override ExplainedNumber CalculateGarrisonPartySizeLimit(Settlement settlement, bool includeDescriptions = false)
        {
            try
            {
                ExplainedNumber baseLimit = base.CalculateGarrisonPartySizeLimit(settlement, includeDescriptions);

                if (Settings == null || settlement == null)
                {
                    return baseLimit;
                }

                if (Settings.GarrisonCapacity > 0 && SettlementCheatHelper.ShouldApplyCheatToSettlement(settlement))
                {
                    int bonus = Settings.GarrisonCapacity;
                    baseLimit.Add(bonus, GarrisonCapacityBonusText);
                    ModLogger.DebugThrottled($"[CustomPartyLimitModel] Applied garrison capacity bonus +{bonus} to {settlement.Name}: {baseLimit.ResultNumber:F0}");
                }

                return baseLimit;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomPartyLimitModel] Error in CalculateGarrisonPartySizeLimit: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.CalculateGarrisonPartySizeLimit(settlement, includeDescriptions);
            }
        }
    }
}
