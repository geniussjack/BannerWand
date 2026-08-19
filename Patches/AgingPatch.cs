#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
// Third-party namespaces
using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to prevent character aging for the player and, once they reach
    /// <see cref="GameConstants.MinimumAgeForStopAging"/>, NPC heroes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// As of the currently installed game version, <c>Hero.BirthDay</c> no longer has a public
    /// setter, so aging can no longer be stopped by preventing writes to it (the approach this
    /// patch originally used, which is why it was disabled). Age is a value computed live from
    /// <c>BirthDay</c> and the current campaign time, so instead this patches the getter of
    /// <c>Hero.Age</c> directly: the first time it is read for an eligible hero, the returned
    /// value is cached as that hero's frozen age, and every later read for that hero returns the
    /// cached value instead of the real, still-advancing one.
    /// </para>
    /// <para>
    /// The frozen age is captured once per hero for the lifetime of the game process and is not
    /// reset when the cheat is toggled off and back on - re-enabling continues from the
    /// originally captured age rather than re-freezing at whatever the real age has become in
    /// the meantime.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(Hero), "get_Age")]
    internal static class AgingPatch
    {
        /// <summary>
        /// Frozen age per hero, populated the first time an eligible hero's age is read.
        /// </summary>
        private static readonly Dictionary<Hero, float> _frozenAges = [];

        /// <summary>
        /// Postfix that freezes the returned age for eligible heroes.
        /// </summary>
        /// <param name="__instance">The hero whose age was just computed.</param>
        /// <param name="__result">The freshly computed age, overwritten if this hero is eligible.</param>
        [HarmonyPostfix]
        private static void Postfix(Hero __instance, ref float __result)
        {
            try
            {
                if (__instance is null)
                {
                    return;
                }

                CheatSettings? settings = CheatSettings.Instance;
                CheatTargetSettings? targetSettings = CheatTargetSettings.Instance;
                if (settings is null || targetSettings is null)
                {
                    return;
                }

                bool isPlayer = __instance == Hero.MainHero;

                bool eligible = isPlayer
                    ? settings.StopPlayerAging && targetSettings.ApplyToPlayer
                    : settings.StopNPCAging && __result >= GameConstants.MinimumAgeForStopAging;

                if (!eligible)
                {
                    return;
                }

                if (!_frozenAges.TryGetValue(__instance, out float frozenAge))
                {
                    frozenAge = __result;
                    _frozenAges[__instance] = frozenAge;
                }

                __result = frozenAge;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AgingPatch] Error in Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Clears all frozen ages. Called on new game/save load so a previous campaign's frozen
        /// ages never leak into a new one.
        /// </summary>
        public static void ResetTracking()
        {
            _frozenAges.Clear();
        }
    }
}
