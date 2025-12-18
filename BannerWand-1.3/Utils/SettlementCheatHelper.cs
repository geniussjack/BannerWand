#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

// Project namespaces
using BannerWand.Settings;
using static BannerWand.Utils.ModLogger;

namespace BannerWand.Utils
{
    /// <summary>
    /// Utility class for determining if settlement cheats should be applied.
    /// Centralizes the logic for checking if a settlement qualifies for cheat effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a consistent way to check if settlement cheats should apply
    /// to a given settlement, whether it's player-owned or NPC-owned.
    /// </para>
    /// <para>
    /// The logic follows the same pattern as other cheat targeting:
    /// - Player settlements: Check if ApplyToPlayer is enabled
    /// - NPC settlements: Check if any NPC target is enabled and if the clan matches target criteria
    /// </para>
    /// </remarks>
    public static class SettlementCheatHelper
    {
        /// <summary>
        /// Determines if cheats should be applied to the specified settlement.
        /// </summary>
        /// <param name="settlement">The settlement to check. Cannot be null.</param>
        /// <returns>
        /// True if the settlement should receive cheats, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method checks three main conditions:
        /// 1. Player-owned settlements: If the settlement belongs to the player's clan
        /// 2. Rebelling settlements: If the settlement is rebelling, it's treated as an NPC settlement and checked against NPC target settings
        /// 3. NPC-owned settlements: If the settlement belongs to a targeted NPC clan (based on TargetFilter settings)
        /// </para>
        /// <para>
        /// Returns false if:
        /// - Settlement is null
        /// - OwnerClan is null (unless it's a rebelling settlement)
        /// - TargetSettings is null
        /// - Settlement doesn't match any target criteria
        /// </para>
        /// </remarks>
        public static bool ShouldApplyCheatToSettlement(Settlement? settlement)
        {
            // Early exit for null settlement
            if (settlement == null)
            {
                return false;
            }

            // Early exit if target settings are null
            CheatTargetSettings? targetSettings = CheatTargetSettings.Instance;
            if (targetSettings == null)
            {
                return false;
            }

            // Check if this is player's settlement
            // For settlement cheats, we always apply to player settlements (settlements are not heroes)
            // ApplyToPlayer is for hero cheats, not settlement cheats
            if (settlement.OwnerClan == Clan.PlayerClan)
            {
                ModLogger.Debug($"[SettlementCheatHelper] Player settlement {settlement.Name} qualifies for cheats");
                return true;
            }

            // Check if this is a rebelling settlement
            // Rebelling settlements should be treated as NPC settlements and checked against NPC target settings
            bool isRebellingSettlement = IsRebellingSettlement(settlement);
            // Early exit if owner clan is null (and it's not a rebelling settlement)
            if (settlement.OwnerClan == null && !isRebellingSettlement)
            {
                return false;
            }

            // SPECIAL CASE: Rebelling settlements require special handling
            // Rebelling settlements can originate from any clan (player or NPC), and may have null OwnerClan
            // We treat them as NPC settlements and check against NPC target settings
            // This ensures cheats apply to rebelling settlements regardless of their original owner
            if (isRebellingSettlement)
            {
                // Check if NPC targets are enabled and if the rebelling settlement qualifies
                // For rebelling settlements, we check if the owner clan (if exists) matches NPC criteria
                // or if NPC targets are enabled for all settlements
                if (targetSettings.HasAnyNPCTargetEnabled())
                {
                    // If owner clan exists, check if it matches NPC criteria
                    // This allows selective application based on clan type (vassals, independent, etc.)
                    if (settlement.OwnerClan != null)
                    {
                        bool rebellingQualifies = TargetFilter.ShouldApplyCheatToClan(settlement.OwnerClan);
                        if (rebellingQualifies)
                        {
                            ModLogger.Debug($"[SettlementCheatHelper] Rebelling settlement {settlement.Name} (owner: {settlement.OwnerClan.Name}) qualifies for cheats via NPC targets");
                            return true;
                        }
                    }
                    else
                    {
                        // If owner clan is null (common for rebelling settlements), apply cheats to all
                        // rebelling settlements when NPC targets are enabled
                        // This ensures rebelling settlements get cheats regardless of owner
                        // Rationale: Rebelling settlements without owners are typically in a transitional state
                        ModLogger.Debug($"[SettlementCheatHelper] Rebelling settlement {settlement.Name} (no owner clan) qualifies for cheats via NPC targets");
                        return true;
                    }
                }
            }

            // Check if this is a targeted NPC settlement
            // HasAnyNPCTargetEnabled checks if any NPC target options are enabled (companions, vassals, etc.)
            // ShouldApplyCheatToClan checks if the specific clan matches the target criteria
            if (settlement.OwnerClan == null)
            {
                return false;
            }

            bool qualifies = settlement.OwnerClan != Clan.PlayerClan &&
                             targetSettings.HasAnyNPCTargetEnabled() &&
                             TargetFilter.ShouldApplyCheatToClan(settlement.OwnerClan);
            if (qualifies)
            {
                ModLogger.Debug($"[SettlementCheatHelper] NPC settlement {settlement.Name} (owner: {settlement.OwnerClan.Name}) qualifies for cheats");
            }
            else
            {
                string ownerName = settlement.OwnerClan?.Name?.ToString() ?? "null";
                ModLogger.Debug($"[SettlementCheatHelper] NPC settlement {settlement.Name} (owner: {ownerName}) does NOT qualify for cheats (HasAnyNPCTargetEnabled: {targetSettings.HasAnyNPCTargetEnabled()}, ShouldApplyCheatToClan: {TargetFilter.ShouldApplyCheatToClan(settlement.OwnerClan)})");
            }
            return qualifies;
        }

