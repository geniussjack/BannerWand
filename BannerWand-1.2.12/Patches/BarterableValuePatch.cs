#nullable enable
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;

namespace BannerWandRetro.Patches
{
    /// <summary>
    /// Harmony patch to make player barter offers always appear fair (green bar).
    /// Makes NPC items cheap and player items valuable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FIXED (v1.0.9): Now properly checks if PLAYER is involved in the barter transaction.
    /// Previously, the cheat affected ALL barter transactions in the game,
    /// causing clans to switch kingdoms unexpectedly due to AI-to-AI barters being affected.
    /// </para>
    /// <para>
    /// The fix ensures the value manipulation only applies when the player is directly
    /// involved in the transaction (either as buyer or seller).
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(Barterable), nameof(Barterable.GetValueForFaction))]
    public static class BarterableValuePatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;
        private static bool _firstLogDone = false;

        /// <summary>
        /// Postfix patch that manipulates barter value calculations.
        /// Strategy: Make NPC items worthless (cheap) and player items super valuable.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The IFaction parameter tells us WHO is evaluating the value.
        /// If neither the evaluator nor the item owner is the player, we don't modify anything.
        /// </para>
        /// </remarks>
        [HarmonyPostfix]
        private static void Postfix(Barterable __instance, IFaction faction, ref int __result)
        {
            try
            {
                // Early exit if settings not configured
                if (Settings is null || TargetSettings is null)
                {
                    return;
                }

                // Early exit if cheat not enabled
                if (!Settings.BarterAlwaysAccepted || !TargetSettings.ApplyToPlayer)
                {
                    return;
                }

                // Skip if no player hero
                if (Hero.MainHero == null)
                {
                    return;
                }

                // CRITICAL FIX: Check if player is involved in this barter transaction
                // A barter involves:
                // 1. The item/offer owner (OriginalOwner)
                // 2. The faction evaluating the offer (faction parameter)
                bool playerIsOwner = __instance.OriginalOwner == Hero.MainHero;
                bool playerIsEvaluator = IsPlayerFaction(faction);

                // If player is neither owner nor evaluator, this is an AI-to-AI barter - don't modify!
                if (!playerIsOwner && !playerIsEvaluator)
                {
                    return;
                }

                // Log first application for debugging
                if (!_firstLogDone)
                {
                    _firstLogDone = true;
                    string ownerName = __instance.OriginalOwner?.Name?.ToString() ?? "null";
                    string factionName = faction?.Name?.ToString() ?? "null";
                    ModLogger.Log($"[Barter] First value modification - Owner: {ownerName}, Evaluator: {factionName}, PlayerInvolved: true");
                }

                // Strategy constants
                const int worthlessItemValue = 1;
                const int valuableItemMultiplier = 1000;

                // Strategy 1: Make NPC items worthless (player is RECEIVING from NPC)
                // This makes it cheap for player to get items
                if (__instance.OriginalOwner != Hero.MainHero && __instance.OriginalOwner != null)
                {
                    __result = worthlessItemValue;
                    return;
                }

                // Strategy 2: Make player items super valuable (player is GIVING)
                // This makes NPCs think they're getting a great deal
                if (__instance.OriginalOwner == Hero.MainHero)
                {
                    if (__result > 0)
                    {
                        __result *= valuableItemMultiplier;
                    }
                    else if (__result < 0)
                    {
                        __result = Math.Abs(__result) * valuableItemMultiplier;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[BarterableValuePatch] Error in Postfix: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the faction is the player's faction or clan.
        /// </summary>
        private static bool IsPlayerFaction(IFaction? faction)
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
            if (Hero.MainHero?.MapFaction != null && faction == Hero.MainHero.MapFaction)
            {
                return true;
            }

            // Check if the faction leader is the player
            return faction is Clan clan && clan.Leader == Hero.MainHero;
        }
    }
}
