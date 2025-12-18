#nullable enable
// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;

// Project namespaces
using BannerWand.Settings;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom clan tier model that adds bonus companion limit for player and NPC clans.
    /// Extends <see cref="DefaultClanTierModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all clan tier calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Clan Companions Limit: Adds bonus to companion limit for all clans (player and NPC)
    /// </para>
    /// <para>
    /// Base game behavior:
    /// - Companion limit is calculated from clan tier (tier + 3) plus perks
    /// - Perks like "We Pledge our Swords" and "Camaraderie" add additional companions
    /// </para>
    /// <para>
    /// Cheat behavior:
    /// - If bonus > 0, adds the configured bonus to all clans' companion limit
    /// - Works for both player and NPC clans
    /// - Compatible with mods that allow NPCs to recruit companions
    /// </para>
    /// </remarks>
    public class CustomClanTierModel : DefaultClanTierModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Calculates the companion limit for a clan.
        /// Adds bonus companion limit if enabled.
        /// </summary>
        /// <param name="clan">The clan to calculate companion limit for. Cannot be null.</param>
        /// <returns>
        /// The maximum number of companions the clan can have.
        /// Base value from tier and perks, plus configured bonus if enabled.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates base limit from clan tier (tier + 3)
        /// - Adds bonuses from perks (Leadership "We Pledge our Swords", Charm "Camaraderie")
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If ClanCompanionsLimit > 0, adds the bonus to the result
        /// - Applies to all clans (player and NPC)
        /// </para>
        /// </remarks>
        public override int GetCompanionLimit(Clan clan)
        {
            // Get base limit from default implementation (tier + perks)
            int baseLimit = base.GetCompanionLimit(clan);

            // Early exit if settings not available or bonus not enabled
            if (Settings == null || clan == null)
            {
                return baseLimit;
            }

            // Add bonus if enabled
            if (Settings.ClanCompanionsLimit > 0)
            {
                int bonus = Settings.ClanCompanionsLimit;
                return baseLimit + bonus;
            }

            return baseLimit;
        }
    }
}
