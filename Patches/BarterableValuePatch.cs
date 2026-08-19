#nullable enable
using BannerWand.Settings;
using BannerWand.Utils;
using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to make player barter offers always appear fair (green bar).
    /// Makes NPC items cheap and player items valuable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Barterable.GetValueForFaction is not just used for goods trades - it's the shared,
    /// unoverridden base implementation for every negotiation type in the game, including
    /// DeclareWarBarterable, FiefBarterable, JoinKingdomAsClanBarterable,
    /// LeaveKingdomAsClanBarterable, MarriageBarterable, PeaceBarterable,
    /// TransferPrisonerBarterable and more (verified against the installed game DLL - none
    /// of these subclasses override the method, they all fall through to this one). Patching
    /// the base method therefore intercepts ALL of them, not just item/gold trades.
    /// </para>
    /// <para>
    /// The Postfix restricts itself to <see cref="GoldBarterable"/> and
    /// <see cref="ItemBarterable"/> - the only two subtypes that represent an actual goods
    /// trade - so it can never inflate the value of a war declaration, kingdom membership
    /// change, fief transfer, marriage, or prisoner exchange. An earlier attempt at fixing
    /// this only checked whether the player was the item's OriginalOwner, which still let
    /// the multiplier reach every non-trade Barterable subtype and destabilized AI diplomacy,
    /// with kingdoms losing or gaining clans and fiefs seemingly at random.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(Barterable), nameof(Barterable.GetValueForFaction))]
    public static class BarterableValuePatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatSettings? TargetSettings => CheatSettings.Instance;
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

                // Only actual goods trades - never war/peace/marriage/kingdom-membership/fief/
                // prisoner/mercenary/safe-passage negotiations, which share this same method
                if (__instance is not (GoldBarterable or ItemBarterable))
                {
                    return;
                }

                // Only manipulate values when player is the actual item owner
                // Do NOT manipulate based on evaluator faction - this would affect NPC-to-NPC barters
                // that just happen to be evaluated by player's faction
                bool playerIsOwner = __instance.OriginalOwner == Hero.MainHero;

                // If player is not the owner, this is an NPC barter - don't modify!
                // Even if player's faction is evaluating, we shouldn't affect NPC-to-NPC transactions
                if (!playerIsOwner)
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
                const int valuableItemMultiplier = 1000;

                // Strategy: Make player items super valuable (player is GIVING)
                // This makes NPCs think they're getting a great deal
                // Note: We only modify player's items (already checked above that playerIsOwner is true)
                if (__result > 0)
                {
                    __result *= valuableItemMultiplier;
                }
                else if (__result < 0)
                {
                    __result = Math.Abs(__result) * valuableItemMultiplier;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[BarterableValuePatch] Error in Postfix: {ex.Message}");
            }
        }

    }
}
