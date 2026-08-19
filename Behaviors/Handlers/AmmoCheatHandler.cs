#nullable enable
using BannerWand.Patches;
using BannerWand.Settings;
using BannerWand.Utils;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handles ammo-related cheats in combat.
    /// </summary>
    public class AmmoCheatHandler
    {
        /// <summary>
        /// Tracks whether unlimited ammo has been logged for current mission.
        /// </summary>
        private bool _unlimitedAmmoLogged;

        /// <summary>
        /// Ensures we log restoration only once per mission to avoid spam.
        /// </summary>
        private bool _ammoRestoredLogged;

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Applies unlimited ammo to the player agent.
        /// </summary>
        /// <param name="agent">The player agent to apply the cheat to.</param>
        public void ApplyUnlimitedAmmo(Agent agent)
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            if (!settings.UnlimitedAmmo || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            if (agent?.IsActive() != true)
            {
                return;
            }

            // Log unlimited ammo activation once per mission
            if (!_unlimitedAmmoLogged)
            {
                _unlimitedAmmoLogged = true;
                string patchStatus = AmmoConsumptionPatch.IsPatchApplied
                    ? "Harmony patch ACTIVE (consumption blocked at source); tick restore also enabled"
                    : "WARNING: Harmony patch NOT applied - using tick-based restoration";
                ModLogger.Log($"[UnlimitedAmmo] {patchStatus}");
            }

            // Tick-based restoration to max ammo (safe because runs only in mission, on main agent)
            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                MissionWeapon weapon = agent.Equipment[i];

                // Skip empty slots, shields (SetWeaponAmountInSlot is for stackable ammo/throwables
                // only - calling it on a shield slot has been observed to also repair the shield,
                // making Unlimited Shield Durability trigger even when that cheat is disabled),
                // and weapons without ammo
                if (weapon.IsEmpty || weapon.CurrentUsageItem == null ||
                    weapon.CurrentUsageItem?.IsShield == true || weapon.ModifiedMaxAmount <= 0)
                {
                    continue;
                }

                // Always restore to the weapon's current max, never a remembered historical
                // value: some weapon/loadout combinations (e.g. certain throwing weapons in
                // arena fights) briefly report an anomalously high ModifiedMaxAmount, and
                // caching "the highest value ever seen" would lock that in for the rest of
                // the mission, ballooning ammo counts (and encumbrance) far past normal.
                short maxAmmo = weapon.ModifiedMaxAmount;
                short currentAmmo = weapon.Amount;
                if (currentAmmo < maxAmmo)
                {
                    bool patchApplied = AmmoConsumptionPatch.IsPatchApplied;
                    if (patchApplied)
                    {
                        AmmoConsumptionPatch.IsRestorationInProgress = true;
                    }
                    try
                    {
                        agent.SetWeaponAmountInSlot(i, maxAmmo, true);
                    }
                    finally
                    {
                        if (patchApplied)
                        {
                            AmmoConsumptionPatch.IsRestorationInProgress = false;
                        }
                    }

                    if (!_ammoRestoredLogged)
                    {
                        _ammoRestoredLogged = true;
                        string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                        ModLogger.Log($"[UnlimitedAmmo] Restored ammo to max via tick: {weaponName} ({currentAmmo} -> {maxAmmo})");
                    }
                }
            }
        }

        /// <summary>
        /// Handles ammo restoration after consumption.
        /// </summary>
        /// <param name="agent">The agent whose ammo to restore.</param>
        public void RestoreAmmo(Agent agent)
        {
            ApplyUnlimitedAmmo(agent);
        }

        /// <summary>
        /// Resets the tracking flags for the next mission.
        /// </summary>
        public void ResetTracking()
        {
            _unlimitedAmmoLogged = false;
            _ammoRestoredLogged = false;
        }
    }
}

