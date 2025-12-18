#nullable enable
// System namespaces
using System;
using System.Reflection;

// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

// Project namespaces
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to add a fixed speed bonus to parties.
    /// Patches MobileParty.SpeedExplained to add a constant speed bonus when the cheat is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch is inspired by Character Reload's implementation, which patches
    /// MobileParty.CalculateSpeed and MobileParty.SpeedExplained to add speed bonuses.
    /// </para>
    /// <para>
    /// The patch intercepts the speed calculation at the MobileParty level and adds
    /// a fixed bonus value. This ensures the bonus is constant and doesn't fluctuate,
    /// unlike multiplier-based approaches that can cause values to jump around.
    /// </para>
    /// <para>
    /// NOTE: We only patch SpeedExplained, not CalculateSpeed, because:
    /// 1. CalculateSpeed typically calls SpeedExplained internally and returns ResultNumber
    /// 2. Patching both would cause double application of the bonus
    /// 3. SpeedExplained is the primary method used by the game for speed calculations
    /// </para>
    /// <para>
    /// The bonus is added to the calculated speed, preserving all terrain, composition,
    /// and other modifiers while ensuring a stable, predictable speed increase.
    /// </para>
    /// </remarks>
    [HarmonyPatch]
    public static class MobilePartySpeedPatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Gets the speed bonus for a mobile party if the cheat is enabled.
        /// Returns 0 if the cheat is disabled or doesn't apply to this party.
        /// </summary>
        /// <param name="mobileParty">The mobile party to get speed bonus for.</param>
        /// <returns>
        /// The speed bonus value (0-16) if cheat is enabled, or 0 if disabled/not applicable.
        /// This value will be added to the calculated speed.
        /// </returns>
        private static float GetSpeedBonus(MobileParty mobileParty)
        {
            if (mobileParty == null || Settings == null || TargetSettings == null)
            {
                return 0f;
            }

            // Check for player speed bonus (only for player's main party)
            if (mobileParty == MobileParty.MainParty &&
                Settings.MovementSpeed > 0f &&
                TargetSettings.ApplyToPlayer &&
                Campaign.Current != null)
            {
                // Cap to maximum allowed value (16.0)
                return Math.Min(Settings.MovementSpeed, 16.0f);
            }

            // Check for NPC speed bonus (only for non-player parties)
            if (mobileParty != MobileParty.MainParty &&
                Settings.NPCMovementSpeed > 0f &&
                Campaign.Current != null)
            {
                // Cap to maximum allowed value (16.0)
                return Math.Min(Settings.NPCMovementSpeed, 16.0f);
            }

            return 0f;
        }

        /// <summary>
        /// Text object for speed bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TaleWorlds.Localization.TextObject SpeedBonusText = new("BannerWand Speed Bonus");

        /// <summary>
        /// Harmony patch target method - MobileParty.SpeedExplained
        /// </summary>
        [HarmonyTargetMethod]
        public static MethodBase? TargetSpeedExplainedMethod()
        {
            try
            {
                MethodInfo? method = typeof(MobileParty).GetMethod(
                    "SpeedExplained",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(bool)],
                    null);
                if (method != null)
                {
                    ModLogger.Log("[MobilePartySpeedPatch] Found MobileParty.SpeedExplained method");
                    return method;
                }

                ModLogger.Warning("[MobilePartySpeedPatch] MobileParty.SpeedExplained method not found");
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[MobilePartySpeedPatch] Error finding SpeedExplained method: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Postfix for MobileParty.SpeedExplained that adds a speed bonus
        /// if the cheat is enabled.
        /// </summary>
        /// <param name="__instance">The MobileParty instance.</param>
        /// <param name="__result">The calculated speed ExplainedNumber (modified by ref).</param>
        [HarmonyPostfix]
        public static void SpeedExplained_Postfix(MobileParty __instance, ref ExplainedNumber __result)
        {
            try
            {
                if (__instance == null)
                {
                    return;
                }

                float speedBonus = GetSpeedBonus(__instance);
                if (speedBonus > 0f)
                {
                    // Add the bonus to the calculated speed
                    // This ensures the bonus is constant and doesn't fluctuate
                    // Similar to Character Reload's implementation
                    __result.Add(speedBonus, SpeedBonusText, null);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[MobilePartySpeedPatch] Error in SpeedExplained_Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
