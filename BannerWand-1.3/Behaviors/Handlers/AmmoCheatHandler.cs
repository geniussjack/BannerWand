#nullable enable
// System namespaces
using System.Collections.Generic;

// Third-party namespaces
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

// Project namespaces
using BannerWand.Interfaces;
using BannerWand.Patches;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handles ammo-related cheats in combat.
    /// </summary>
    /// <remarks>
    /// This class encapsulates ammo cheat logic, making it easier to test
    /// and maintain. It implements <see cref="IAmmoCheatHandler"/> for dependency injection.
    /// </remarks>
    public class AmmoCheatHandler : IAmmoCheatHandler
    {
        /// <summary>
        /// Tracks whether unlimited ammo has been logged for current mission.
        /// </summary>
        private bool _unlimitedAmmoLogged;

        /// <summary>
        /// Tracks maximum ammo per weapon slot to restore when it drops.
        /// </summary>
        private readonly Dictionary<EquipmentIndex, short> _ammoMaxBySlot = [];

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

        /// <inheritdoc />
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

                // Skip empty slots or weapons without ammo
                if (weapon.IsEmpty || weapon.CurrentUsageItem == null || weapon.ModifiedMaxAmount <= 0)
                {
                    _ = _ammoMaxBySlot.Remove(i);
                    continue;
                }

                // Track max ammo for this slot (account for buffs)
                short maxAmmo = weapon.ModifiedMaxAmount;
                if (_ammoMaxBySlot.TryGetValue(i, out short existingMax))
                {
                    if (maxAmmo < existingMax)
                    {
                        maxAmmo = existingMax;
                    }
                    _ammoMaxBySlot[i] = maxAmmo;
                }
                else
                {
                    _ammoMaxBySlot[i] = maxAmmo;
                }

                short currentAmmo = weapon.Amount;
                if (currentAmmo < _ammoMaxBySlot[i])
                {
                    bool patchApplied = AmmoConsumptionPatch.IsPatchApplied;
                    if (patchApplied)
                    {
                        AmmoConsumptionPatch.IsRestorationInProgress = true;
                    }
                    try
                    {
                        agent.SetWeaponAmountInSlot(i, _ammoMaxBySlot[i], true);
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
                        ModLogger.Log($"[UnlimitedAmmo] Restored ammo to max via tick: {weaponName} ({currentAmmo} -> {_ammoMaxBySlot[i]})");
                    }
                }
            }
        }

        /// <inheritdoc />
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
            _ammoMaxBySlot.Clear();
        }
    }
}

