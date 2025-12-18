#nullable enable
// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to provide unlimited carrying capacity for player and NPC parties.
    /// Modifies the result of CalculateInventoryCapacity to add maximum capacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch uses Postfix to modify the capacity after the base calculation completes.
    /// This approach is more reliable than method override for API changes between versions.
    /// </para>
    /// <para>
    /// The patch adds ~1 million capacity when the cheat is enabled, making capacity
    /// effectively unlimited for any practical gameplay scenario.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.GameComponents.DefaultInventoryCapacityModel))]
    [HarmonyPatch("CalculateInventoryCapacity")]
    internal static class InventoryCapacityPatch
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
        /// Postfix patch that adds maximum capacity after base calculation.
        /// </summary>
        /// <param name="__result">The result from the original method (passed by reference).</param>
        /// <param name="mobileParty">The party whose capacity was calculated.</param>
        /// <remarks>
        /// <para>
        /// Postfix patches run AFTER the original method completes, allowing us to modify
        /// the return value. The __result parameter contains the ExplainedNumber returned
        /// by DefaultInventoryCapacityModel.CalculateInventoryCapacity.
        /// </para>
        /// <para>
        /// Harmony will match this method to any CalculateInventoryCapacity overload that has
        /// MobileParty as the first parameter. Additional parameters are ignored but must match
        /// the signature for Harmony to properly bind the patch.
        /// </para>
        /// <para>
        /// We modify __result by reference to add maximum capacity when conditions are met:
        /// 1. Cheat is enabled (MaxCarryingCapacity setting)
        /// 2. Party qualifies (player party or targeted NPC party)
        /// </para>
        /// </remarks>
#pragma warning disable RCS1213 // Remove unused method declaration
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter
        private static void Postfix(
            ref ExplainedNumber __result,
            MobileParty mobileParty,
            bool isCurrentlyAtSea = false,
            bool includeDescriptions = false,
            int additionalTroops = 0,
            int additionalSpareMounts = 0,
            int additionalPackAnimals = 0,
            bool includeFollowers = false)
#pragma warning restore RCS1163 // Unused parameter
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1213 // Remove unused method declaration
        {
            try
            {
                // Early exit for null or unconfigured settings
                if (Settings == null || TargetSettings == null || mobileParty == null)
                {
                    return;
                }

                // Check if cheat is enabled
                if (!Settings.MaxCarryingCapacity)
                {
                    return;
                }

                // Determine if this party should receive unlimited capacity
                bool shouldApplyMaxCapacity = ShouldApplyMaxCapacityToParty(mobileParty);

                if (shouldApplyMaxCapacity)
                {
                    // Calculate adjustment needed to reach maximum capacity
                    float capacityAdjustment = GameConstants.MaxCarryingCapacity - __result.ResultNumber;

                    // Add adjustment to result
                    __result.Add(capacityAdjustment, null);
                }
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"[InventoryCapacityPatch] Error in Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Determines if maximum carrying capacity should be applied to the specified party.
        /// </summary>
        /// <param name="mobileParty">The party to check. Cannot be null.</param>
        /// <returns>
        /// True if the party should receive maximum carrying capacity, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method centralizes the logic for determining if a party qualifies for the cheat.
        /// It checks two main conditions:
        /// </para>
        /// <para>
        /// 1. Player party: If this is the main player party and ApplyToPlayer is enabled
        /// 2. NPC parties: If this is a targeted NPC party (based on TargetFilter settings)
        /// </para>
        /// </remarks>
        private static bool ShouldApplyMaxCapacityToParty(MobileParty mobileParty)
        {
            // Early exit if party is null
            if (mobileParty == null || TargetSettings == null)
            {
                return false;
            }

            // Check if this is player party
            if (mobileParty == MobileParty.MainParty && TargetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if this is a targeted NPC party
            return mobileParty != MobileParty.MainParty &&
                   TargetSettings.HasAnyNPCTargetEnabled() &&
                   TargetFilter.ShouldApplyCheatToParty(mobileParty);
        }
    }
}
