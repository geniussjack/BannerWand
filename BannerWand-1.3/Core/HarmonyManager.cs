#nullable enable
using BannerWand.Patches;
using BannerWand.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Core
{
    /// <summary>
    /// Manages Harmony patches for BannerWand mod.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class initializes and manages all Harmony patches used by the mod.
    /// Harmony (lib.harmony v2.3.6) is used to patch game methods that cannot be
    /// overridden through the official Game Model API.
    /// </para>
    /// <para>
    /// Patches are located in the Patches/ folder and are automatically discovered
    /// and applied through PatchAll().
    /// </para>
    /// <para>
    /// Patches implemented:
    /// - BarterableValuePatch: Patches Barterable.GetValueForFaction() for barter cheats
    /// - RenownMultiplierPatch: Patches Clan.AddRenown() to multiply renown gains
    /// - RelationshipBoostPatch: Patches CharacterRelationManager for relationship boosts
    /// - InventoryCapacityPatch: Patches inventory capacity calculations
    /// </para>
    /// <para>
    /// Patches applied via HarmonyTargetMethod (require manual application):
    /// - RenownMultiplierPatch: Patches Clan.AddRenown() to multiply renown gains
    /// - AmmoConsumptionPatch: Prevents ammo decrease for player when Unlimited Ammo enabled
    /// </para>
    /// <para>
    /// REMOVED PATCHES (caused bugs):
    /// - StealthInvisibilityPatch: Caused NPC model visual bugs (broken poses)
    /// </para>
    /// </remarks>
    public static class HarmonyManager
    {
        #region Constants

        /// <summary>
        /// Unique identifier for BannerWand's Harmony patches.
        /// </summary>
        private const string HarmonyId = "com.bannerwand.harmony";

        #endregion

        #region Properties

        /// <summary>
        /// Gets the active Harmony instance.
        /// </summary>
        public static Harmony? Instance { get; private set; }

        /// <summary>
        /// Gets whether Harmony patches have been initialized.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes and applies all Harmony patches.
        /// </summary>
        /// <returns>
        /// <c>true</c> if initialization successful (all patches applied);
        /// <c>false</c> if initialization failed (check logs for details).
        /// </returns>
        public static bool Initialize()
        {
            try
            {
                if (IsInitialized)
                {
                    ModLogger.Warning("Harmony already initialized, skipping re-initialization");
                    return true;
                }

                ModLogger.Log("Initializing Harmony patches...");

                // Create Harmony instance with unique mod identifier
                Instance = new Harmony(HarmonyId);

                // Automatically discover and apply all patches in the executing assembly
                // Patches are marked with [HarmonyPatch] attribute
                Assembly executingAssembly = Assembly.GetExecutingAssembly();

                try
                {
                    Instance.PatchAll(executingAssembly);
                    ModLogger.Log("PatchAll() completed successfully");
                }
                catch (Exception patchAllEx)
                {
                    ModLogger.Warning($"PatchAll() failed: {patchAllEx.Message}");
                    ModLogger.Warning("This is expected if some patches use HarmonyTargetMethod. Continuing with manual patches...");
                }

                // Manually apply patches that use HarmonyTargetMethod (PatchAll doesn't handle these well)
                ApplyRenownMultiplierPatch();
                ApplyAmmoConsumptionPatch();

                IsInitialized = true;

                ModLogger.Log($"Harmony patches applied successfully (ID: {HarmonyId})");

                // Log all patched methods for debugging and conflict detection
                LogPatchedMethods();

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HarmonyManager] Error in Initialize: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Removes all Harmony patches applied by this mod.
        /// </summary>
        public static void Uninitialize()
        {
            try
            {
                if (!IsInitialized || Instance == null)
                {
                    ModLogger.Warning("Harmony not initialized, nothing to uninitialize");
                    return;
                }

                ModLogger.Log("Removing Harmony patches...");

                // Remove only patches applied by this mod (identified by HarmonyId)
                Instance.UnpatchAll(HarmonyId);

                IsInitialized = false;

                ModLogger.Log("Harmony patches removed successfully");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HarmonyManager] Error in Uninitialize: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Manually applies the RenownMultiplierPatch.
        /// </summary>
        /// <remarks>
        /// PatchAll() doesn't handle patches with [HarmonyTargetMethod] well,
        /// so we apply this patch manually to ensure it works.
        /// </remarks>
        private static void ApplyRenownMultiplierPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[RenownMultiplier] Harmony Instance is null - cannot apply patch!");
                    return;
                }

                // Get the target method from the patch class
                MethodBase? targetMethod = RenownMultiplierPatch.TargetMethod();
                if (targetMethod == null)
                {
                    ModLogger.Error("[RenownMultiplier] TargetMethod() returned null - patch cannot be applied!");
                    return;
                }

                // Get the prefix method
                MethodInfo? prefixMethod = typeof(RenownMultiplierPatch).GetMethod("Prefix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (prefixMethod == null)
                {
                    ModLogger.Error("[RenownMultiplier] Prefix method not found!");
                    return;
                }

                // Apply the patch
                HarmonyMethod harmonyPrefix = new(prefixMethod);
                _ = Instance.Patch(targetMethod, prefix: harmonyPrefix);

                ModLogger.Log("[RenownMultiplier] Patch applied successfully via manual patching");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[RenownMultiplier] Error applying patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually applies the AmmoConsumptionPatch and MissionEquipmentAmmoSafetyPatch.
        /// </summary>
        /// <remarks>
        /// These patches prevent ammo decrease for player when Unlimited Ammo is enabled.
        /// Uses HarmonyTargetMethod so requires manual application.
        /// </remarks>
        private static void ApplyAmmoConsumptionPatch()
        {
            bool primaryPatchApplied = false;
            bool fallbackPatchApplied = false;

            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[AmmoConsumptionPatch] Harmony Instance is null - cannot apply patches!");
                    return;
                }

                // Try to apply primary patch (Agent.SetWeaponAmountInSlot)
                MethodBase? targetMethod = AmmoConsumptionPatch.TargetMethod();
                if (targetMethod != null)
                {
                    // Find the Prefix method with 4 parameters (EquipmentIndex, short, bool)
                    MethodInfo? prefixMethod = typeof(AmmoConsumptionPatch).GetMethod("Prefix",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        [typeof(Agent), typeof(EquipmentIndex), typeof(short), typeof(bool)],
                        null);

                    // If not found, try with 3 parameters
                    if (prefixMethod is null)
                    {
                        prefixMethod = typeof(AmmoConsumptionPatch).GetMethod("Prefix",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            [typeof(Agent), typeof(EquipmentIndex), typeof(short)],
                            null);
                    }

                    // Last resort - get any Prefix method
                    prefixMethod ??= typeof(AmmoConsumptionPatch).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(m => m.Name == "Prefix");

                    if (prefixMethod != null)
                    {
                        HarmonyMethod harmonyPrefix = new(prefixMethod);
                        _ = Instance.Patch(targetMethod, prefix: harmonyPrefix);
                        primaryPatchApplied = true;
                        AmmoConsumptionPatch.IsPatchApplied = true;
                        ModLogger.Log("[AmmoConsumptionPatch] Primary patch applied successfully - ammo decrease will be blocked");
                    }
                    else
                    {
                        ModLogger.Warning("[AmmoConsumptionPatch] Prefix method not found!");
                    }
                }
                else
                {
                    ModLogger.Warning("[AmmoConsumptionPatch] Target method not found - primary patch skipped");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AmmoConsumptionPatch] Error applying primary patch: {ex.Message}");
            }

            // Try to apply fallback patch (MissionEquipment indexer)
            try
            {
                MethodBase? fallbackTarget = MissionEquipmentAmmoSafetyPatch.TargetMethod();
                if (fallbackTarget != null)
                {
                    MethodInfo? fallbackPrefix = typeof(MissionEquipmentAmmoSafetyPatch).GetMethod("Prefix",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (fallbackPrefix != null)
                    {
                        HarmonyMethod harmonyPrefix = new(fallbackPrefix);
                        _ = Instance!.Patch(fallbackTarget, prefix: harmonyPrefix);
                        fallbackPatchApplied = true;
                        ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Fallback patch applied successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[MissionEquipmentAmmoSafetyPatch] Error applying fallback patch: {ex.Message}");
            }

            // Summary
            if (!primaryPatchApplied && !fallbackPatchApplied)
            {
                ModLogger.Warning("[AmmoConsumptionPatch] No ammo patches could be applied!");
                ModLogger.Warning("[AmmoConsumptionPatch] Unlimited Ammo will rely on tick-based fallback restoration only.");
            }
            else
            {
                ModLogger.Log($"[AmmoConsumptionPatch] Ammo patches status: Primary={primaryPatchApplied}, Fallback={fallbackPatchApplied}");
            }
        }

        /// <summary>
        /// Logs all methods that have been patched by this mod.
        /// </summary>
        private static void LogPatchedMethods()
        {
            if (Instance == null)
            {
                return;
            }

            try
            {
                IEnumerable<MethodBase> patchedMethods = Instance.GetPatchedMethods();
                int patchedMethodCount = 0;

                ModLogger.Log("Patched methods:");

                foreach (MethodBase method in patchedMethods)
                {
                    HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(method);
                    string declaringTypeName = method.DeclaringType?.FullName ?? "Unknown";
                    ModLogger.Log($"  - {declaringTypeName}.{method.Name}");

                    if (patchInfo != null)
                    {
                        int prefixCount = patchInfo.Prefixes.Count;
                        int postfixCount = patchInfo.Postfixes.Count;

                        if (prefixCount > 0)
                        {
                            ModLogger.Debug($"    Prefixes: {prefixCount}");
                        }

                        if (postfixCount > 0)
                        {
                            ModLogger.Debug($"    Postfixes: {postfixCount}");
                        }
                    }

                    patchedMethodCount++;
                }

                ModLogger.Log($"Total patched methods: {patchedMethodCount}");
            }
            catch (Exception exception)
            {
                ModLogger.Warning($"Failed to log patched methods: {exception.Message}");
            }
        }

        #endregion
    }
}
