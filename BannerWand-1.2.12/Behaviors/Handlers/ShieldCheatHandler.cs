#nullable enable
using BannerWandRetro.Interfaces;
using BannerWandRetro.Settings;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Behaviors.Handlers
{
    /// <summary>
    /// Handles shield-related cheats in combat.
    /// </summary>
    /// <remarks>
    /// This class encapsulates shield cheat logic, making it easier to test
    /// and maintain. It implements <see cref="IShieldCheatHandler"/> for dependency injection.
    /// </remarks>
    public class ShieldCheatHandler : IShieldCheatHandler
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <inheritdoc />
        public void ApplyUnlimitedShieldDurability(Agent agent)
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            if (!settings.UnlimitedShieldDurability || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            if (agent?.IsPlayerControlled is not true)
            {
                return;
            }

            // Iterate through weapon slots to find and repair shields
            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                MissionWeapon equipment = agent.Equipment[i];

                // Check if this slot contains a shield
                if (equipment.CurrentUsageItem?.IsShield is true)
                {
                    short maxHitPoints = equipment.ModifiedMaxHitPoints;

                    // Only repair if damaged (optimization: avoid unnecessary API calls)
                    if (equipment.HitPoints < maxHitPoints)
                    {
                        agent.ChangeWeaponHitPoints(i, maxHitPoints);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void OnAgentHit(Agent affectedAgent, Agent? affectorAgent)
        {
            // Restore shield durability for player (if enabled and applicable)
            if (affectedAgent is not null)
            {
                ApplyUnlimitedShieldDurability(affectedAgent);
            }
        }
    }
}

