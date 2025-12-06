#nullable enable
using BannerWand.Patches;
using BannerWand.Utils;
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
    /// </para>
    /// <para>
    /// Patches applied via HarmonyTargetMethod (require manual application):
    /// - RenownMultiplierPatch: Patches Clan.AddRenown() to multiply renown gains
    /// - AmmoConsumptionPatch: Prevents ammo decrease for player when Unlimited Ammo enabled
    /// - ItemBarterablePatch: Prevents item loss during barter/trade transactions
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

                patchesTotal++;
                if (ApplyAmmoConsumptionPatch())
                {
                    patchesApplied++;
                }

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
                return existingPatches.Prefixes.Any(p => p.PatchMethod == patchMethod) ||
                       existingPatches.Postfixes.Any(p => p.PatchMethod == patchMethod) ||
                       existingPatches.Transpilers.Any(p => p.PatchMethod == patchMethod) ||
                       existingPatches.Finalizers.Any(p => p.PatchMethod == patchMethod);
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
            bool fallbackPatchApplied = false;

            // TEMPORARILY DISABLED: Testing if SetWeaponAmountInSlot patch causes character model corruption
            // If disabling this fixes the issue, we'll need to find an alternative approach
            ModLogger.Warning("[AmmoConsumptionPatch] TEMPORARILY DISABLED for testing - SetWeaponAmountInSlot patch");
            ModLogger.Warning("[AmmoConsumptionPatch] Unlimited Ammo will rely on tick-based restoration only");
            bool primaryPatchApplied = false;
            
            /* TEMPORARILY DISABLED FOR TESTING
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
                    // Find the Prefix method with 4 parameters (EquipmentIndex, short, bool)
                    MethodInfo? prefixMethod = typeof(AmmoConsumptionPatch).GetMethod("Prefix",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        [typeof(Agent), typeof(EquipmentIndex), typeof(short), typeof(bool)],
                        null);

                    // If not found, try with 3 parameters
                    prefixMethod ??= typeof(AmmoConsumptionPatch).GetMethod("Prefix",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            [typeof(Agent), typeof(EquipmentIndex), typeof(short)],
                            null);

                    // Last resort - get any Prefix method
                    prefixMethod ??= typeof(AmmoConsumptionPatch).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(m => m.Name == "Prefix");

                    if (prefixMethod != null)
                    {
                        // Check if patch is already applied (e.g., by PatchAll())
                        if (IsPatchAlreadyApplied(targetMethod, prefixMethod))
                        {
                            primaryPatchApplied = true;
                            AmmoConsumptionPatch.IsPatchApplied = true;
                        }
                        else
                        {
                            HarmonyMethod harmonyPrefix = new(prefixMethod);
                            _ = Instance.Patch(targetMethod, prefix: harmonyPrefix);
                            primaryPatchApplied = true;
                            AmmoConsumptionPatch.IsPatchApplied = true;
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
            */ // END TEMPORARY DISABLE

            // DISABLED: MissionEquipmentAmmoSafetyPatch is causing character model corruption
            // This patch modifies MissionEquipment.set_Item which is used for ALL equipment changes,
            // including visual model updates. Even with careful checks, it can interfere with
            // character rendering. The primary patch (SetWeaponAmountInSlot) should be sufficient.
            // 
            // If primary patch fails, we'll rely on tick-based restoration in CombatCheatBehavior.ApplyUnlimitedAmmo()
            ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Fallback patch DISABLED to prevent character model corruption");
            ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Using primary SetWeaponAmountInSlot patch only");

            // Summary
            bool success = primaryPatchApplied || fallbackPatchApplied;
            if (!success)
            {
                ModLogger.Warning("[AmmoConsumptionPatch] No ammo patches could be applied!");
                ModLogger.Warning("[AmmoConsumptionPatch] Unlimited Ammo will rely on tick-based fallback restoration only.");
            }
            return success;
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

                // Find the method with the correct signature (MobileParty, bool, bool, int, int, int, bool)
                // According to API docs: CalculateInventoryCapacity(MobileParty, bool isCurrentlyAtSea, bool includeDescriptions, int additionalTroops, int additionalSpareMounts, int additionalPackAnimals, bool includeFollowers)
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
                    return true;
                }

                // Apply the patch
                HarmonyMethod harmonyPostfix = new(postfixMethod);
                _ = Instance.Patch(targetMethod, postfix: harmonyPostfix);
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
