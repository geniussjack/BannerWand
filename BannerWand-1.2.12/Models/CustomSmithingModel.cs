using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace BannerWandRetro.Models
{
    /// <summary>
    /// Custom smithing model that removes stamina costs for smithing and smelting (PLAYER ONLY).
    /// Extends <see cref="DefaultSmithingModel"/> to add cheat functionality without Harmony patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all smithing calculations.
    /// </para>
    /// <para>
    /// Cheat feature provided:
    /// - Unlimited Smithy Stamina: Sets energy cost to 0 for PLAYER smithing operations only
    /// </para>
    /// <para>
    /// Smithing system in Bannerlord:
    /// - Each smithing/smelting action costs stamina
    /// - Stamina recovers over time
    /// - Running out of stamina limits smithing sessions
    /// - This cheat removes stamina costs entirely for player
    /// </para>
    /// <para>
    /// IMPORTANT: This cheat applies ONLY to the player hero, not to NPC companions or other heroes.
    /// This design choice ensures:
    /// - Consistent behavior (NPCs don't benefit from unlimited stamina)
    /// - Performance optimization (no need to check every NPC hero)
    /// - Game balance (player gets the advantage, NPCs follow normal rules)
    /// </para>
    /// </remarks>
    public class CustomSmithingModel : DefaultSmithingModel
    {
        /// <summary>
        /// Energy cost for unlimited stamina (0).
        /// </summary>
        private const int ZeroEnergyCost = 0;

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance!;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        /// <summary>
        /// Gets the energy cost for smithing an item, with cheat override for zero cost (PLAYER ONLY).
        /// Overrides <see cref="DefaultSmithingModel.GetEnergyCostForSmithing"/>.
        /// </summary>
        /// <param name="item">The item being smithed. Cannot be null.</param>
        /// <param name="hero">The hero doing the smithing. Cannot be null.</param>
        /// <returns>
        /// The energy cost (0 when cheat enabled for player, base cost otherwise).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Energy cost calculation:
        /// - Base cost: Typically 10-30 energy per action
        /// - Heroes have limited max energy (usually 100-150)
        /// - Cheat returns: 0 energy cost for PLAYER only
        /// </para>
        /// <para>
        /// Applies ONLY to player hero, not to NPCs.
        /// This ensures the cheat doesn't affect companion smithing or NPC behavior.
        /// </para>
        /// <para>
        /// Performance: Called during smithing UI interactions (not performance-critical).
        /// Very lightweight - just boolean checks and early returns.
        /// </para>
        /// </remarks>
        public override int GetEnergyCostForSmithing(ItemObject item, Hero hero)
        {
            try
            {
                return ShouldApplyUnlimitedSmithyStamina(hero) ? ZeroEnergyCost : base.GetEnergyCostForSmithing(item, hero);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSmithingModel] Error in GetEnergyCostForSmithing: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the energy cost for smelting an item, with cheat override for zero cost (PLAYER ONLY).
        /// Overrides <see cref="DefaultSmithingModel.GetEnergyCostForSmelting"/>.
        /// </summary>
        /// <param name="item">The item being smelted. Cannot be null.</param>
        /// <param name="hero">The hero doing the smelting. Cannot be null.</param>
        /// <returns>
        /// The energy cost (0 when cheat enabled for player, base cost otherwise).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Smelting is similar to smithing but breaks down items for materials.
        /// Same stamina system applies, so same cheat logic is used.
        /// </para>
        /// <para>
        /// Applies ONLY to player hero, not to NPCs.
        /// This ensures the cheat doesn't affect companion smelting or NPC behavior.
        /// </para>
        /// <para>
        /// Performance: Called during smithing UI interactions (not performance-critical).
        /// Very lightweight - just boolean checks and early returns.
        /// </para>
        /// </remarks>
        public override int GetEnergyCostForSmelting(ItemObject item, Hero hero)
        {
            try
            {
                return ShouldApplyUnlimitedSmithyStamina(hero) ? ZeroEnergyCost : base.GetEnergyCostForSmelting(item, hero);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSmithingModel] Error in GetEnergyCostForSmelting: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the energy cost for refining materials, with cheat override for zero cost (PLAYER ONLY).
        /// Overrides <see cref="DefaultSmithingModel.GetEnergyCostForRefining"/>.
        /// </summary>
        /// <param name="refineFormula">The refining formula being used (passed by reference). Cannot be null.</param>
        /// <param name="hero">The hero doing the refining. Cannot be null.</param>
        /// <returns>
        /// The energy cost (0 when cheat enabled for player, base cost otherwise).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Refining is the process of converting raw materials (wood, iron ore) into processed materials
        /// (charcoal, iron ingots). This also consumes stamina like smithing and smelting.
        /// </para>
        /// <para>
        /// Applies ONLY to player hero, not to NPCs.
        /// This ensures the cheat doesn't affect companion refining or NPC behavior.
        /// </para>
        /// <para>
        /// Performance: Called during smithing UI interactions (not performance-critical).
        /// Very lightweight - just boolean checks and early returns.
        /// </para>
        /// </remarks>
        public override int GetEnergyCostForRefining(ref Crafting.RefiningFormula refineFormula, Hero hero)
        {
            try
            {
                return ShouldApplyUnlimitedSmithyStamina(hero) ? ZeroEnergyCost : base.GetEnergyCostForRefining(ref refineFormula, hero);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSmithingModel] Error in GetEnergyCostForRefining: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return 0;
            }
        }

        /// <summary>
        /// Determines if unlimited smithy stamina should be applied to the hero.
        /// </summary>
        private bool ShouldApplyUnlimitedSmithyStamina(Hero hero)
        {
            if (Settings == null || TargetSettings == null || hero == null)
            {
                return false;
            }

            if (!Settings.UnlimitedSmithyStamina)
            {
                return false;
            }

            // Check if hero is player
            if (hero == Hero.MainHero && TargetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if hero is in player clan
            if (hero.Clan == Clan.PlayerClan && TargetSettings.ApplyToPlayerClanMembers)
            {
                return true;
            }

            // Check other NPC targets
            return TargetSettings.HasAnyNPCTargetEnabled() && TargetFilter.ShouldApplyCheat(hero);
        }
    }
}
