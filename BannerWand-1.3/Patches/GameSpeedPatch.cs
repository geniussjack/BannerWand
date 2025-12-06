#nullable enable
using BannerWand.Settings;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to multiply campaign time speed for Play and Fast Forward buttons.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch intercepts Campaign.TickMapTime() to apply a speed multiplier
    /// to both Play (1x) and Fast Forward (4x) button speeds.
    /// </para>
    /// <para>
    /// The multiplier from CheatSettings.GameSpeed is applied to the calculated
    /// time delta before it's used to advance campaign time.
    /// </para>
    /// <para>
    /// Example: If multiplier is 2.0, Play becomes 2x speed, Fast Forward becomes 8x speed.
    /// </para>
    /// <para>
    /// IMPORTANT: We also patch MapTimeTracker.Tick() because it's called with a local
    /// variable 'num' instead of '_dt', so the day/night cycle wasn't being accelerated.
    /// </para>
    /// </remarks>
    [HarmonyPatch]
    public static class GameSpeedPatch
    {
        /// <summary>
        /// Finds the target method to patch: Campaign.TickMapTime(float realDt).
        /// </summary>
        /// <returns>The MethodInfo for Campaign.TickMapTime, or null if not found.</returns>
        public static MethodBase? TargetMethod()
        {
            try
            {
                Type? campaignType = typeof(Campaign);
                if (campaignType == null)
                {
                    return null;
                }

                // Find TickMapTime method with signature: void TickMapTime(float realDt)
                MethodInfo? tickMapTimeMethod = campaignType.GetMethod("TickMapTime",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    [typeof(float)],
                    null);

                return tickMapTimeMethod;
            }
            catch (Exception ex)
            {
                Utils.ModLogger.Warning($"[GameSpeedPatch] Error in TargetMethod: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Postfix patch that applies the speed multiplier to the calculated time delta.
        /// </summary>
        /// <param name="__instance">The Campaign instance.</param>
        /// <remarks>
        /// <para>
        /// This patch runs after TickMapTime calculates the time delta (_dt).
        /// We need to access the private _dt field and multiply it by the speed multiplier.
        /// </para>
        /// <para>
        /// Since we can't directly access private fields, we use reflection to get
        /// and set the _dt field value.
        /// </para>
        /// </remarks>
        public static void Postfix(Campaign __instance)
        {
            try
            {
                // Early return if cheat not enabled
                if (CheatSettings.Instance == null || CheatTargetSettings.Instance == null)
                {
                    return;
                }

                if (CheatSettings.Instance.GameSpeed <= 0f || !CheatTargetSettings.Instance.ApplyToPlayer)
                {
                    return;
                }

                // Get the private _dt field using reflection
                FieldInfo? dtField = typeof(Campaign).GetField("_dt",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (dtField == null)
                {
                    return;
                }

                // Get current time delta
                float currentDt = (float)(dtField.GetValue(__instance) ?? 0f);

                // Apply multiplier only if time is advancing (not paused)
                if (currentDt > 0f)
                {
                    float multiplier = CheatSettings.Instance.GameSpeed;
                    float newDt = currentDt * multiplier;

                    // Set the modified time delta
                    dtField.SetValue(__instance, newDt);
                }
            }
            catch (Exception ex)
            {
                Utils.ModLogger.Error($"[GameSpeedPatch] Error in Postfix: {ex.Message}");
                Utils.ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Harmony patch to multiply MapTimeTracker.Tick() parameter to accelerate day/night cycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch is necessary because Campaign.TickMapTime() calls MapTimeTracker.Tick(4320f * num)
    /// with a local variable 'num' instead of '_dt'. Since our main patch modifies '_dt' but
    /// MapTimeTracker.Tick() is called with 'num', the day/night cycle wasn't being accelerated.
    /// </para>
    /// <para>
    /// This patch intercepts MapTimeTracker.Tick(float seconds) and multiplies the 'seconds'
    /// parameter by our GameSpeed multiplier.
    /// </para>
    /// </remarks>
    [HarmonyPatch]
    public static class MapTimeTrackerTickPatch
    {
        /// <summary>
        /// Finds the target method to patch: MapTimeTracker.Tick(float seconds).
        /// </summary>
        /// <returns>The MethodInfo for MapTimeTracker.Tick, or null if not found.</returns>
        public static MethodBase? TargetMethod()
        {
            try
            {
                // MapTimeTracker is internal, so we need to find it by name
                Type? mapTimeTrackerType = typeof(Campaign).Assembly.GetType("TaleWorlds.CampaignSystem.MapTimeTracker");
                if (mapTimeTrackerType == null)
                {
                    return null;
                }

                // Find Tick method with signature: void Tick(float seconds)
                MethodInfo? tickMethod = mapTimeTrackerType.GetMethod("Tick",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                    null,
                    [typeof(float)],
                    null);

                return tickMethod;
            }
            catch (Exception ex)
            {
                Utils.ModLogger.Warning($"[MapTimeTrackerTickPatch] Error in TargetMethod: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Prefix patch that multiplies the seconds parameter by the speed multiplier.
        /// </summary>
        /// <param name="seconds">The seconds parameter (ref so we can modify it).</param>
        /// <remarks>
        /// In Harmony, using 'ref' on a parameter in a Prefix allows us to modify
        /// the value before it's passed to the original method.
        /// </remarks>
        public static void Prefix(ref float seconds)
        {
            try
            {
                // Early return if cheat not enabled
                if (CheatSettings.Instance == null || CheatTargetSettings.Instance == null)
                {
                    return;
                }

                if (CheatSettings.Instance.GameSpeed <= 0f || !CheatTargetSettings.Instance.ApplyToPlayer)
                {
                    return;
                }

                // Apply multiplier only if time is advancing (not paused)
                if (seconds > 0f)
                {
                    float multiplier = CheatSettings.Instance.GameSpeed;
                    seconds *= multiplier;
                }
            }
            catch (Exception ex)
            {
                Utils.ModLogger.Error($"[MapTimeTrackerTickPatch] Error in Prefix: {ex.Message}");
                Utils.ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}

