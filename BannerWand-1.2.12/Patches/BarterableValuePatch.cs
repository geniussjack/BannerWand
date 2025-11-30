#nullable enable
using BannerWandRetro.Settings;
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
    [HarmonyPatch(typeof(Barterable), nameof(Barterable.GetValueForFaction))]
    public static class BarterableValuePatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Postfix patch that manipulates barter value calculations.
        /// Strategy: Make NPC items worthless (cheap) and player items super valuable.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(Barterable __instance, ref int __result)
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

            // Strategy 1: Make NPC items worthless (player is RECEIVING)
            const int worthlessItemValue = 1;
            const int valuableItemMultiplier = 1000;
            if (__instance.OriginalOwner != Hero.MainHero && __instance.OriginalOwner != null)
            {
                __result = worthlessItemValue;
                return;
            }

            // Strategy 2: Make player items super valuable (player is GIVING)
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
    }
}
