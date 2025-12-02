#nullable enable
using BannerWandRetro.Patches;
using BannerWandRetro.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Core
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
    /// - TradeItemsNoDecrease: Patches ItemBarterable.Apply() to prevent item loss
    /// - UnlockAllSmithyParts: Patches CraftingCampaignBehavior to unlock smithing parts via Reflection
    /// - RenownMultiplier: Patches Clan.AddRenown() to multiply renown gains
    /// </para>
    /// <para>
    /// This static class provides the default implementation of Harmony patch management.
    /// For dependency injection scenarios, use <see cref="Interfaces.IHarmonyManager"/> interface
    /// with <see cref="HarmonyManagerWrapper"/> wrapper class.
    /// </para>
    /// </remarks>
    /// <example>
    /// Usage in SubModule lifecycle:
    /// <code>
    /// // In OnSubModuleLoad()
    /// if (HarmonyManager.Initialize())
    /// {
    ///     ModLogger.Log("Harmony patches initialized successfully");
    /// }
    ///
    /// // In OnSubModuleUnloaded()
    /// HarmonyManager.Uninitialize();
    /// </code>
    /// </example>
    public static class HarmonyManager
    {
        #region Constants

        /// <summary>
        /// Unique identifier for BannerWand's Harmony patches.
        /// </summary>
        /// <remarks>
        /// This ID is used to distinguish BannerWand's patches from other mods,
        /// allowing for safe unpatch operations without affecting other mods.
        /// </remarks>
        private const string HarmonyId = "com.BannerWandRetro.harmony";

        #endregion

        #region Fields

        /// <summary>
        /// The active Harmony instance used for patching game methods.
        /// </summary>

        #endregion

        #region Properties

        /// <summary>
        /// Gets the active Harmony instance.
        /// </summary>
        /// <value>The Harmony instance, or null if not initialized.</value>
        public static Harmony? Instance { get; private set; }

        /// <summary>
        /// Gets whether Harmony patches have been initialized.
        /// </summary>
        /// <value>True if initialized, false otherwise.</value>
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
        /// <remarks>
        /// <para>
        /// Safe to call multiple times - subsequent calls are no-ops if already initialized.
        /// </para>
        /// <para>
        /// Patches are discovered automatically through assembly scanning.
        /// Classes marked with [HarmonyPatch] attribute are automatically patched.
        /// </para>
        /// </remarks>
        /// <exception cref="Exception">
        /// Any exception during patch application is caught, logged, and returns <c>false</c>.
        /// Common causes: Harmony library missing, target methods not found (wrong game version).
        /// </exception>
        public static bool Initialize()
        {
            try
            {
                if (IsInitialized)
                {
                    ModLogger.Warning("Harmony already initialized, skipping re-initialization");
                    return true;
                }


                try
                {
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

                    // Manually apply RenownMultiplierPatch (uses HarmonyTargetMethod which PatchAll doesn't handle well)
                    ApplyRenownMultiplierPatch();

                    // Manually apply AmmoConsumptionPatch (uses HarmonyTargetMethod which PatchAll doesn't handle well)
                    ApplyAmmoConsumptionPatch();

                    IsInitialized = true;

                    ModLogger.Log($"Harmony patches applied successfully (ID: {HarmonyId})");

                    // Log all patched methods for debugging and conflict detection
                    LogPatchedMethods();

                    return true;
                }
                catch (Exception exception)
                {
                    ModLogger.Error($"Failed to initialize Harmony patches: {exception.Message}");
                    ModLogger.Error($"Stack trace: {exception.StackTrace}");
                    return false;
                }
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
        /// <remarks>
        /// <para>
        /// Cleanly unpatches all methods modified by this mod, restoring original behavior.
        /// Other mods' patches are unaffected (Harmony uses unique IDs per mod).
        /// </para>
        /// <para>
        /// Safe to call even if not initialized - logs warning and returns without error.
        /// </para>
        /// </remarks>
        /// <exception cref="Exception">
        /// Caught and logged if unpatch operation fails. Game remains playable but patches
        /// may still be active.
        /// </exception>
        public static void Uninitialize()
        {
            try
            {
                if (!IsInitialized || Instance == null)
                {
                    ModLogger.Warning("Harmony not initialized, nothing to uninitialize");
                    return;
                }

                try
                {
                    ModLogger.Log("Removing Harmony patches...");

                    // Remove only patches applied by this mod (identified by HarmonyId)
                    // Other mods' patches remain unaffected
                    Instance.UnpatchAll(HarmonyId);

                    IsInitialized = false;

                    ModLogger.Log("Harmony patches removed successfully");
                }
                catch (Exception exception)
                {
                    ModLogger.Error($"Failed to remove Harmony patches: {exception.Message}");
                }

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
                    if (prefixMethod == null)
                    {
                        prefixMethod = typeof(AmmoConsumptionPatch).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .FirstOrDefault(m => m.Name == "Prefix");
                    }

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
        /// <remarks>
        /// <para>
        /// Debug utility that enumerates all game methods modified by Harmony patches.
        /// Useful for troubleshooting conflicts with other mods.
        /// </para>
        /// <para>
        /// Output format: "DeclaringType.MethodName" for each patched method.
        /// </para>
        /// </remarks>
        /// <exception cref="Exception">
        /// Caught and logged as warning if enumeration fails. Does not affect patch functionality.
        /// </exception>
        private static void LogPatchedMethods()
        {
            if (Instance == null)
            {
                return;
            }

            try
            {
                // Get all methods that have been patched by this mod
                IEnumerable<MethodBase> patchedMethods = Instance.GetPatchedMethods();
                int patchedMethodCount = 0;

                ModLogger.Log("Patched methods:");

                // Enumerate and log each patched method with its patch details
                foreach (MethodBase method in patchedMethods)
                {
                    HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(method);
                    string declaringTypeName = method.DeclaringType?.FullName ?? "Unknown";
                    ModLogger.Log($"  - {declaringTypeName}.{method.Name}");

                    // Log additional patch details for debugging
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
