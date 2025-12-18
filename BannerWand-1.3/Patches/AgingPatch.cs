#nullable enable
// System namespaces
using System;
using System.Reflection;

// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to prevent character aging for player and NPCs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// DISABLED: This patch is currently disabled because aging prevention is not working
    /// in the current game version. The functionality has been removed from the codebase.
    /// </para>
    /// <para>
    /// This class is kept for reference only. The [HarmonyPatch] attribute has been removed
    /// to prevent PatchAll() from applying it automatically.
    /// </para>
    /// </remarks>
    // [HarmonyPatch] - REMOVED: Patch is disabled, functionality not working in current game version
    public static class AgingPatch
    {
        // MinimumAgeForStopAging moved to GameConstants for consistency

        /// <summary>
        /// Stores the original birthday for each hero to prevent it from changing.
        /// Key: Hero, Value: Original BirthDay (CampaignTime)
        /// </summary>
        private static System.Collections.Generic.Dictionary<Hero, CampaignTime>? _originalBirthdays;

        /// <summary>
        /// Finds the target method to patch: Hero.BirthDay property setter.
        /// </summary>
        /// <returns>The MethodInfo for the BirthDay property setter, or null if not found.</returns>
        /// <remarks>
        /// <para>
        /// In Bannerlord, character aging is handled by updating Hero.BirthDay property.
        /// This method finds the setter for this property and patches it to prevent updates.
        /// </para>
        /// </remarks>
        public static MethodBase? TargetMethod()
        {
            try
            {
                // Try to find BirthDay property setter in Hero class
                Type? heroType = typeof(Hero);
                if (heroType != null)
                {
                    // Look for BirthDay property
                    PropertyInfo? birthDayProperty = heroType.GetProperty("BirthDay",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (birthDayProperty != null)
                    {
                        // Get the setter method
                        MethodInfo? setter = birthDayProperty.GetSetMethod(true);
                        if (setter != null)
                        {
                            ModLogger.Log($"[AgingPatch] Found BirthDay property setter: {setter.DeclaringType?.Name}.{setter.Name}");
                            _originalBirthdays = [];
                            return setter;
                        }
                    }
                }

                ModLogger.Warning("[AgingPatch] Could not find Hero.BirthDay property setter. Aging prevention may not work.");
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AgingPatch] Error in TargetMethod: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Prefix patch that prevents birthday updates for characters that should not age.
        /// </summary>
        /// <param name="__instance">The Hero instance whose birthday is being updated.</param>
        /// <param name="_">The new BirthDay value being set (unused).</param>
        /// <returns>False to skip the original setter if aging should be prevented, true otherwise.</returns>
        /// <remarks>
        /// <para>
        /// This patch intercepts the BirthDay property setter and prevents the update if:
        /// 1. Stop Player Aging is enabled and the hero is the player
        /// 2. Stop NPC Aging is enabled and the hero is an NPC aged <see cref="GameConstants.MinimumAgeForStopAging"/> or older
        /// </para>
        /// <para>
        /// If the patch returns false, the original setter is skipped, preventing the birthday update.
        /// We also store the original birthday to restore it if needed.
        /// </para>
        /// </remarks>
        [HarmonyPrefix]
        public static bool Prefix(Hero __instance, CampaignTime _)
        {
            try
            {
                // Early exit if hero is null
                if (__instance == null)
                {
                    return true; // Allow original method to run
                }

                // Early exit if settings are null
                CheatSettings? settings = CheatSettings.Instance;
                CheatTargetSettings? targetSettings = CheatTargetSettings.Instance;
                if (settings == null || targetSettings == null)
                {
                    return true; // Allow original method to run
                }

                // DISABLED: Aging prevention cheats removed - not working in current game version
                // This patch is kept for reference but is currently disabled
                // Always allow original setter to run (aging prevention disabled)
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AgingPatch] Error in Prefix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // On error, allow original method to run to avoid breaking the game
                return true;
            }
        }
    }
}

