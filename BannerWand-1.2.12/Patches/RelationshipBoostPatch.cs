using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace BannerWandRetro.Patches
{
    /// <summary>
    /// Intercepts relationship changes and boosts them to +99 when Max Character Relationship is enabled.
    /// Only applies when the change involves the player hero.
    /// </summary>
    [HarmonyPatch(typeof(CharacterRelationManager))]
    [HarmonyPatch(nameof(CharacterRelationManager.SetHeroRelation))]
    [HarmonyPatch([typeof(Hero), typeof(Hero), typeof(int)])]
    internal static class RelationshipBoostPatch
    {
        /// <summary>
        /// Replaces the relation value with max (100) when cheat is enabled and change is positive.
        /// </summary>
        /// <param name="hero1">First hero in the relationship.</param>
        /// <param name="hero2">Second hero in the relationship.</param>
        /// <param name="value">Target relation value.</param>
        /// <remarks>
        /// <para>
        /// This patches CharacterRelationManager.SetHeroRelation instead of ChangeRelationAction
        /// to avoid issues with static method patching.
        /// </para>
        /// <para>
        /// We detect positive changes by checking if the new value is higher than current value.
        /// If it is, and the cheat is enabled, we boost directly to maximum (100).
        /// </para>
        /// </remarks>
        [HarmonyPrefix]
        private static void Prefix(Hero hero1, Hero hero2, ref int value)
        {
            CheatSettings settings = CheatSettings.Instance;
            CheatTargetSettings targetSettings = CheatTargetSettings.Instance;

            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Only apply if cheat is enabled and player targeting is on
            if (!settings.MaxCharacterRelationship || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            // Only apply if one of the heroes is the player
            if (hero1 != Hero.MainHero && hero2 != Hero.MainHero)
            {
                return;
            }

            // Get current relationship
            int currentRelation = hero1.GetRelation(hero2);

            // Only boost if this is an increase (positive change)
            if (value > currentRelation)
            {
                ModLogger.Debug($"[Relationship] Boosting relation from {currentRelation} to {GameConstants.MaxRelationship} between {hero1.Name} and {hero2.Name}");
                value = GameConstants.MaxRelationship;
            }
        }
    }
}
