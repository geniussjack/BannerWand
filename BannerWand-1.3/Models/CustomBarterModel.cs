using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom barter model that adds penalty to force acceptance.
    /// Combined with BarterableValuePatch for complete barter cheat.
    /// </summary>
    public class CustomBarterModel : DefaultBarterModel
    {
        private static CheatSettings Settings => CheatSettings.Instance!;
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        /// <summary>
        /// Gets the barter penalty for an offer.
        /// Adds massive negative penalty to force acceptance.
        /// </summary>
        public override ExplainedNumber GetBarterPenalty(IFaction faction, ItemBarterable barterable, Hero originalOwner, PartyBase party)
        {
            try
            {
                ExplainedNumber basePenalty = base.GetBarterPenalty(faction, barterable, originalOwner, party);

                if (Settings == null || TargetSettings == null)
                {
                    return basePenalty;
                }

                // Apply massive negative penalty for player offers
                if (Settings.BarterAlwaysAccepted &&
                    TargetSettings.ApplyToPlayer &&
                    originalOwner == Hero.MainHero)
                {
                    basePenalty.Add(GameConstants.BarterAutoAcceptPenalty, null);
                }

                return basePenalty;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomBarterModel] Error in GetBarterPenalty: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return new ExplainedNumber(0f);
            }
        }
    }
}
