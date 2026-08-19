#nullable enable
using BannerWand.Settings;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handles shield-related cheats in combat.
    /// </summary>
    public class ShieldCheatHandler
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
        /// Applies unlimited shield durability to an agent.
        /// </summary>
        /// <param name="agent">The agent whose shield to apply the cheat to.</param>
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

        /// <summary>
        /// Handles shield durability restoration after an agent is hit.
        /// </summary>
        /// <param name="affectedAgent">The agent that was hit.</param>
        /// <param name="affectorAgent">The agent that dealt the damage.</param>
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

