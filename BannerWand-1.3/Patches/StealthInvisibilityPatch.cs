using BannerWand.Settings;
using BannerWand.Utils;
using HarmonyLib;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch that makes the player completely invisible and undetectable in stealth missions.
    /// Intercepts detection methods to prevent NPCs from detecting the player.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch uses Harmony to intercept methods responsible for agent detection in stealth missions.
    /// When the cheat is enabled, it prevents NPCs from detecting the player agent.
    /// </para>
    /// <para>
    /// The patch targets multiple detection-related methods to ensure complete invisibility:
    /// - Agent.IsDetected property: Returns false for player agent when cheat is enabled
    /// - Agent detection checks in stealth mission logic
    /// </para>
    /// <para>
    /// This is a player-only cheat and does not affect NPCs.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(Agent))]
    [HarmonyPatch("get_IsDetected")]
    internal static class StealthInvisibilityPatch
    {
        /// <summary>
        /// Prefix patch that prevents player agent from being detected.
        /// </summary>
        /// <param name="__instance">The agent being checked for detection.</param>
        /// <param name="__result">The result of the detection check (will be set to false for player).</param>
        /// <returns>True to skip original method, false to continue with original.</returns>
        [HarmonyPrefix]
        static bool Prefix(Agent __instance, ref bool __result)
        {
            try
            {
                CheatSettings settings = CheatSettings.Instance!;
                CheatTargetSettings targetSettings = CheatTargetSettings.Instance!;

                if (settings is null || targetSettings is null)
                {
                    return true; // Continue with original method
                }

                // Only apply if cheat is enabled and player targeting is on
                if (!settings.StealthInvisibility || !targetSettings.ApplyToPlayer)
                {
                    return true; // Continue with original method
                }

                // Only affect player agent
                if (__instance?.IsPlayerControlled == true)
                {
                    __result = false; // Player is never detected
                    return false; // Skip original method
                }

                return true; // Continue with original method for non-player agents
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"[StealthInvisibilityPatch] Error in Prefix: {ex.Message}");
                return true; // Continue with original method on error
            }
        }
    }

    /// <summary>
    /// Alternative patch for stealth detection methods that may use different naming.
    /// </summary>
    /// <remarks>
    /// This patch targets methods that check if an agent can be detected by other agents.
    /// It's a fallback in case the primary patch doesn't cover all detection scenarios.
    /// </remarks>
    [HarmonyPatch(typeof(Agent))]
    [HarmonyPatch("CanBeDetected", MethodType.Getter)]
    internal static class StealthInvisibilityCanBeDetectedPatch
    {
        /// <summary>
        /// Prefix patch that prevents player agent from being detectable.
        /// </summary>
        [HarmonyPrefix]
        static bool Prefix(Agent __instance, ref bool __result)
        {
            try
            {
                CheatSettings settings = CheatSettings.Instance!;
                CheatTargetSettings targetSettings = CheatTargetSettings.Instance!;

                if (settings is null || targetSettings is null)
                {
                    return true;
                }

                if (!settings.StealthInvisibility || !targetSettings.ApplyToPlayer)
                {
                    return true;
                }

                if (__instance?.IsPlayerControlled == true)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                ModLogger.Error($"[StealthInvisibilityCanBeDetectedPatch] Error in Prefix: {ex.Message}");
                return true;
            }
        }
    }
}

