#nullable enable
using BannerWandRetro.Patches;
using BannerWandRetro.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
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
    /// - ItemBarterablePatch: Patches ItemBarterable.Apply() to prevent item loss during barter transactions
    /// - ItemRosterTradePatch: Patches ItemRoster.AddToCounts() to prevent item removal during all trade types (towns, villages, caravans, etc.)
    /// - RenownMultiplierPatch: Patches Clan.AddRenown() to multiply renown gains
    /// - RelationshipBoostPatch: Patches CharacterRelationManager for relationship boosts
    /// - InventoryCapacityPatch: Patches inventory capacity calculations
    /// </para>
    /// <para>
    /// Patches applied via manual patching (require manual application):
    /// - RenownMultiplierPatch: Patches Clan.AddRenown() to multiply renown gains
    /// - AmmoConsumptionPatch: Prevents ammo decrease for player when Unlimited Ammo enabled
    /// - ItemBarterablePatch: Prevents item loss during barter/trade transactions
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

        // Fields are defined as properties below (Instance, IsInitialized)

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

                    // Manually apply patches and track success
                    // Count patches dynamically to avoid hardcoding the total
                    int patchesApplied = 0;
                    int patchesTotal = 0;

                    // Apply each patch and count both attempts and successes
                    patchesTotal++;
                    if (ApplyRenownMultiplierPatch())
                    {
                        patchesApplied++;
                    }

                    // CRITICAL: AmmoConsumptionPatch is NOT applied here during initialization
                    // It is applied dynamically in OnMissionBehaviorInitialize to prevent breaking
                    // character models in menus. See ApplyAmmoConsumptionPatchForMission().
                    // patchesTotal++;
                    // if (ApplyAmmoConsumptionPatch())
                    // {
                    //     patchesApplied++;
                    // }

                    patchesTotal++;
                    if (ApplyInventoryCapacityPatch())
                    {
                        patchesApplied++;
                    }

                    patchesTotal++;
                    if (ApplyItemBarterablePatch())
                    {
                        patchesApplied++;
                    }

                    patchesTotal++;
                    if (ApplyItemRosterTradePatch())
                    {
                        patchesApplied++;
                    }

                    patchesTotal++;
                    if (ApplyGameSpeedPatch())
                    {
                        patchesApplied++;
                    }

                    patchesTotal++;
                    if (ApplyMapTimeTrackerTickPatch())
                    {
                        patchesApplied++;
                    }

                    // Only mark as initialized if at least some patches were applied successfully
                    // This prevents silent failures when patches can't be applied (e.g., wrong game version)
                    if (patchesApplied > 0)
                    {
                        IsInitialized = true;
                        ModLogger.Log($"Harmony patches applied successfully: {patchesApplied}/{patchesTotal} patches active (ID: {HarmonyId})");

                        if (patchesApplied < patchesTotal)
                        {
                            ModLogger.Warning($"Some patches failed to apply ({patchesTotal - patchesApplied} failed). This may be normal for some game versions.");
                        }

                        // Log all patched methods for debugging and conflict detection
                        LogPatchedMethods();

                        return true;
                    }
                    else
                    {
                        ModLogger.Error("All Harmony patches failed to apply! This may indicate a version mismatch or missing dependencies.");
                        ModLogger.Error("The mod may not function correctly. Please check the logs for details.");
                        return false;
                    }
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
        /// Checks if a patch method is already applied to a target method.
        /// </summary>
        /// <param name="targetMethod">The method that may be patched.</param>
        /// <param name="patchMethod">The patch method to check for.</param>
        /// <returns>True if the patch is already applied, false otherwise.</returns>
        private static bool IsPatchAlreadyApplied(MethodBase targetMethod, MethodInfo patchMethod)
        {
            try
            {
                HarmonyLib.Patches? existingPatches = Harmony.GetPatchInfo(targetMethod);
                if (existingPatches == null)
                {
                    return false;
                }

                // Check if our patch method is already in the prefixes or postfixes
                // CRITICAL FIX: Compare by declaring type and method name, not exact MethodInfo
                // This handles cases where PatchAll() applied a different overload than what we're checking
                string patchClassName = patchMethod.DeclaringType?.FullName ?? "";
                string patchMethodName = patchMethod.Name;

                return existingPatches.Prefixes.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName)) ||
                       existingPatches.Postfixes.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName)) ||
                       existingPatches.Transpilers.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName)) ||
                       existingPatches.Finalizers.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName));
            }
            catch (Exception ex)
            {
                // If we can't check, assume not applied to be safe
                ModLogger.Warning($"[HarmonyManager] Failed to check if patch is already applied: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the RenownMultiplierPatch.
        /// </summary>
        /// <returns>True if patch was applied successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// PatchAll() doesn't handle patches with [HarmonyTargetMethod] well,
        /// so we apply this patch manually to ensure it works.
        /// </remarks>
        private static bool ApplyRenownMultiplierPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[RenownMultiplier] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Get the target method from the patch class
                MethodBase? targetMethod = RenownMultiplierPatch.TargetMethod();
                if (targetMethod == null)
                {
                    ModLogger.Error("[RenownMultiplier] TargetMethod() returned null - patch cannot be applied!");
                    return false;
                }

                // Get the prefix method
                MethodInfo? prefixMethod = typeof(RenownMultiplierPatch).GetMethod("Prefix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (prefixMethod == null)
                {
                    ModLogger.Error("[RenownMultiplier] Prefix method not found!");
                    return false;
                }

                // Check if patch is already applied (e.g., by PatchAll())
                if (IsPatchAlreadyApplied(targetMethod, prefixMethod))
                {
                    ModLogger.Log("[RenownMultiplier] Patch already applied (likely by PatchAll()), skipping manual application");
                    return true;
                }

                // Apply the patch
                HarmonyMethod harmonyPrefix = new(prefixMethod);
                _ = Instance.Patch(targetMethod, prefix: harmonyPrefix);

                ModLogger.Log("[RenownMultiplier] Patch applied successfully via manual patching");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[RenownMultiplier] Error applying patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the AmmoConsumptionPatch and MissionEquipmentAmmoSafetyPatch.
        /// </summary>
        /// <returns>True if at least one patch was applied successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// These patches prevent ammo decrease for player when Unlimited Ammo is enabled.
        /// Uses HarmonyTargetMethod so requires manual application.
        /// </remarks>
        private static bool ApplyAmmoConsumptionPatch()
        {
            bool primaryPatchApplied = false;
            bool nativeConsumePatchApplied = false;
            bool fallbackPatchApplied = false;

            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[AmmoConsumptionPatch] Harmony Instance is null - cannot apply patches!");
                    return false;
                }

                // Try to apply primary patch (Agent.SetWeaponAmountInSlot)
                MethodBase? targetMethod = AmmoConsumptionPatch.TargetMethod();
                if (targetMethod != null)
                {
                    // Get target method parameters to determine which Prefix to use
                    ParameterInfo[] targetParams = targetMethod.GetParameters();
                    bool targetHasRef = targetParams.Length >= 2 && targetParams[1].ParameterType.IsByRef;
                    
                    string targetSig = string.Join(", ", targetParams.Select(p => 
                        p.ParameterType.IsByRef ? $"ref {p.ParameterType.GetElementType()?.Name ?? p.ParameterType.Name}" : p.ParameterType.Name));
                    ModLogger.Log($"[AmmoConsumptionPatch] Target method signature: {targetMethod.Name}({targetSig}), hasRef={targetHasRef}");
                    
                    MethodInfo? prefixMethod = null;

                    if (targetHasRef)
                    {
                        // Target method has ref parameter - use Prefix with ref
                        // Find the Prefix method with 4 parameters (EquipmentIndex, ref short, bool)
                        prefixMethod = typeof(AmmoConsumptionPatch).GetMethod("Prefix",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            [typeof(Agent), typeof(EquipmentIndex), typeof(short).MakeByRefType(), typeof(bool)],
                            null);

                        // If not found, try with 3 parameters (ref short)
                        prefixMethod ??= typeof(AmmoConsumptionPatch).GetMethod("Prefix",
                                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                null,
                                [typeof(Agent), typeof(EquipmentIndex), typeof(short).MakeByRefType()],
                                null);
                    }
                    else
                    {
                        // Target method does NOT have ref parameter - use Prefix_NoRef
                        // Find the Prefix_NoRef method with 4 parameters (EquipmentIndex, short, bool) - NO ref
                        prefixMethod = typeof(AmmoConsumptionPatch).GetMethod("Prefix_NoRef",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            [typeof(Agent), typeof(EquipmentIndex), typeof(short), typeof(bool)],
                            null);
                    }

                    // Last resort - get Prefix or Prefix_NoRef method with most parameters
                    prefixMethod ??= typeof(AmmoConsumptionPatch).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(m => m.Name == "Prefix" || m.Name == "Prefix_NoRef")
                        .OrderByDescending(m => m.GetParameters().Length)
                        .FirstOrDefault();

                    if (prefixMethod != null)
                    {
                        ParameterInfo[] prefixParams = prefixMethod.GetParameters();
                        string prefixSig = string.Join(", ", prefixParams.Select(p => 
                            p.ParameterType.IsByRef ? $"ref {p.ParameterType.GetElementType()?.Name ?? p.ParameterType.Name}" : p.ParameterType.Name));
                        ModLogger.Log($"[AmmoConsumptionPatch] Found Prefix method: {prefixMethod.Name} with {prefixParams.Length} parameters ({prefixSig})");

                        // Check if patch is already applied (e.g., by PatchAll())
                        if (IsPatchAlreadyApplied(targetMethod, prefixMethod))
                        {
                            primaryPatchApplied = true;
                            AmmoConsumptionPatch.IsPatchApplied = true;
                            ModLogger.Log("[AmmoConsumptionPatch] Patch already applied by PatchAll(), skipping manual application");
                        }
                        else
                        {
                            // CRITICAL: Create HarmonyMethod with explicit argument types to ensure proper parameter matching
                            // Harmony may fail to match parameters if types are not explicitly specified
                            HarmonyMethod harmonyPrefix = new(prefixMethod)
                            {
                                argumentTypes = targetMethod.GetParameters().Select(p => p.ParameterType).ToArray()
                            };
                            
                            ModLogger.Log($"[AmmoConsumptionPatch] Applying patch: Target={targetMethod.DeclaringType?.FullName}.{targetMethod.Name}, Prefix={prefixMethod.DeclaringType?.FullName}.{prefixMethod.Name}");
                            ModLogger.Log($"[AmmoConsumptionPatch] Target parameters: {string.Join(", ", targetMethod.GetParameters().Select(p => p.ParameterType.Name))}");
                            ModLogger.Log($"[AmmoConsumptionPatch] Prefix parameters: {string.Join(", ", prefixMethod.GetParameters().Select(p => p.ParameterType.Name))}");
                            
                            MethodInfo patchResult = Instance.Patch(targetMethod, prefix: harmonyPrefix);
                            primaryPatchApplied = true;
                            AmmoConsumptionPatch.IsPatchApplied = true;
                            ModLogger.Log($"[AmmoConsumptionPatch] Primary patch applied successfully via manual patching. Patch result: {patchResult}");
                            
                            // Verify patch was actually applied
                            HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(targetMethod);
                            if (patchInfo != null)
                            {
                                ModLogger.Debug($"[AmmoConsumptionPatch] Verify: Target method has {patchInfo.Prefixes.Count} prefix(es) applied");
                                foreach (var prefix in patchInfo.Prefixes)
                                {
                                    ModLogger.Debug($"[AmmoConsumptionPatch]   - Prefix: {prefix.owner}.{prefix.PatchMethod.Name}");
                                }
                            }
                        }
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

            // Apply native ammo consume patch (Agent.OnWeaponAmmoConsume) – this is invoked by the engine when ammo decreases
            try
            {
                MethodInfo? nativeConsumePrefix = typeof(AmmoConsumptionPatch).GetMethod(
                    "OnWeaponAmmoConsume_Prefix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                MethodInfo? nativeConsumeTarget = typeof(Agent).GetMethod(
                    "OnWeaponAmmoConsume",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (nativeConsumePrefix != null && nativeConsumeTarget != null)
                {
                    if (IsPatchAlreadyApplied(nativeConsumeTarget, nativeConsumePrefix))
                    {
                        nativeConsumePatchApplied = true;
                        ModLogger.Log("[AmmoConsumptionPatch] Native OnWeaponAmmoConsume patch already applied, skipping");
                    }
                    else
                    {
                        HarmonyMethod harmonyPrefix = new(nativeConsumePrefix);
                        MethodInfo patchResult = Instance!.Patch(nativeConsumeTarget, prefix: harmonyPrefix);
                        nativeConsumePatchApplied = true;
                        ModLogger.Log($"[AmmoConsumptionPatch] Native OnWeaponAmmoConsume patch applied. Patch result: {patchResult}");
                    }
                }
                else
                {
                    ModLogger.Warning("[AmmoConsumptionPatch] Native OnWeaponAmmoConsume patch not applied (method not found)");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AmmoConsumptionPatch] Error applying native consume patch: {ex.Message}");
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
                        // Check if patch is already applied (e.g., by PatchAll())
                        if (IsPatchAlreadyApplied(fallbackTarget, fallbackPrefix))
                        {
                            ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Fallback patch already applied (likely by PatchAll()), skipping manual application");
                            fallbackPatchApplied = true;
                        }
                        else
                        {
                            HarmonyMethod harmonyPrefix = new(fallbackPrefix);
                            _ = Instance!.Patch(fallbackTarget, prefix: harmonyPrefix);
                            fallbackPatchApplied = true;
                            ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Fallback patch applied successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[MissionEquipmentAmmoSafetyPatch] Error applying fallback patch: {ex.Message}");
            }

            // Summary
            bool success = primaryPatchApplied || fallbackPatchApplied;
            if (!success)
            {
                ModLogger.Warning("[AmmoConsumptionPatch] No ammo patches could be applied!");
                ModLogger.Warning("[AmmoConsumptionPatch] Unlimited Ammo will rely on tick-based fallback restoration only.");
            }
            else
            {
                ModLogger.Log($"[AmmoConsumptionPatch] Ammo patches status: Primary={primaryPatchApplied}, Native={nativeConsumePatchApplied}, Fallback={fallbackPatchApplied}");
            }
            return success;
        }

        /// <summary>
        /// Applies AmmoConsumptionPatch dynamically for a combat mission.
        /// This method should be called in OnMissionBehaviorInitialize to apply the patch
        /// only during combat missions, preventing it from breaking character models in menus.
        /// </summary>
        /// <returns>True if patch was applied successfully, false otherwise.</returns>
        public static bool ApplyAmmoConsumptionPatchForMission()
        {
            // Only apply if not already applied
            if (AmmoConsumptionPatch.IsPatchApplied)
            {
                ModLogger.Debug("[AmmoConsumptionPatch] Patch already applied for this mission");
                return true;
            }

            return ApplyAmmoConsumptionPatch();
        }

        /// <summary>
        /// Removes AmmoConsumptionPatch after mission ends.
        /// This method should be called in OnEndMission to remove the patch
        /// and prevent it from affecting character models in menus.
        /// </summary>
        public static void RemoveAmmoConsumptionPatch()
        {
            try
            {
                if (Instance == null)
                {
                    return;
                }

                if (!AmmoConsumptionPatch.IsPatchApplied)
                {
                    return; // Patch not applied, nothing to remove
                }

                // Get target method
                MethodBase? targetMethod = AmmoConsumptionPatch.TargetMethod();
                if (targetMethod == null)
                {
                    return;
                }

                // Get all Prefix methods and try to unpatch each one
                // This handles both ref and non-ref versions
                MethodInfo[] prefixMethods = typeof(AmmoConsumptionPatch).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == "Prefix" || m.Name == "Prefix_NoRef")
                    .ToArray();

                bool anyUnpatched = false;
                foreach (MethodInfo prefixMethod in prefixMethods)
                {
                    try
                    {
                        Instance.Unpatch(targetMethod, prefixMethod);
                        anyUnpatched = true;
                    }
                    catch
                    {
                        // This prefix wasn't applied, try next one
                    }
                }

                if (anyUnpatched)
                {
                    AmmoConsumptionPatch.IsPatchApplied = false;
                    ModLogger.Debug("[AmmoConsumptionPatch] Patch removed after mission end");
                }

                // Also remove fallback patch if it exists
                MethodBase? fallbackTarget = MissionEquipmentAmmoSafetyPatch.TargetMethod();
                if (fallbackTarget != null)
                {
                    MethodInfo? fallbackPrefix = typeof(MissionEquipmentAmmoSafetyPatch).GetMethod("Prefix",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fallbackPrefix != null)
                    {
                        Instance.Unpatch(fallbackTarget, fallbackPrefix);
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[AmmoConsumptionPatch] Error removing patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually applies the InventoryCapacityPatch.
        /// </summary>
        /// <returns>True if patch was applied successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// This patch modifies inventory capacity calculations to provide unlimited carrying capacity.
        /// Applied manually to ensure it works correctly.
        /// </remarks>
        private static bool ApplyInventoryCapacityPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[InventoryCapacityPatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Get the target type - try loading from assembly first
                Type? targetType = null;

                // Try to get type from loaded assemblies
                foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    targetType = assembly.GetType("TaleWorlds.CampaignSystem.GameComponents.DefaultInventoryCapacityModel");
                    if (targetType != null)
                    {
                        break;
                    }
                }

                if (targetType == null)
                {
                    ModLogger.Warning("[InventoryCapacityPatch] Could not find DefaultInventoryCapacityModel type");
                    return false;
                }

                // Get all methods named CalculateInventoryCapacity
                MethodInfo[] allMethods = [.. targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name == "CalculateInventoryCapacity")];

                if (allMethods.Length == 0)
                {
                    ModLogger.Warning("[InventoryCapacityPatch] Could not find any CalculateInventoryCapacity method");
                    return false;
                }

                // Find the method with the correct signature (MobileParty, bool, int, int, int, bool)
                // According to decompiled code: CalculateInventoryCapacity(MobileParty mobileParty, bool includeDescriptions = false, int additionalTroops = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
                MethodInfo? targetMethod = allMethods.FirstOrDefault(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length >= 2 &&
                           parameters[0].ParameterType == typeof(MobileParty) &&
                           parameters[1].ParameterType == typeof(bool);
                });

                // If not found, try with just MobileParty as first parameter
                targetMethod ??= allMethods.FirstOrDefault(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length >= 1 &&
                           parameters[0].ParameterType == typeof(MobileParty);
                });

                if (targetMethod == null)
                {
                    ModLogger.Warning("[InventoryCapacityPatch] Could not find CalculateInventoryCapacity method with matching signature");
                    return false;
                }

                // Get the Postfix method from InventoryCapacityPatch
                MethodInfo? postfixMethod = typeof(InventoryCapacityPatch).GetMethod("Postfix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (postfixMethod == null)
                {
                    ModLogger.Warning("[InventoryCapacityPatch] Postfix method not found!");
                    return false;
                }

                // Check if patch is already applied (e.g., by PatchAll())
                if (IsPatchAlreadyApplied(targetMethod, postfixMethod))
                {
                    ModLogger.Log("[InventoryCapacityPatch] Patch already applied (likely by PatchAll()), skipping manual application");
                    return true;
                }

                // Apply the patch
                HarmonyMethod harmonyPostfix = new(postfixMethod);
                _ = Instance.Patch(targetMethod, postfix: harmonyPostfix);
                ModLogger.Log("[InventoryCapacityPatch] Patch applied successfully via manual patching");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[InventoryCapacityPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the ItemBarterablePatch.
        /// </summary>
        /// <returns>True if patch was applied successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// This patch prevents items from being removed from player's inventory during barter transactions.
        /// Applied manually to ensure it works correctly.
        /// </remarks>
        private static bool ApplyItemBarterablePatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[ItemBarterablePatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                MethodBase? targetMethod = typeof(ItemBarterable).GetMethod(nameof(ItemBarterable.Apply), BindingFlags.Public | BindingFlags.Instance);
                if (targetMethod == null)
                {
                    ModLogger.Error("[ItemBarterablePatch] Target method ItemBarterable.Apply not found!");
                    return false;
                }

                MethodInfo? prefixMethod = typeof(ItemBarterablePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo? postfixMethod = typeof(ItemBarterablePatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (prefixMethod == null || postfixMethod == null)
                {
                    ModLogger.Error("[ItemBarterablePatch] Prefix or Postfix method not found!");
                    return false;
                }

                // Check if patch is already applied (e.g., by PatchAll())
                if (IsPatchAlreadyApplied(targetMethod, prefixMethod) || IsPatchAlreadyApplied(targetMethod, postfixMethod))
                {
                    ModLogger.Log("[ItemBarterablePatch] Patch already applied (likely by PatchAll()), skipping manual application");
                    return true;
                }

                HarmonyMethod harmonyPrefix = new(prefixMethod);
                HarmonyMethod harmonyPostfix = new(postfixMethod);
                _ = Instance.Patch(targetMethod, prefix: harmonyPrefix, postfix: harmonyPostfix);

                ModLogger.Log("[ItemBarterablePatch] Patch applied successfully via manual patching");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ItemBarterablePatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the ItemRosterTradePatch.
        /// </summary>
        /// <returns>True if at least one method was patched successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// This patch prevents items from being removed from player's inventory during trade transactions.
        /// Applied manually to ensure it works correctly.
        /// </remarks>
        private static bool ApplyItemRosterTradePatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[ItemRosterTradePatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Find ALL AddToCounts methods and patch them all
                MethodInfo[] allMethods = [.. typeof(ItemRoster).GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => m.Name == "AddToCounts")];

                if (allMethods.Length == 0)
                {
                    ModLogger.Error("[ItemRosterTradePatch] Could not find any AddToCounts method!");
                    return false;
                }

                ModLogger.Log($"[ItemRosterTradePatch] Found {allMethods.Length} AddToCounts method(s), patching all:");
                foreach (MethodInfo method in allMethods)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    string paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    ModLogger.Log($"[ItemRosterTradePatch]   - AddToCounts({paramList})");
                }

                MethodInfo? prefixMethod = typeof(ItemRosterTradePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo? postfixMethod = typeof(ItemRosterTradePatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (prefixMethod == null || postfixMethod == null)
                {
                    ModLogger.Error($"[ItemRosterTradePatch] Prefix or Postfix method not found! (Prefix: {prefixMethod != null}, Postfix: {postfixMethod != null})");
                    return false;
                }

                HarmonyMethod harmonyPrefix = new(prefixMethod);
                HarmonyMethod harmonyPostfix = new(postfixMethod);

                // Patch ALL overloads
                int patchedCount = 0;
                foreach (MethodInfo method in allMethods)
                {
                    try
                    {
                        // Only patch methods with ItemObject and int parameters (our Prefix/Postfix signature)
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 2 &&
                            parameters[0].ParameterType == typeof(ItemObject) &&
                            parameters[1].ParameterType == typeof(int))
                        {
                            // Check if patch is already applied (e.g., by PatchAll())
                            if (IsPatchAlreadyApplied(method, prefixMethod) || IsPatchAlreadyApplied(method, postfixMethod))
                            {
                                ModLogger.Log($"[ItemRosterTradePatch] Patch already applied to {method.Name} (likely by PatchAll()), skipping");
                                patchedCount++;
                            }
                            else
                            {
                                _ = Instance.Patch(method, prefix: harmonyPrefix, postfix: harmonyPostfix);
                                patchedCount++;
                                ModLogger.Log($"[ItemRosterTradePatch] Successfully patched: AddToCounts({string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                            }
                        }
                        else
                        {
                            ModLogger.Log($"[ItemRosterTradePatch] Skipping overload with different signature: AddToCounts({string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Warning($"[ItemRosterTradePatch] Failed to patch method {method.Name}: {ex.Message}");
                    }
                }

                bool success = patchedCount > 0;
                ModLogger.Log($"[ItemRosterTradePatch] Patch applied successfully to {patchedCount} method(s)");
                return success;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ItemRosterTradePatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the GameSpeedPatch.
        /// </summary>
        /// <returns>True if patch was applied successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// This patch intercepts Campaign.TickMapTime() to apply a speed multiplier
        /// to both Play (1x) and Fast Forward (4x) button speeds.
        /// </remarks>
        private static bool ApplyGameSpeedPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[GameSpeedPatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Get the target method from the patch class
                MethodBase? targetMethod = GameSpeedPatch.TargetMethod();
                if (targetMethod == null)
                {
                    ModLogger.Warning("[GameSpeedPatch] TargetMethod() returned null - patch cannot be applied!");
                    ModLogger.Warning("[GameSpeedPatch] This is OK if the method doesn't exist in this game version.");
                    return false;
                }

                // Get the postfix method
                MethodInfo? postfixMethod = typeof(GameSpeedPatch).GetMethod("Postfix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (postfixMethod == null)
                {
                    ModLogger.Error("[GameSpeedPatch] Postfix method not found!");
                    return false;
                }

                // Check if patch is already applied (e.g., by PatchAll())
                if (IsPatchAlreadyApplied(targetMethod, postfixMethod))
                {
                    ModLogger.Log("[GameSpeedPatch] Patch already applied (likely by PatchAll()), skipping manual application");
                    return true;
                }

                // Apply the patch
                HarmonyMethod harmonyPostfix = new(postfixMethod);
                _ = Instance.Patch(targetMethod, postfix: harmonyPostfix);

                ModLogger.Log("[GameSpeedPatch] Patch applied successfully via manual patching");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[GameSpeedPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the MapTimeTrackerTickPatch.
        /// </summary>
        /// <returns>True if patch was applied successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// This patch intercepts MapTimeTracker.Tick() to apply a speed multiplier
        /// to the day/night cycle. This is necessary because Campaign.TickMapTime() calls
        /// MapTimeTracker.Tick() with a local variable instead of the modified '_dt' field.
        /// </remarks>
        private static bool ApplyMapTimeTrackerTickPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[MapTimeTrackerTickPatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Get the target method from the patch class
                MethodBase? targetMethod = MapTimeTrackerTickPatch.TargetMethod();
                if (targetMethod == null)
                {
                    ModLogger.Warning("[MapTimeTrackerTickPatch] TargetMethod() returned null - patch cannot be applied!");
                    ModLogger.Warning("[MapTimeTrackerTickPatch] This is OK if the method doesn't exist in this game version.");
                    return false;
                }

                // Get the prefix method
                MethodInfo? prefixMethod = typeof(MapTimeTrackerTickPatch).GetMethod("Prefix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (prefixMethod == null)
                {
                    ModLogger.Error("[MapTimeTrackerTickPatch] Prefix method not found!");
                    return false;
                }

                // Check if patch is already applied (e.g., by PatchAll())
                if (IsPatchAlreadyApplied(targetMethod, prefixMethod))
                {
                    ModLogger.Log("[MapTimeTrackerTickPatch] Patch already applied (likely by PatchAll()), skipping manual application");
                    return true;
                }

                // Apply the patch
                HarmonyMethod harmonyPrefix = new(prefixMethod);
                _ = Instance.Patch(targetMethod, prefix: harmonyPrefix);

                ModLogger.Log("[MapTimeTrackerTickPatch] Patch applied successfully via manual patching");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[MapTimeTrackerTickPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
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
