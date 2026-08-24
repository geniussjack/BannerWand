#nullable enable
using BannerWand.Settings;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party healing model that can force every simulated battle casualty to die outright,
    /// completing the "All Battles No Wounding" cheat that <see cref="CustomCombatSimulationModel"/>
    /// alone cannot fully deliver. Extends <see cref="DefaultPartyHealingModel"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="TaleWorlds.CampaignSystem.CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all party healing calculations.
    /// </para>
    /// <para>
    /// <b>Why <see cref="CustomCombatSimulationModel"/> is not enough on its own:</b> during
    /// auto-resolved (simulated) map battles, <see cref="TaleWorlds.CampaignSystem.MapEvents.MapEventSide"/>
    /// decides per-hit whether a struck troop is wounded or killed in two steps. First it asks
    /// <see cref="TaleWorlds.CampaignSystem.ComponentInterfaces.CombatSimulationModel.GetBluntDamageChance"/>
    /// for a Blunt/Cut damage type; <see cref="CustomCombatSimulationModel"/> forces this to Cut. But Cut
    /// damage only skips the automatic 100% survival shortcut the base <see cref="GetSurvivalChance"/>
    /// grants to Blunt hits - the struck troop still rolls its normal survival chance (based on level,
    /// armor, age, perks, difficulty settings, etc.) and very often survives as "wounded" regardless.
    /// Forcing the damage type alone can therefore never guarantee a kill. This model closes that gap by
    /// overriding <see cref="GetSurvivalChance"/> itself, so the roll that <see cref="TaleWorlds.CampaignSystem.MapEvents.MapEventSide.ApplySimulationDamageToSelectedTroop"/>
    /// performs for both heroes and regular troops always resolves to death while the cheat is enabled.
    /// </para>
    /// <para>
    /// <b>Scope:</b> <see cref="GetSurvivalChance"/> is only ever called from
    /// <see cref="TaleWorlds.CampaignSystem.MapEvents.MapEventSide"/> while resolving a simulated hit, for
    /// every <see cref="TaleWorlds.CampaignSystem.MapEvents.MapEvent"/> type (field battles, sieges, and
    /// raids all share that same code path) - it plays no part in the player's own real-time mission
    /// combat (handled separately by "One-Hit Kills") or in post-battle prisoner-taking for heroes who
    /// were never struck this tick.
    /// </para>
    /// </remarks>
    public class CustomPartyHealingModel : DefaultPartyHealingModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Returns the chance that a struck troop survives a simulated battle hit as a wound
        /// instead of dying. Forced to zero while the cheat is enabled, so every simulated
        /// casualty dies outright rather than being wounded or later taken prisoner.
        /// </summary>
        /// <param name="party">The party the struck character belongs to.</param>
        /// <param name="character">The character (hero or troop) that was struck.</param>
        /// <param name="damageType">The damage type of the hit (Blunt or Cut).</param>
        /// <param name="canDamageKillEvenIfBlunt">Whether Blunt damage is allowed to kill regardless.</param>
        /// <param name="enemyParty">The party that dealt the hit, if any.</param>
        /// <returns>0f while the cheat is enabled; otherwise the base game's survival chance.</returns>
        public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase? enemyParty = null)
        {
            return Settings?.AllBattlesNoWounding == true
                ? 0f
                : base.GetSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty);
        }
    }
}
