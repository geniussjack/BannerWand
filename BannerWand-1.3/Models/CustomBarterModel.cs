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
    /// <remarks>
    /// <para>
    /// FIXED: Now properly checks if PLAYER is involved in the barter transaction.
    /// Previously, the cheat affected ALL barter transactions in the game,
    /// causing clans to switch kingdoms unexpectedly.
    /// </para>
    /// <para>
    /// The fix ensures the penalty only applies when:
    /// 1. The cheat is enabled
    /// 2. ApplyToPlayer is true
    /// 3. The player is one of the parties in the barter
    /// </para>
    /// </remarks>
    public class CustomBarterModel : DefaultBarterModel
    {
        private static CheatSettings Settings => CheatSettings.Instance;
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Gets the barter penalty for an offer.
        /// Adds massive negative penalty to force acceptance ONLY for player's barters.
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

                // Early exit if cheat is disabled
                if (!Settings.BarterAlwaysAccepted || !TargetSettings.ApplyToPlayer)
                {
                    return basePenalty;
                }

                // FIXED: Only apply penalty when PLAYER is the item owner (giving items)
                // Do NOT apply when player is receiving items (party/faction check)
                // The penalty should only affect offers where player is giving items away
                bool playerIsOwner = originalOwner == Hero.MainHero;

                if (!playerIsOwner)
                {
                    // Player is not the owner - don't affect this barter
                    // This prevents unintended behavior when NPCs evaluate offers to the player
                    return basePenalty;
                }

                // Apply massive negative penalty ONLY for player's offers (when player is giving items)
                basePenalty.Add(GameConstants.BarterAutoAcceptPenalty, null);

                ModLogger.Debug($"[Barter] Penalty applied - Owner: {originalOwner?.Name}, Party: {party?.Name}, Faction: {faction?.Name}");

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
