#nullable enable
using BannerWand.Settings;
using BannerWand.Utils;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch that grants a perk's alternative automatically whenever the player picks
    /// one from a pair, so the player ends up with both instead of having to choose.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Perks are stored as independent per-<see cref="PerkObject"/> booleans on <see cref="Hero"/>
    /// (<c>GetPerkValue</c>/<c>SetPerkValueInternal</c>), not as a single "which alternative was
    /// chosen" field - <see cref="PerkObject.AlternativePerk"/> links the two sides of a pair, and
    /// nothing in the data model itself prevents both being <c>true</c> at once. The "pick one"
    /// restriction is enforced by the selection screen, not the save data.
    /// </para>
    /// <para>
    /// Patches the single internal method every perk grant/selection path funnels through
    /// (<c>Hero.SetPerkValueInternal</c>) instead of the level-up screen specifically, so this
    /// also covers perks granted by quests or other mods, not just manual selection.
    /// </para>
    /// <para>
    /// Previous "both perks" implementations, including the player's own no-longer-available one,
    /// reportedly required reloading the save for the second perk's effects to take hold. This
    /// fires <see cref="CampaignEventDispatcher.OnPerkOpened"/> for the alternative right after
    /// granting it, the same event the game raises for a normal selection, so that whatever the
    /// game normally does to apply a freshly chosen perk's effects (stat recalculation, etc.) also
    /// runs here - the intent is to avoid needing a reload, though this could not be confirmed
    /// against a live game session and needs an in-game check.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(Hero), "SetPerkValueInternal")]
    internal static class BothPerksPatch
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
        /// Cached reflection handle for the internal <c>Hero.SetPerkValueInternal(PerkObject, bool)</c>
        /// method, used to grant the alternative perk the same way the game itself does.
        /// </summary>
        private static readonly MethodInfo? _setPerkValueInternalMethod = typeof(Hero).GetMethod(
            "SetPerkValueInternal",
            BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Guards against this patch's own call to <c>SetPerkValueInternal</c> (for the
        /// alternative perk) re-triggering this same postfix recursively.
        /// </summary>
        private static bool _isApplyingAlternative;

        /// <summary>
        /// Postfix that grants the alternative perk when the player activates one side of a pair.
        /// </summary>
        /// <param name="__instance">The hero the perk was set on.</param>
        /// <param name="perk">The perk that was just set.</param>
        /// <param name="value">The value it was set to.</param>
        [HarmonyPostfix]
        private static void Postfix(Hero __instance, PerkObject perk, bool value)
        {
            try
            {
                if (_isApplyingAlternative || !value || __instance is null || perk is null)
                {
                    return;
                }

                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings is null || targetSettings is null || !settings.AllowBothPerks)
                {
                    return;
                }

                if (__instance != Hero.MainHero || !targetSettings.ApplyToPlayer)
                {
                    return;
                }

                PerkObject? alternative = perk.AlternativePerk;
                if (alternative is null || __instance.GetPerkValue(alternative))
                {
                    return;
                }

                if (_setPerkValueInternalMethod is null)
                {
                    ModLogger.Warning("[BothPerksPatch] Could not find Hero.SetPerkValueInternal method");
                    return;
                }

                _isApplyingAlternative = true;
                try
                {
                    _ = _setPerkValueInternalMethod.Invoke(__instance, [alternative, true]);
                    CampaignEventDispatcher.Instance?.OnPerkOpened(__instance, alternative);
                    ModLogger.Log($"[BothPerksPatch] Granted alternative perk {alternative.Name} alongside {perk.Name}");
                }
                finally
                {
                    _isApplyingAlternative = false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[BothPerksPatch] Error in Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Prefix that blocks the game from clearing a perk back to <c>false</c> when it is the
        /// alternative of a pair this patch already granted, in case the selection screen enforces
        /// mutual exclusivity with a separate call after choosing one side.
        /// </summary>
        /// <param name="__instance">The hero the perk is being set on.</param>
        /// <param name="perk">The perk being set.</param>
        /// <param name="value">The value it is being set to.</param>
        /// <returns><c>false</c> to skip the original call and keep the perk granted, <c>true</c> to proceed normally.</returns>
        [HarmonyPrefix]
        private static bool Prefix(Hero __instance, PerkObject perk, bool value)
        {
            try
            {
                if (_isApplyingAlternative || value || __instance is null || perk is null)
                {
                    return true;
                }

                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings is null || targetSettings is null || !settings.AllowBothPerks)
                {
                    return true;
                }

                if (__instance != Hero.MainHero || !targetSettings.ApplyToPlayer)
                {
                    return true;
                }

                // If this perk's alternative is currently active, this pair was granted by us -
                // do not let it be cleared back to false.
                PerkObject? alternative = perk.AlternativePerk;
                return alternative is null || !__instance.GetPerkValue(alternative);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[BothPerksPatch] Error in Prefix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return true;
            }
        }
    }
}