        /// <summary>
        /// Checks if a settlement is currently rebelling.
        /// </summary>
        /// <param name="settlement">The settlement to check.</param>
        /// <returns>True if the settlement is rebelling, false otherwise.</returns>
        private static bool IsRebellingSettlement(Settlement settlement)
        {
            try
            {
                // Check if settlement has a garrison party that is rebelling
                // Rebelling settlements typically have a garrison party
                // We check if the garrison party's faction is different from the settlement's owner faction
                if (settlement.Town?.GarrisonParty != null)
                {
                    // If garrison party exists and owner clan is null, it's likely rebelling
                    // Or if garrison party's faction doesn't match owner clan's faction
                    if (settlement.OwnerClan == null)
                    {
                        return true;
                    }
                    // Check if garrison party's faction is different from owner clan's faction
                    // This indicates the settlement is rebelling
                    if (settlement.Town.GarrisonParty.MapFaction != null &&
                        settlement.OwnerClan.MapFaction != null &&
                        settlement.Town.GarrisonParty.MapFaction != settlement.OwnerClan.MapFaction)
                    {
                        return true;
                    }
                }

                // Check if OwnerClan is a minor faction (rebel clan) without a kingdom
                // Rebel clans are typically minor factions that don't belong to any kingdom
                if (settlement.OwnerClan?.IsMinorFaction == true &&
                    settlement.OwnerClan.Kingdom == null)
                {
                    // Additional check: rebel clans usually have specific naming patterns or are not player clan
                    // But we want to catch all rebel clans, so we check if it's not the player clan
                    if (settlement.OwnerClan != Clan.PlayerClan)
                    {
                        return true;
                    }
                }

                // Check if OwnerClan is null (common for rebelling settlements)
                // But only if there's a garrison party (indicates it's not just unowned)
                return settlement.OwnerClan == null && settlement.Town?.GarrisonParty != null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[SettlementCheatHelper] Error checking if settlement is rebelling: {ex.Message}");
                return false;
            }
        }

    }
}
