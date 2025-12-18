#nullable enable
// System namespaces
using System;
using System.Linq;
using System.Reflection;

// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

// Project namespaces
using BannerWand.Core.Harmony;
using BannerWand.Interfaces;
using BannerWand.Patches;
using BannerWand.Utils;

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
    /// - ItemBarterablePatch: Prevents item loss during barter transactions
    /// - ItemRosterTradePatch: Patches ItemRoster.AddToCounts() to prevent item removal during all trade types (towns, villages, caravans, etc.)
    /// - Note: GarrisonWagesPatch is deprecated - we now use CustomPartyWageModel instead
    ///   (Harmony patching DefaultPartyWageModel causes TypeInitializationException)
    /// </para>
    /// <para>
    /// Patches applied via HarmonyTargetMethod (require manual application):
    /// - RenownMultiplierPatch: Patches Clan.AddRenown() to multiply renown gains
    /// - AmmoConsumptionPatch: Prevents ammo decrease for player when Unlimited Ammo enabled
    /// - ItemBarterablePatch: Prevents item loss during barter/trade transactions
    /// - Note: GarrisonWagesPatch is deprecated - we now use CustomPartyWageModel instead
    ///   (Harmony patching DefaultPartyWageModel causes TypeInitializationException)
    /// - NavalSpeedPatch: Patches naval speed calculations for War Sails DLC
    ///   (Applied in OnAfterGameInitializationFinished because DLC loads later)
    /// </para>
    /// <para>
    /// This class uses dependency injection components (<see cref="IPatchApplier"/>, <see cref="IPatchValidator"/>, <see cref="IPatchLogger"/>)
    /// to improve modularity and testability.
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

        #region Fields

        /// <summary>
        /// The patch applier component for applying Harmony patches.
        /// </summary>
        private static IPatchApplier? _patchApplier;

        /// <summary>
        /// The patch validator component for checking patch application status.
        /// </summary>
        private static IPatchValidator? _patchValidator;

        /// <summary>
        /// The patch logger component for logging patch information.
        /// </summary>
        private static IPatchLogger? _patchLogger;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the active Harmony instance.
        /// </summary>
        public static HarmonyLib.Harmony? Instance { get; private set; }

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
                if (IsInitialized && Instance != null)
                {
                    ModLogger.Warning("Harmony already initialized, skipping re-initialization");
                    return true;
                }

                ModLogger.Log("Initializing Harmony patches...");

                // Create Harmony instance with unique mod identifier
                Instance = new HarmonyLib.Harmony(HarmonyId);

                // Initialize dependency injection components
                _patchApplier = new PatchApplier(Instance);
                _patchValidator = new PatchValidator();
                _patchLogger = new PatchLogger(Instance);

                // Strategy: PatchAll() applies patches with [HarmonyPatch] attributes automatically.
                // Manual patches (with [HarmonyTargetMethod]) are applied separately and checked
                // via IsPatchAlreadyApplied to prevent double-patching.
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
                // These patches use [HarmonyTargetMethod] and are not discovered by PatchAll()
                // Count patches dynamically to avoid hardcoding the total
                int patchesApplied = 0;
                int patchesTotal = 0;

                // Apply each patch and count both attempts and successes
                patchesTotal++;
                if (ApplyRenownMultiplierPatch())
                {
                    patchesApplied++;
                }

                // AmmoConsumptionPatch is NOT applied here during initialization
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

                // GarrisonWagesPatch is NOT applied here during initialization
                // It is applied in OnAfterGameInitializationFinished to prevent TypeInitializationException
                // because DefaultPartyWageModel's static constructor may depend on uninitialized game components
                // See ApplyGarrisonWagesPatch() documentation for details

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

                patchesTotal++;
                if (ApplyMobilePartySpeedPatch())
                {
                    patchesApplied++;
                }

                // AgingPatch is disabled - not working in current game version
                // See ApplyAgingPatch() method for details (commented out)

                // NavalSpeedPatch is applied in OnAfterGameInitializationFinished because DLC loads later
                // Don't apply it here in OnSubModuleLoad
                // GarrisonWagesPatch is applied in OnAfterGameInitializationFinished to prevent TypeInitializationException
                // Don't apply it here in OnSubModuleLoad

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
                    _patchLogger?.LogPatchedMethods();

                    return true;
                }
                else
                {
                    ModLogger.Error("All Harmony patches failed to apply! This may indicate a version mismatch or missing dependencies.");
                    ModLogger.Error("The mod may not function correctly. Please check the logs for details.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HarmonyManager] Error in Initialize: {ex.Message}", ex);
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

                // Clear component references
                _patchApplier = null;
                _patchValidator = null;
                _patchLogger = null;

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
        /// Checks if a patch method is already applied to a target method.
        /// </summary>
        /// <param name="targetMethod">The method that may be patched.</param>
        /// <param name="patchMethod">The patch method to check for.</param>
        /// <returns>True if the patch is already applied, false otherwise.</returns>
        /// <remarks>
        /// Delegates to <see cref="IPatchValidator.IsPatchAlreadyApplied"/> for validation logic.
        /// </remarks>
        private static bool IsPatchAlreadyApplied(MethodBase targetMethod, MethodInfo patchMethod)
        {
            if (_patchValidator == null)
            {
                ModLogger.Warning("[HarmonyManager] PatchValidator not initialized - cannot check patch status");
                return false;
            }

            return _patchValidator.IsPatchAlreadyApplied(targetMethod, patchMethod);
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
                    _patchLogger?.LogPatchApplication("RenownMultiplier", targetMethod, true);
                    return true;
                }

                // Apply the patch using PatchApplier
                if (_patchApplier?.ApplyPatch(targetMethod, prefixMethod, "prefix") == true)
                {
                    _patchLogger?.LogPatchApplication("RenownMultiplier", targetMethod, true);
                    ModLogger.Log("[RenownMultiplier] Patch applied successfully via manual patching");
                    return true;
                }
                else
                {
                    _patchLogger?.LogPatchApplication("RenownMultiplier", targetMethod, false);
                    return false;
                }
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

                    // CRITICAL: Different game versions have different method signatures for SetWeaponAmountInSlot
                    // We need to match the exact signature to apply the patch correctly
                    if (targetHasRef)
                    {
                        // Target method has ref parameter - use Prefix with ref
                        // Find the Prefix method with 4 parameters (EquipmentIndex, ref short, bool)
                        // The ref parameter allows us to modify the amount value directly
                        prefixMethod = typeof(AmmoConsumptionPatch).GetMethod("Prefix",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            [typeof(Agent), typeof(EquipmentIndex), typeof(short).MakeByRefType(), typeof(bool)],
                            null);

                        // If not found, try with 3 parameters (ref short) - fallback for older versions
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
                        // Without ref, we can only block the call, not modify the parameter
                        prefixMethod = typeof(AmmoConsumptionPatch).GetMethod("Prefix_NoRef",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            [typeof(Agent), typeof(EquipmentIndex), typeof(short), typeof(bool)],
                            null);
                    }

                    // Last resort - get Prefix or Prefix_NoRef method with most parameters
                    // This handles edge cases where signature detection fails but a compatible method exists
                    prefixMethod ??= typeof(AmmoConsumptionPatch).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(m => m.Name is "Prefix" or "Prefix_NoRef")
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
                            // Create HarmonyMethod with explicit argument types to ensure proper parameter matching
                            // Harmony may fail to match parameters if types are not explicitly specified
                            HarmonyMethod harmonyPrefix = new(prefixMethod)
                            {
                                argumentTypes = [.. targetMethod.GetParameters().Select(p => p.ParameterType)]
                            };

                            ModLogger.Log($"[AmmoConsumptionPatch] Applying patch: Target={targetMethod.DeclaringType?.FullName}.{targetMethod.Name}, Prefix={prefixMethod.DeclaringType?.FullName}.{prefixMethod.Name}");
                            ModLogger.Log($"[AmmoConsumptionPatch] Target parameters: {string.Join(", ", targetMethod.GetParameters().Select(p => p.ParameterType.Name))}");
                            ModLogger.Log($"[AmmoConsumptionPatch] Prefix parameters: {string.Join(", ", prefixMethod.GetParameters().Select(p => p.ParameterType.Name))}");

                            MethodInfo patchResult = Instance.Patch(targetMethod, prefix: harmonyPrefix);
                            primaryPatchApplied = true;
                            AmmoConsumptionPatch.IsPatchApplied = true;
                            ModLogger.Log($"[AmmoConsumptionPatch] Primary patch applied successfully via manual patching. Patch result: {patchResult}");

                            // Verify patch was actually applied
                            HarmonyLib.Patches patchInfo = HarmonyLib.Harmony.GetPatchInfo(targetMethod);
                            if (patchInfo != null)
                            {
                                ModLogger.Debug($"[AmmoConsumptionPatch] Verify: Target method has {patchInfo.Prefixes.Count} prefix(es) applied");
                                foreach (Patch? prefix in patchInfo.Prefixes)
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
                if (Instance == null)
                {
                    ModLogger.Warning("[AmmoConsumptionPatch] Harmony Instance is null - cannot apply native patch!");
                    return false;
                }

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
                        MethodInfo patchResult = Instance.Patch(nativeConsumeTarget, prefix: harmonyPrefix);
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

            // DISABLED: MissionEquipmentAmmoSafetyPatch is causing character model corruption
            // This patch modifies MissionEquipment.set_Item which is used for ALL equipment changes,
            // including visual model updates. Even with careful checks, it can interfere with
            // character rendering. The primary patch (SetWeaponAmountInSlot) should be sufficient.
            // 
            // If primary patch fails, we'll rely on tick-based restoration in CombatCheatBehavior.ApplyUnlimitedAmmo()
            ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Fallback patch DISABLED to prevent character model corruption");
            ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Using primary SetWeaponAmountInSlot patch only");

            // Summary
            bool success = primaryPatchApplied || nativeConsumePatchApplied;
            ModLogger.Log($"[AmmoConsumptionPatch] Ammo patches status: Primary={primaryPatchApplied}, Native={nativeConsumePatchApplied}");
            if (!success)
            {
                ModLogger.Warning("[AmmoConsumptionPatch] No ammo patches could be applied!");
                ModLogger.Warning("[AmmoConsumptionPatch] Unlimited Ammo will rely on tick-based fallback restoration only.");
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

                // Also remove OnWeaponAmmoConsume patch if it was applied
                MethodInfo? nativeConsumeTarget = typeof(Agent).GetMethod(
                    "OnWeaponAmmoConsume",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (nativeConsumeTarget != null)
                {
                    try
                    {
                        MethodInfo? nativeConsumePrefix = typeof(AmmoConsumptionPatch).GetMethod(
                            "OnWeaponAmmoConsume_Prefix",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                        if (nativeConsumePrefix != null)
                        {
                            Instance.Unpatch(nativeConsumeTarget, nativeConsumePrefix);
                        }
                    }
                    catch
                    {
                        // Patch might not be applied, which is fine
                    }
                }

                // Get all Prefix methods and try to unpatch each one
                // This handles both ref and non-ref versions
                MethodInfo[] prefixMethods = [.. typeof(AmmoConsumptionPatch).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name is "Prefix" or "Prefix_NoRef")];

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

                // Use UnpatchAll as fallback to ensure all patches from this mod are removed
                // This is more reliable than trying to unpatch individual methods
                if (anyUnpatched)
                {
                    AmmoConsumptionPatch.IsPatchApplied = false;
                    ModLogger.Debug("[AmmoConsumptionPatch] Patch removed after mission end");
                }
                else
                {
                    // If individual unpatch failed, try removing all patches from this method by our mod
                    // This ensures patches applied via PatchAll() are also removed
                    try
                    {
                        // Remove all patches from this method that belong to our mod
                        HarmonyLib.Patches? patchInfo = HarmonyLib.Harmony.GetPatchInfo(targetMethod);
                        if (patchInfo != null)
                        {
                            // Remove all prefixes from our mod
                            foreach (HarmonyLib.Patch prefix in patchInfo.Prefixes.ToList())
                            {
                                if (prefix.owner == HarmonyId)
                                {
                                    Instance.Unpatch(targetMethod, prefix.PatchMethod);
                                }
                            }
                        }
                        AmmoConsumptionPatch.IsPatchApplied = false;
                        ModLogger.Debug("[AmmoConsumptionPatch] Patch removed via fallback method after mission end");
                    }
                    catch
                    {
                        // Patch might not be applied, which is fine
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
                    _patchLogger?.LogPatchApplication("InventoryCapacity", targetMethod, true);
                    return true;
                }

                // Apply the patch using PatchApplier
                if (_patchApplier?.ApplyPatch(targetMethod, postfixMethod, "postfix") == true)
                {
                    _patchLogger?.LogPatchApplication("InventoryCapacity", targetMethod, true);
                    return true;
                }
                else
                {
                    _patchLogger?.LogPatchApplication("InventoryCapacity", targetMethod, false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[InventoryCapacityPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the GarrisonWagesPatch.
        /// </summary>
        /// <returns>True if patch was applied successfully or already applied, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// This patch modifies garrison wage calculations to apply the configured multiplier.
        /// Applied manually because it uses [HarmonyTargetMethod] which PatchAll() doesn't handle well.
        /// Patches the concrete implementation DefaultPartyWageModel.GetTotalWage, not the abstract method.
        /// </para>
        /// <para>
        /// IMPORTANT: This patch MUST be applied in OnGameStart, NOT in OnSubModuleLoad or OnAfterGameInitializationFinished.
        /// Reasons:
        /// 1. Patching DefaultPartyWageModel.GetTotalWage triggers the static constructor of DefaultPartyWageModel,
        ///    which may depend on game components that are not yet initialized during OnSubModuleLoad.
        ///    This causes TypeInitializationException.
        /// 2. OnAfterGameInitializationFinished is only called once on first game launch, not when loading saved games.
        ///    OnGameStart is called for both new games and loaded saves, ensuring the patch is always applied.
        /// 3. The patch method checks if it's already applied to prevent double-patching.
        /// </para>
        /// </remarks>
        public static bool ApplyGarrisonWagesPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[GarrisonWagesPatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Get the target method using HarmonyTargetMethod
                MethodBase? targetMethod = typeof(GarrisonWagesPatch).GetMethod("TargetMethod",
                    BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null) as MethodBase;

                if (targetMethod == null)
                {
                    ModLogger.Warning("[GarrisonWagesPatch] Could not find target method via HarmonyTargetMethod");
                    return false;
                }

                // Get the Postfix method from GarrisonWagesPatch
                MethodInfo? postfixMethod = typeof(GarrisonWagesPatch).GetMethod("GetTotalWage_Postfix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (postfixMethod == null)
                {
                    ModLogger.Warning("[GarrisonWagesPatch] Postfix method not found!");
                    return false;
                }

                // Check if patch is already applied (e.g., by PatchAll())
                if (IsPatchAlreadyApplied(targetMethod, postfixMethod))
                {
                    ModLogger.Log("[GarrisonWagesPatch] Patch already applied (likely by PatchAll()), skipping manual application");
                    _patchLogger?.LogPatchApplication("GarrisonWages", targetMethod, true);
                    return true;
                }

                // Apply the patch using PatchApplier
                if (_patchApplier?.ApplyPatch(targetMethod, postfixMethod, "postfix") == true)
                {
                    ModLogger.Log($"[GarrisonWagesPatch] Successfully applied patch to {targetMethod.DeclaringType?.Name}.{targetMethod.Name}");
                    _patchLogger?.LogPatchApplication("GarrisonWages", targetMethod, true);
                    return true;
                }
                else
                {
                    ModLogger.Warning($"[GarrisonWagesPatch] Failed to apply patch to {targetMethod.DeclaringType?.Name}.{targetMethod.Name}");
                    _patchLogger?.LogPatchApplication("GarrisonWages", targetMethod, false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[GarrisonWagesPatch] Error applying patch: {ex.Message}");
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
                    _patchLogger?.LogPatchApplication("ItemBarterable", targetMethod, true);
                    return true;
                }

                // Apply prefix and postfix patches
                // Note: PatchApplier doesn't support applying both prefix and postfix in one call,
                // so we use Instance.Patch directly for this case
                HarmonyMethod harmonyPrefix = new(prefixMethod);
                HarmonyMethod harmonyPostfix = new(postfixMethod);
                _ = Instance.Patch(targetMethod, prefix: harmonyPrefix, postfix: harmonyPostfix);

                _patchLogger?.LogPatchApplication("ItemBarterable", targetMethod, true);
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
                    _patchLogger?.LogPatchApplication("GameSpeed", targetMethod, true);
                    return true;
                }

                // Apply the patch using PatchApplier
                if (_patchApplier?.ApplyPatch(targetMethod, postfixMethod, "postfix") == true)
                {
                    _patchLogger?.LogPatchApplication("GameSpeed", targetMethod, true);
                    ModLogger.Log("[GameSpeedPatch] Patch applied successfully via manual patching");
                    return true;
                }
                else
                {
                    _patchLogger?.LogPatchApplication("GameSpeed", targetMethod, false);
                    return false;
                }
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
                    _patchLogger?.LogPatchApplication("MapTimeTrackerTick", targetMethod, true);
                    return true;
                }

                // Apply the patch using PatchApplier
                if (_patchApplier?.ApplyPatch(targetMethod, prefixMethod, "prefix") == true)
                {
                    _patchLogger?.LogPatchApplication("MapTimeTrackerTick", targetMethod, true);
                    ModLogger.Log("[MapTimeTrackerTickPatch] Patch applied successfully via manual patching");
                    return true;
                }
                else
                {
                    _patchLogger?.LogPatchApplication("MapTimeTrackerTick", targetMethod, false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[MapTimeTrackerTickPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// DISABLED: AgingPatch is currently disabled because aging prevention is not working
        /// in the current game version. The functionality has been removed from the codebase.
        /// </summary>
        /// <remarks>
        /// This method is kept for reference only. It is not called and the patch is not applied.
        /// TODO: Re-implement aging prevention in the future when the game API supports it.
        /// </remarks>
        /*
        private static bool ApplyAgingPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[AgingPatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Get the target method from the patch class
                MethodBase? targetMethod = AgingPatch.TargetMethod();
                if (targetMethod == null)
                {
                    ModLogger.Warning("[AgingPatch] TargetMethod() returned null - patch cannot be applied!");
                    ModLogger.Warning("[AgingPatch] This is OK if the BirthDay property setter doesn't exist in this game version.");
                    return false;
                }

                // Get the prefix method
                MethodInfo? prefixMethod = typeof(AgingPatch).GetMethod("Prefix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (prefixMethod == null)
                {
                    ModLogger.Error("[AgingPatch] Prefix method not found!");
                    return false;
                }

                // Check if patch is already applied (e.g., by PatchAll())
                if (IsPatchAlreadyApplied(targetMethod, prefixMethod))
                {
                    ModLogger.Log("[AgingPatch] Patch already applied (likely by PatchAll()), skipping manual application");
                    _patchLogger?.LogPatchApplication("Aging", targetMethod, true);
                    return true;
                }

                // Apply the patch using PatchApplier
                if (_patchApplier?.ApplyPatch(targetMethod, prefixMethod, "prefix") == true)
                {
                    _patchLogger?.LogPatchApplication("Aging", targetMethod, true);
                    ModLogger.Log("[AgingPatch] Patch applied successfully via manual patching");
                    return true;
                }
                else
                {
                    _patchLogger?.LogPatchApplication("Aging", targetMethod, false);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AgingPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        */

        /// <summary>
        /// Manually applies the MobilePartySpeedPatch to add a fixed speed bonus.
        /// </summary>
        /// <returns>True if patch was applied successfully, false otherwise.</returns>
        /// <remarks>
        /// This patch ensures that when the "Set Movement Speed" cheat is enabled,
        /// a fixed speed bonus is added to the party speed. The bonus is constant and doesn't fluctuate.
        /// It patches MobileParty.SpeedExplained to add the bonus (we don't patch CalculateSpeed
        /// because it typically calls SpeedExplained internally, which would cause double application).
        /// </remarks>
        public static bool ApplyMobilePartySpeedPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[MobilePartySpeedPatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // NOTE: We only patch SpeedExplained, not CalculateSpeed, because:
                // 1. CalculateSpeed typically calls SpeedExplained internally and returns ResultNumber
                // 2. Patching both would cause double application of the bonus
                // 3. SpeedExplained is the primary method used by the game for speed calculations

                // Apply SpeedExplained patch
                MethodBase? targetSpeedExplainedMethod = MobilePartySpeedPatch.TargetSpeedExplainedMethod();
                if (targetSpeedExplainedMethod != null)
                {
                    ModLogger.Log($"[MobilePartySpeedPatch] Target method found: {targetSpeedExplainedMethod.DeclaringType?.FullName}.{targetSpeedExplainedMethod.Name}");

                    MethodInfo? postfixMethod = typeof(MobilePartySpeedPatch).GetMethod(
                        "SpeedExplained_Postfix",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (postfixMethod != null)
                    {
                        if (IsPatchAlreadyApplied(targetSpeedExplainedMethod, postfixMethod))
                        {
                            ModLogger.Log("[MobilePartySpeedPatch] SpeedExplained patch already applied (likely by PatchAll()), skipping manual application");
                        }
                        else
                        {
                            HarmonyMethod harmonyPostfix = new(postfixMethod);
                            MethodInfo patchResult = Instance.Patch(targetSpeedExplainedMethod, postfix: harmonyPostfix);
                            ModLogger.Log($"[MobilePartySpeedPatch] SpeedExplained patch applied successfully. Patch result: {patchResult}");
                        }
                    }
                    else
                    {
                        ModLogger.Warning("[MobilePartySpeedPatch] SpeedExplained_Postfix method not found!");
                        return false;
                    }
                }
                else
                {
                    ModLogger.Warning("[MobilePartySpeedPatch] TargetSpeedExplainedMethod returned null!");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[MobilePartySpeedPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Manually applies the NavalSpeedPatch for War Sails DLC support.
        /// </summary>
        /// <returns>True if patch was applied successfully or NavalDLC is not available, false otherwise.</returns>
        /// <remarks>
        /// This patch supports War Sails DLC by patching NavalDLCPartySpeedCalculationModel
        /// to apply movement speed multiplier for both land and sea travel.
        /// If NavalDLC is not available, this patch gracefully skips application.
        /// This method should be called in OnGameStart, not OnSubModuleLoad, because DLC loads later.
        /// </remarks>
        public static bool ApplyNavalSpeedPatch()
        {
            try
            {
                if (Instance == null)
                {
                    ModLogger.Warning("[NavalSpeedPatch] Harmony Instance is null - cannot apply patch!");
                    return false;
                }

                // Get the target method from the patch class
                // Use direct type access since NavalSpeedPatch is in the same assembly
                MethodBase? targetFinalSpeedMethod = NavalSpeedPatch.TargetMethod();

                // If NavalDLC is not available, gracefully skip (not an error)
                if (targetFinalSpeedMethod == null)
                {
                    ModLogger.Log("[NavalSpeedPatch] NavalDLC not available - skipping patch (this is normal if War Sails DLC is not installed)");
                    return true; // Return true because skipping is expected behavior
                }

                bool finalSpeedPatchApplied = false;
                bool baseSpeedPatchApplied = false;

                // Apply CalculateBaseSpeed patch first (for sea travel)
                MethodBase? targetBaseSpeedMethod = NavalSpeedPatch.TargetCalculateBaseSpeedMethod();
                if (targetBaseSpeedMethod != null)
                {
                    ModLogger.Log($"[NavalSpeedPatch] Target method found: {targetBaseSpeedMethod.DeclaringType?.FullName}.{targetBaseSpeedMethod.Name}, Signature: {targetBaseSpeedMethod}");

                    MethodInfo? baseSpeedPostfixMethod = typeof(NavalSpeedPatch).GetMethod(
                        "CalculateBaseSpeed_Postfix",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (baseSpeedPostfixMethod != null)
                    {
                        ModLogger.Log($"[NavalSpeedPatch] Postfix method found: {baseSpeedPostfixMethod.DeclaringType?.FullName}.{baseSpeedPostfixMethod.Name}, Signature: {baseSpeedPostfixMethod}");

                        if (IsPatchAlreadyApplied(targetBaseSpeedMethod, baseSpeedPostfixMethod))
                        {
                            ModLogger.Log("[NavalSpeedPatch] CalculateBaseSpeed patch already applied (likely by PatchAll()), skipping manual application");
                            baseSpeedPatchApplied = true;
                        }
                        else
                        {
                            HarmonyMethod harmonyPostfix = new(baseSpeedPostfixMethod);
                            MethodInfo patchResult = Instance.Patch(targetBaseSpeedMethod, postfix: harmonyPostfix);
                            baseSpeedPatchApplied = true;
                            ModLogger.Log($"[NavalSpeedPatch] CalculateBaseSpeed patch applied successfully via manual patching. Patch result: {patchResult}");

                            // Verify patch was actually applied
                            HarmonyLib.Patches patchInfo = HarmonyLib.Harmony.GetPatchInfo(targetBaseSpeedMethod);
                            if (patchInfo != null)
                            {
                                ModLogger.Log($"[NavalSpeedPatch] Verify: CalculateBaseSpeed has {patchInfo.Postfixes.Count} postfix(es) applied");
                            }
                        }
                    }
                    else
                    {
                        ModLogger.Warning("[NavalSpeedPatch] CalculateBaseSpeed_Postfix method not found!");
                    }
                }
                else
                {
                    ModLogger.Warning("[NavalSpeedPatch] TargetCalculateBaseSpeedMethod returned null!");
                }

                // Apply CalculateFinalSpeed patch
                if (targetFinalSpeedMethod != null)
                {
                    ModLogger.Log($"[NavalSpeedPatch] Target method found: {targetFinalSpeedMethod.DeclaringType?.FullName}.{targetFinalSpeedMethod.Name}, Signature: {targetFinalSpeedMethod}");

                    MethodInfo? postfixMethod = typeof(NavalSpeedPatch).GetMethod(
                        "CalculateFinalSpeed_Postfix",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (postfixMethod != null)
                    {
                        ModLogger.Log($"[NavalSpeedPatch] Postfix method found: {postfixMethod.DeclaringType?.FullName}.{postfixMethod.Name}, Signature: {postfixMethod}");

                        // Check if patch is already applied (e.g., by PatchAll())
                        if (IsPatchAlreadyApplied(targetFinalSpeedMethod, postfixMethod))
                        {
                            ModLogger.Log("[NavalSpeedPatch] CalculateFinalSpeed patch already applied (likely by PatchAll()), skipping manual application");
                            finalSpeedPatchApplied = true;
                        }
                        else
                        {
                            HarmonyMethod harmonyPostfix = new(postfixMethod);
                            MethodInfo patchResult = Instance.Patch(targetFinalSpeedMethod, postfix: harmonyPostfix);
                            finalSpeedPatchApplied = true;
                            ModLogger.Log($"[NavalSpeedPatch] CalculateFinalSpeed patch applied successfully via manual patching. Patch result: {patchResult}");

                            // Verify patch was actually applied
                            HarmonyLib.Patches patchInfo = HarmonyLib.Harmony.GetPatchInfo(targetFinalSpeedMethod);
                            if (patchInfo != null)
                            {
                                ModLogger.Log($"[NavalSpeedPatch] Verify: CalculateFinalSpeed has {patchInfo.Postfixes.Count} postfix(es) applied");
                            }
                        }
                    }
                    else
                    {
                        ModLogger.Warning("[NavalSpeedPatch] CalculateFinalSpeed_Postfix method not found!");
                    }
                }
                else
                {
                    ModLogger.Warning("[NavalSpeedPatch] TargetMethod returned null!");
                }

                bool success = baseSpeedPatchApplied && finalSpeedPatchApplied;
                if (!success && targetFinalSpeedMethod != null)
                {
                    ModLogger.Warning("[NavalSpeedPatch] Failed to apply patches - War Sails speed multiplier may not work correctly");
                }

                return success;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[NavalSpeedPatch] Error applying patch: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // Return true because NavalDLC might not be available (not an error)
                return true;
            }
        }


        #endregion
    }
}
