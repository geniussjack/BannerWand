#nullable enable
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party healing model providing two independent cheats: forcing every simulated battle
    /// casualty to die outright ("All Battles No Wounding"), and instant wound recovery for targeted
    /// parties ("Party Regeneration"). Extends <see cref="DefaultPartyHealingModel"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="TaleWorlds.CampaignSystem.CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all party healing calculations.
    /// </para>
    /// <para>
    /// <b>All Battles No Wounding - why <see cref="CustomCombatSimulationModel"/> is not enough on its
    /// own:</b> during auto-resolved (simulated) map battles, <see cref="TaleWorlds.CampaignSystem.MapEvents.MapEventSide"/>
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
    /// <b>Scope of <see cref="GetSurvivalChance"/>:</b> only ever called from
    /// <see cref="TaleWorlds.CampaignSystem.MapEvents.MapEventSide"/> while resolving a simulated hit, for
    /// every <see cref="TaleWorlds.CampaignSystem.MapEvents.MapEvent"/> type (field battles, sieges, and
    /// raids all share that same code path) - it plays no part in the player's own real-time mission
    /// combat (handled separately by "One-Hit Kills") or in post-battle prisoner-taking for heroes who
    /// were never struck this tick.
    /// </para>
    /// <para>
    /// <b>Party Regeneration:</b> <see cref="GetDailyHealingForRegulars"/> and
    /// <see cref="GetDailyHealingHpForHeroes"/> govern between-battle wound recovery only - how many
    /// wounded regular troops return to healthy each campaign day, and how much HP wounded heroes (the
    /// player included) recover each day. Neither has anything to do with in-combat health, which the
    /// existing Unlimited/Infinite HP cheats already cover. Targeting reuses <see cref="TargetFilter.ShouldApplyCheatToParty"/>,
    /// the same hero/clan/kingdom target settings every other targetable cheat in the mod uses.
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

        /// <summary>
        /// Returns how many wounded regular troops recover to healthy in the party this campaign day.
        /// Boosted to guarantee full recovery of the whole roster while Party Regeneration is enabled
        /// and the party is a valid cheat target.
        /// </summary>
        /// <param name="party">The party whose regulars to calculate healing for.</param>
        /// <param name="isPrisoners">Whether this calculates healing for the party's prisoners instead of its own troops.</param>
        /// <param name="includeDescriptions">Whether to include human-readable modifier descriptions.</param>
        /// <returns>The base game's result with a large bonus added while the cheat applies to this party.</returns>
        /// <remarks>
        /// Prisoner recovery (<paramref name="isPrisoners"/> true) is left untouched - Party
        /// Regeneration is about a party's own wounded members, not the captives it is holding.
        /// </remarks>
        public override ExplainedNumber GetDailyHealingForRegulars(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
        {
            ExplainedNumber result = base.GetDailyHealingForRegulars(party, isPrisoners, includeDescriptions);

            if (!isPrisoners && Settings?.PartyRegeneration == true && party?.IsMobile == true && TargetFilter.ShouldApplyCheatToParty(party.MobileParty))
            {
                result.Add(GameConstants.InstantRegenerationAmount);
            }

            return result;
        }

        /// <summary>
        /// Returns how much HP wounded heroes in the party - including the player - recover this
        /// campaign day. Boosted to guarantee full recovery while Party Regeneration is enabled and
        /// the party is a valid cheat target.
        /// </summary>
        /// <param name="party">The party whose heroes to calculate healing for.</param>
        /// <param name="isPrisoners">Whether this calculates healing for the party's prisoners instead of its own heroes.</param>
        /// <param name="includeDescriptions">Whether to include human-readable modifier descriptions.</param>
        /// <returns>The base game's result with a large bonus added while the cheat applies to this party.</returns>
        /// <remarks>
        /// Prisoner recovery (<paramref name="isPrisoners"/> true) is left untouched, for the same
        /// reason as in <see cref="GetDailyHealingForRegulars"/>.
        /// </remarks>
        public override ExplainedNumber GetDailyHealingHpForHeroes(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
        {
            ExplainedNumber result = base.GetDailyHealingHpForHeroes(party, isPrisoners, includeDescriptions);

            if (!isPrisoners && Settings?.PartyRegeneration == true && party?.IsMobile == true && TargetFilter.ShouldApplyCheatToParty(party.MobileParty))
            {
                result.Add(GameConstants.InstantRegenerationAmount);
            }

            return result;
        }
    }
}
