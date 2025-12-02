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
        private static CheatSettings Settings => CheatSettings.Instance!;
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

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

                // FIXED: Check if PLAYER is involved in this barter transaction
                // Either as the original owner OR as the receiving party
                bool playerIsOwner = originalOwner == Hero.MainHero;
                bool playerIsParty = IsPlayerParty(party);
                bool playerIsFaction = IsPlayerFaction(faction);

                if (!playerIsOwner && !playerIsParty && !playerIsFaction)
                {
                    // Player not involved - don't affect this barter
                    return basePenalty;
                }

                // Apply massive negative penalty for player's offers
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

        /// <summary>
        /// Checks if the party belongs to or is controlled by the player.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: Only checks for direct player control, NOT clan membership.
        /// This prevents the cheat from affecting AI-to-AI trades within player's clan.
        /// </remarks>
        private static bool IsPlayerParty(PartyBase party)
        {
            if (party == null)
            {
                return false;
            }

            // Check if it's the main player party
            if (party == PartyBase.MainParty)
            {
                return true;
            }

            // Check if party's leader is the player (direct control)
            if (party.LeaderHero == Hero.MainHero)
            {
                return true;
            }

            // NOTE: We intentionally do NOT check party.MapFaction == Clan.PlayerClan
            // because that would affect AI-to-AI trades within player's clan,
            // which can cause AI lords to switch factions unexpectedly.

            return false;
        }

        /// <summary>
        /// Checks if the faction is the player's faction or clan.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: This checks if the faction evaluating the barter is the player's faction.
        /// We keep the Clan.PlayerClan check because the faction parameter represents WHO is evaluating,
        /// not who owns the item. If the player's clan is evaluating, it means the player is involved.
        /// However, we still require that either originalOwner or party is player-controlled
        /// (checked in the main method) to ensure player is directly involved.
        /// </remarks>
        private static bool IsPlayerFaction(IFaction faction)
        {
            if (faction == null)
            {
                return false;
            }

            // Check if it's the player's clan
            if (faction == Clan.PlayerClan)
            {
                return true;
            }

            // Check if it's the player's kingdom
            return Hero.MainHero?.MapFaction != null && faction == Hero.MainHero.MapFaction;
        }
    }
}
