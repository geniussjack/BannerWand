#nullable enable
using BannerWand.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom combat simulation model that can force auto-resolved (simulated) map battles to
    /// never deal blunt/non-lethal damage, so troops die instead of being wounded or captured.
    /// Extends <see cref="DefaultCombatSimulationModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all combat simulation calculations.
    /// </para>
    /// <para>
    /// <b>Scope:</b> This only affects <see cref="TaleWorlds.CampaignSystem.BattleSimulation"/>, the
    /// system the campaign uses to auto-resolve field battles between AI-controlled parties (and the
    /// AI-controlled portion of battles the player does not personally join). It covers every field
    /// battle on the map, not just the player's own. It does not cover the player's own real-time
    /// mission combat (handled separately by the existing "One-Hit Kills" cheat) or siege casualties
    /// (governed by a separate <see cref="TaleWorlds.CampaignSystem.ComponentInterfaces.SiegeEventModel"/>).
    /// </para>
    /// </remarks>
    public class CustomCombatSimulationModel : DefaultCombatSimulationModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Returns the chance that a simulated hit deals blunt (non-lethal) damage instead of a
        /// killing blow. Forced to zero while the cheat is enabled, so every simulated casualty
        /// on the losing side of a field battle is a kill rather than a wound/capture.
        /// </summary>
        /// <param name="strikerTroop">The troop delivering the simulated hit.</param>
        /// <param name="strikedTroop">The troop receiving the simulated hit.</param>
        /// <param name="strikerParty">The party the striking troop belongs to.</param>
        /// <param name="strikedParty">The party the struck troop belongs to.</param>
        /// <param name="battle">The map event being simulated.</param>
        /// <returns>0f while the cheat is enabled; otherwise the base game's blunt damage chance.</returns>
        public override float GetBluntDamageChance(CharacterObject strikerTroop, CharacterObject strikedTroop, PartyBase strikerParty, PartyBase strikedParty, MapEvent battle)
        {
            return Settings?.AllBattlesNoWounding == true
                ? 0f
                : base.GetBluntDamageChance(strikerTroop, strikedTroop, strikerParty, strikedParty, battle);
        }
    }
}
