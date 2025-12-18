#nullable enable
// System namespaces
using System;
using System.Linq;
using System.Reflection;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

// Project namespaces
using BannerWand.Behaviors;
using BannerWand.Constants;
using BannerWand.Models;
using BannerWand.Patches;
using BannerWand.Utils;

namespace BannerWand.Core
{
    /// <summary>
    /// Main entry point for the BannerWand cheat mod for Mount &amp; Blade II: Bannerlord.
    /// This class extends <see cref="MBSubModuleBase"/> and manages the lifecycle of the mod,
    /// including initialization, game start/load/end events, and mission behaviors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BannerWand uses Bannerlord's official Game Model system instead of Harmony patches,
    /// making it more stable and compatible with game updates. Custom models are registered
    /// via <see cref="CampaignGameStarter.AddModel"/> which is the recommended approach.
    /// </para>
    /// <para>
    /// The mod also implements campaign behaviors for features that cannot be handled
    /// by game models alone, such as gold/influence editing and relationship management.
    /// </para>
    /// <para>
    /// Compatible with: .NET Framework 4.7.2, Bannerlord 1.3.x ONLY
    /// Mod Version: 1.1.1
    /// For Bannerlord 1.2.12, use BannerWand v1.0.9 (BannerWand-1.2.12 project)
    /// </para>
    /// </remarks>
    public class SubModule : MBSubModuleBase
    {
        /// <summary>
        /// Called when the mod module is loaded at the start of the game.
        /// This is the earliest entry point for initialization.
        /// </summary>
        /// <remarks>
        /// Initializes the <see cref="ModLogger"/> which creates the log file and
        /// prepares the logging system for use throughout the mod's lifetime.
        /// </remarks>
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // Initialize logger first - critical for all subsequent operations
            ModLogger.Initialize();
            ModLogger.Log("=== BannerWand Initialization ===");
            string modVersion = VersionReader.GetModVersion();
            ModLogger.Log($"Mod Version: {modVersion}");
            try
            {
                ModLogger.Log($"Game Version: {TaleWorlds.Library.ApplicationVersion.FromParametersFile()}");
            }
            catch (Exception ex)
            {
                // Fallback if version detection fails
                ModLogger.Warning($"Failed to detect game version: {ex.Message}. Using fallback.");
                ModLogger.Log("Game Version: 1.3.x (fallback)");
            }
            ModLogger.Log("Build Configuration: VERSION_1_3_X");
            ModLogger.Log("=================================");
            ModLogger.Log("BannerWand mod loading...");
            ModLogger.Log("Using Bannerlord's Game Model system + Harmony patches for advanced features");

            // Initialize Harmony patches
            if (HarmonyManager.Initialize())
            {
                ModLogger.Log("Harmony patches initialized successfully");
            }
            else
            {
                ModLogger.Error("Failed to initialize Harmony patches - some cheats may not work");
            }

            // Check if localization is working
            CheckLocalization();

            ModLogger.Log("BannerWand mod loaded successfully");
        }

        /// <summary>
        /// Checks if localization strings are being loaded correctly from XML files.
        /// </summary>
        /// <remarks>
        /// This method tests key localization strings to verify that the mod's
        /// ModuleData/Languages/ folder structure is correct and XML files are properly loaded.
        /// </remarks>
        private void CheckLocalization()
        {
            ModLogger.Log(MessageConstants.LocalizationCheckHeader);

            try
            {
                // Test a few key localization strings from the mod's XML files
                TextObject playerCategoryText = new("{=BW_Category_Player}Player");
                TextObject unlimitedHealthText = new("{=BW_Player_UnlimitedHealth}Unlimited Health");

                ModLogger.Log($"Player Category: '{playerCategoryText}'");
                ModLogger.Log($"Unlimited Health: '{unlimitedHealthText}'");

                // Verify localization is working by checking if the ID tag was resolved
                // If the string still contains the ID tag, localization failed to load
                string playerString = playerCategoryText.ToString();
                if (playerString.Contains("{=BW_Category_Player}"))
                {
                    ModLogger.Warning(MessageConstants.LocalizationNotWorking);
                    ModLogger.Warning(MessageConstants.LocalizationFolderStructure);
                    ModLogger.Warning(MessageConstants.LocalizationRequiredFiles);
                }
                else
                {
                    ModLogger.Log(string.Format(MessageConstants.LocalizationWorking, playerString));
                }
            }
            catch (Exception exception)
            {
                ModLogger.Error($"Error checking localization: {exception.Message}", exception);
                ModLogger.Log(MessageConstants.LocalizationCheckFooter);
            }
        }

        /// <summary>
        /// Called when a new game or campaign is started.
        /// Registers all custom game models and campaign behaviors.
        /// </summary>
        /// <param name="game">The <see cref="Game"/> instance being started.</param>
        /// <param name="starterObject">
        /// The game starter object. When starting a campaign, this will be a
        /// <see cref="CampaignGameStarter"/> which allows registering custom models and behaviors.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method performs critical initialization in the following order:
        /// 2. Initialize <see cref="CheatManager"/>
        /// 3. Register custom game models (replaces default Bannerlord models)
        /// 4. Register campaign behaviors (handles cheats not covered by models)
        /// </para>
        /// <para>
        /// IMPORTANT: Model registration must happen here and only here. Registering
        /// models elsewhere or multiple times can cause game instability.
        /// </para>
        /// </remarks>
        protected override void OnGameStart(Game game, IGameStarter starterObject)
        {
            base.OnGameStart(game, starterObject);

            ModLogger.Log("Game starting - initializing BannerWand systems...");

            // Only proceed if this is a campaign game
            if (starterObject is not CampaignGameStarter campaignStarter)
            {
                ModLogger.Warning("Game is not a campaign - BannerWand features disabled");
                return;
            }

            using (ModLogger.BeginPerformanceScope("Game Start Initialization"))
            {
                // Step 0: Show initialization message on first game launch (only once)
                try
                {
                    string modVersion = VersionReader.GetModVersion();
                    TextObject initMessage = new("{=BW_Message_ModInitialized}BannerWand {VERSION} successfully initialized");
                    initMessage.SetTextVariable("VERSION", modVersion);
                    InformationManager.DisplayMessage(new InformationMessage(initMessage.ToString(), GameConstants.SuccessColor));
                }
                catch (Exception imEx)
                {
                    ModLogger.Error($"Failed to display initialization message: {imEx.Message}");
                }

                // Step 1: Initialize cheat manager (silently, no duplicate messages - SubModule already showed init message)
                CheatManager.Initialize(showMessage: false);
                ModLogger.Log("CheatManager initialized successfully");

                // Step 1.5: Remove GarrisonWagesPatch if it was applied (prevents TypeInitializationException)
                // We now use CustomPartyWageModel instead, so the patch is no longer needed
                // This ensures that base.GetTotalWage() calls don't go through the patched method
                try
                {
                    RemoveGarrisonWagesPatchIfApplied();
                }
                catch (Exception ex)
                {
                    ModLogger.Warning($"Failed to remove GarrisonWagesPatch: {ex.Message}");
                }

                // Step 3: Register custom game models
                RegisterCustomModels(campaignStarter);

                // Step 4: Register campaign behaviors
                RegisterCampaignBehaviors(campaignStarter);

                // Step 5: Log current settings state for debugging
                ModLogger.LogSettingsState();
            }

            ModLogger.Log("Game start initialization completed successfully");
        }

        /// <summary>
        /// Called after game initialization is finished.
        /// This is called after all mods have registered their models, so we can safely patch DLC models here.
        /// </summary>
        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            base.OnAfterGameInitializationFinished(game, starterObject);

            // Apply NavalSpeedPatch for War Sails DLC after all mods have initialized
            // This ensures the DLC model is fully loaded before we patch it
            try
            {
                if (HarmonyManager.ApplyNavalSpeedPatch())
                {
                    ModLogger.Log("NavalSpeedPatch applied successfully in OnAfterGameInitializationFinished");
                }
                else
                {
                    ModLogger.Log("NavalSpeedPatch not applied (DLC may not be available or already patched)");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to apply NavalSpeedPatch in OnAfterGameInitializationFinished: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }

            // Note: GarrisonWagesPatch is now applied in OnGameStart instead of here
            // This ensures it's applied when loading saved games, not just on first launch
            // OnAfterGameInitializationFinished is only called once on first game start,
            // but OnGameStart is called for both new games and loaded saves
        }

        /// <summary>
        /// Registers all custom game models that replace default Bannerlord models.
        /// This is the official, Harmony-free way to modify game behavior.
        /// </summary>
        /// <param name="campaignStarter">The <see cref="CampaignGameStarter"/> to register models with.</param>
        /// <remarks>
        /// <para>
        /// Each custom model inherits from a default Bannerlord model and overrides specific
        /// methods to implement cheat functionality. The game engine automatically uses these
        /// custom models instead of the defaults.
        /// </para>
        /// <para>
        /// Models registered (in order):
        /// 1. <see cref="CustomPartySpeedModel"/> - Controls party movement speed on campaign map
        /// 2. <see cref="CustomPartyMoraleModel"/> - Controls party morale calculations
        /// 3. InventoryCapacityPatch (Harmony) - Controls carrying capacity limits
        /// 4. <see cref="CustomBarterModel"/> - Controls barter success rates
        /// 5. <see cref="CustomPersuasionModel"/> - Controls persuasion difficulty
        /// 6. <see cref="CustomBuildingConstructionModel"/> - Controls settlement building speed
        /// 7. <see cref="CustomSiegeEventModel"/> - Controls siege equipment construction speed
        /// 8. <see cref="CustomSmithingModel"/> - Controls smithing stamina costs
        /// 9. <see cref="CustomGenericXpModel"/> - Controls skill XP gain rates (optional)
        /// 10. <see cref="CustomSettlementGarrisonModel"/> - Controls garrison recruitment multiplier
        /// 11. <see cref="CustomSettlementMilitiaModel"/> - Controls militia recruitment multiplier and veteran chance
        /// 12. <see cref="CustomSettlementFoodModel"/> - Controls food growth bonus
        /// 13. <see cref="CustomSettlementProsperityModel"/> - Controls prosperity growth bonus (towns/castles) and hearth growth bonus (villages)
        /// 14. <see cref="CustomSettlementLoyaltyModel"/> - Controls loyalty growth bonus
        /// 15. <see cref="CustomSettlementSecurityModel"/> - Controls security growth bonus
        /// 16. <see cref="CustomPartyWageModel"/> - Controls garrison wages multiplier
        /// 17. <see cref="CustomClanTierModel"/> - Controls clan companion limit bonus
        /// </para>
        /// </remarks>
        private void RegisterCustomModels(CampaignGameStarter campaignStarter)
        {
            ModLogger.Log("Registering custom game models...");

            // Movement speed - replaces DefaultPartySpeedCalculatingModel
            // NOTE: If War Sails DLC is active, NavalDLCPartySpeedCalculationModel will be registered
            // by the DLC, and CustomPartySpeedModel will replace it. The NavalSpeedPatch will handle
            // speed overrides for sea travel, but CustomPartySpeedModel will handle land travel.
            campaignStarter.AddModel(new CustomPartySpeedModel());
            ModLogger.LogModelRegistration(nameof(CustomPartySpeedModel), "Controls party movement speed and AI slowdown (works with War Sails DLC)");

            // Morale - replaces DefaultPartyMoraleModel
            campaignStarter.AddModel(new CustomPartyMoraleModel());
            ModLogger.LogModelRegistration(nameof(CustomPartyMoraleModel), "Controls party morale calculations");

            // Barter - replaces DefaultBarterModel
            campaignStarter.AddModel(new CustomBarterModel());
            ModLogger.LogModelRegistration(nameof(CustomBarterModel), "Controls barter acceptance rates");

            // Persuasion - replaces DefaultPersuasionModel
            campaignStarter.AddModel(new CustomPersuasionModel());
            ModLogger.LogModelRegistration(nameof(CustomPersuasionModel), "Controls persuasion difficulty");

            // Building construction - replaces DefaultBuildingConstructionModel
            campaignStarter.AddModel(new CustomBuildingConstructionModel());
            ModLogger.LogModelRegistration(nameof(CustomBuildingConstructionModel), "Controls settlement building construction speed");

            // Settlement garrison - replaces DefaultSettlementGarrisonModel
            campaignStarter.AddModel(new CustomSettlementGarrisonModel());
            ModLogger.LogModelRegistration(nameof(CustomSettlementGarrisonModel), "Controls garrison recruitment multiplier");

            // Settlement militia - replaces DefaultSettlementMilitiaModel
            campaignStarter.AddModel(new CustomSettlementMilitiaModel());
            ModLogger.LogModelRegistration(nameof(CustomSettlementMilitiaModel), "Controls militia recruitment multiplier and veteran chance");

            // Settlement food - replaces DefaultSettlementFoodModel
            campaignStarter.AddModel(new CustomSettlementFoodModel());
            ModLogger.LogModelRegistration(nameof(CustomSettlementFoodModel), "Controls food growth bonus");

            // Settlement prosperity - replaces DefaultSettlementProsperityModel
            campaignStarter.AddModel(new CustomSettlementProsperityModel());
            ModLogger.LogModelRegistration(nameof(CustomSettlementProsperityModel), "Controls prosperity growth bonus (towns/castles) and hearth growth bonus (villages)");

            // Settlement loyalty - replaces DefaultSettlementLoyaltyModel
            campaignStarter.AddModel(new CustomSettlementLoyaltyModel());
            ModLogger.LogModelRegistration(nameof(CustomSettlementLoyaltyModel), "Controls loyalty growth bonus");

            // Settlement security - replaces DefaultSettlementSecurityModel
            campaignStarter.AddModel(new CustomSettlementSecurityModel());
            ModLogger.LogModelRegistration(nameof(CustomSettlementSecurityModel), "Controls security growth bonus");

            // Party wage - replaces DefaultPartyWageModel
            // This model applies garrison wages multiplier without using Harmony patches
            // Harmony patching DefaultPartyWageModel causes TypeInitializationException
            campaignStarter.AddModel(new CustomPartyWageModel());
            ModLogger.LogModelRegistration(nameof(CustomPartyWageModel), "Controls garrison wages multiplier");

            // Clan tier - replaces DefaultClanTierModel
            campaignStarter.AddModel(new CustomClanTierModel());
            ModLogger.LogModelRegistration(nameof(CustomClanTierModel), "Controls clan companion limit bonus");

            // Siege construction - replaces DefaultSiegeEventModel
            campaignStarter.AddModel(new CustomSiegeEventModel());
            ModLogger.LogModelRegistration(nameof(CustomSiegeEventModel), "Controls siege equipment construction speed");

            // Smithing - replaces DefaultSmithingModel
            campaignStarter.AddModel(new CustomSmithingModel());
            ModLogger.LogModelRegistration(nameof(CustomSmithingModel), "Controls smithing stamina consumption");

            // Prisoner recruitment - DISABLED due to API changes in Bannerlord 1.2.12+
            // GetDailyRecruitedPrisoners method no longer exists in DefaultPrisonerRecruitmentCalculationModel
            // NOTE: Prisoner recruitment control is handled via CustomPersuasionModel
            // The game's recruitment system uses persuasion mechanics, which are overridden by our custom model
            // campaignStarter.AddModel(new CustomPrisonerRecruitmentCalculationModel());
            // ModLogger.LogModelRegistration(nameof(CustomPrisonerRecruitmentCalculationModel), "Controls prisoner recruitment speed");

            // GenericXpModel - may not exist in all game versions, handle gracefully
            try
            {
                campaignStarter.AddModel(new CustomGenericXpModel());
                ModLogger.LogModelRegistration(nameof(CustomGenericXpModel), "Controls skill XP gain multipliers");
            }
            catch
            {
                ModLogger.Warning("CustomGenericXpModel could not be registered - using behavior-based XP boost as fallback");
            }

            // CombatXpModel - multiplies troop XP from battles
            try
            {
                campaignStarter.AddModel(new CustomCombatXpModel());
                ModLogger.LogModelRegistration(nameof(CustomCombatXpModel), "Controls troop XP gain from combat");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"CustomCombatXpModel could not be registered: {ex.Message}");
            }

            // PartyTrainingModel - multiplies troop XP from battles (including simulation battles)
            try
            {
                campaignStarter.AddModel(new CustomPartyTrainingModel());
                ModLogger.LogModelRegistration(nameof(CustomPartyTrainingModel), "Controls troop XP gain from battles (including auto-battles)");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"CustomPartyTrainingModel could not be registered: {ex.Message}");
            }

            // CombatSimulationModel - REMOVED (Party cheats removed)
            // CustomCombatSimulationModel registration removed as Unlimited Party HP and All Units Dead cheats have been removed

            ModLogger.Log("All custom game models registered successfully");
        }

        /// <summary>
        /// Registers campaign behaviors that handle cheats not covered by game models.
        /// </summary>
        /// <param name="campaignStarter">The <see cref="CampaignGameStarter"/> to register behaviors with.</param>
        /// <remarks>
        /// <para>
        /// Campaign behaviors are called by the game engine on various events (hourly tick,
        /// daily tick, session launch, etc.). They handle features that cannot be implemented
        /// via model overrides alone.
        /// </para>
        /// <para>
        /// Behaviors registered:
        /// 1. <see cref="PlayerCheatBehavior"/> - Gold, influence, relationships, smithing materials
        /// 2. <see cref="NPCCheatBehavior"/> - Attribute/focus points, renown for NPCs
        /// 3. <see cref="FoodCheatBehavior"/> - Unlimited food (alternative to model)
        /// 4. <see cref="SkillXPCheatBehavior"/> - Skill and troop XP boosts
        /// </para>
        /// </remarks>
        private void RegisterCampaignBehaviors(CampaignGameStarter campaignStarter)
        {
            ModLogger.Log("Registering campaign behaviors...");

            // Player-specific cheats (gold, influence, relationships, etc.)
            campaignStarter.AddBehavior(new PlayerCheatBehavior());
            ModLogger.LogBehaviorRegistration(nameof(PlayerCheatBehavior), "Handles player gold, influence, and relationship cheats");

            // NPC-specific cheats (attribute points, focus points, renown)
            campaignStarter.AddBehavior(new NPCCheatBehavior());
            ModLogger.LogBehaviorRegistration(nameof(NPCCheatBehavior), "Handles NPC attribute, focus, and renown cheats");

            // Food management (unlimited food cheat)
            campaignStarter.AddBehavior(new FoodCheatBehavior());
            ModLogger.LogBehaviorRegistration(nameof(FoodCheatBehavior), "Handles unlimited food cheat");

            // Skill and troop XP management
            campaignStarter.AddBehavior(new SkillXPCheatBehavior());
            ModLogger.LogBehaviorRegistration(nameof(SkillXPCheatBehavior), "Handles skill and troop XP multipliers");

            // Automatic building queue (starts next project after completion)
            campaignStarter.AddBehavior(new AutoBuildingQueueBehavior());
            ModLogger.LogBehaviorRegistration(nameof(AutoBuildingQueueBehavior), "Handles automatic building queue for settlements");


            ModLogger.Log("All campaign behaviors registered successfully");
        }

        /// <summary>
        /// Called when a mission (battle, arena, etc.) starts.
        /// Adds combat-specific cheat behaviors to the mission.
        /// </summary>
        /// <param name="mission">The <see cref="Mission"/> instance being initialized.</param>
        /// <remarks>
        /// <para>
        /// Mission behaviors are similar to campaign behaviors but operate during
        /// tactical battles and other missions. They handle real-time combat cheats
        /// such as unlimited health, one-hit kills, and shield durability.
        /// </para>
        /// <para>
        /// <see cref="CombatCheatBehavior"/> is added to ALL missions, but it only
        /// activates cheats based on the current settings.
        /// </para>
        /// </remarks>
        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            try
            {
                base.OnMissionBehaviorInitialize(mission);

                // Apply AmmoConsumptionPatch dynamically for this combat mission
                // This prevents the patch from breaking character models in menus
                _ = Core.HarmonyManager.ApplyAmmoConsumptionPatchForMission();

                // Add combat cheat behavior to all missions
                mission.AddMissionBehavior(new CombatCheatBehavior());
                ModLogger.Debug($"CombatCheatBehavior added to mission: {mission.SceneName}");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[SubModule] Error in OnMissionBehaviorInitialize: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Called when a saved game is loaded.
        /// Re-initializes the cheat manager and logs current settings.
        /// </summary>
        /// <param name="game">The <see cref="Game"/> instance being loaded.</param>
        /// <param name="initializerObject">The game initializer object.</param>
        /// <remarks>
        /// <para>
        /// When loading a saved game, behaviors and models are already registered,
        /// but we need to re-initialize the <see cref="CheatManager"/> to ensure
        /// it's in a clean state.
        /// </para>
        /// </remarks>
        public override void OnGameLoaded(Game game, object initializerObject)
        {
            try
            {
                base.OnGameLoaded(game, initializerObject);

                // Only process campaign games
                if (game.GameType is not Campaign)
                {
                    return;
                }

                // Re-initialize cheat manager for loaded game (silently, no duplicate messages)
                CheatManager.Initialize(showMessage: false);

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[SubModule] Error in OnGameLoaded: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Called when the game ends (returning to main menu or exiting to desktop).
        /// Cleans up resources.
        /// </summary>
        /// <param name="game">The <see cref="Game"/> instance that is ending.</param>
        public override void OnGameEnd(Game game)
        {
            try
            {
                base.OnGameEnd(game);

                ModLogger.Log("Game ending - cleaning up BannerWand...");

                // Only cleanup for campaign games
                if (game.GameType is not Campaign)
                {
                    return;
                }

                // Cleanup cheat manager
                CheatManager.Cleanup();
                ModLogger.Log("CheatManager cleaned up successfully");

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[SubModule] Error in OnGameEnd: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Called when the module is unloaded (game shutdown).
        /// Performs final cleanup.
        /// </summary>
        /// <remarks>
        /// This is the last chance to clean up resources before the game closes.
        /// </remarks>
        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

            ModLogger.Log("BannerWand module unloading - performing final cleanup...");
            ModLogger.Log("BannerWand mod unloaded successfully - goodbye!");
        }

        /// <summary>
        /// Called every application frame tick.
        /// Currently unused, reserved for future frame-based features.
        /// </summary>
        /// <param name="dt">Delta time since last frame in seconds.</param>
        /// <remarks>
        /// This method is called very frequently (60+ times per second).
        /// Avoid expensive operations here to prevent performance issues.
        /// </remarks>
        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            // Reserved for future per-frame updates if needed
            // Currently no frame-based cheats implemented
        }

        /// <summary>
        /// Removes GarrisonWagesPatch if it was applied, to prevent TypeInitializationException.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method removes the Harmony patch for DefaultPartyWageModel.GetTotalWage
        /// if it was applied (e.g., by PatchAll() before we removed the [HarmonyPatch] attribute).
        /// We now use CustomPartyWageModel instead, so the patch is no longer needed.
        /// </para>
        /// <para>
        /// Removing the patch ensures that base.GetTotalWage() calls in CustomPartyWageModel
        /// don't go through the patched method, which would trigger TypeInitializationException.
        /// </para>
        /// </remarks>
        private void RemoveGarrisonWagesPatchIfApplied()
        {
            try
            {
                if (HarmonyManager.Instance == null)
                {
                    return; // Harmony not initialized, nothing to remove
                }

                // Get the target method using reflection (same as in GarrisonWagesPatch.TargetMethod)
                Type? defaultPartyWageModelType = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type? foundType = assembly.GetType("TaleWorlds.CampaignSystem.GameComponents.DefaultPartyWageModel");
                    if (foundType != null)
                    {
                        defaultPartyWageModelType = foundType;
                        break;
                    }
                }

                if (defaultPartyWageModelType == null)
                {
                    ModLogger.Debug("[SubModule] DefaultPartyWageModel type not found - cannot remove patch");
                    return;
                }

                MethodInfo? targetMethod = defaultPartyWageModelType.GetMethod(
                    "GetTotalWage",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    [typeof(TaleWorlds.CampaignSystem.Party.MobileParty), typeof(TaleWorlds.CampaignSystem.Roster.TroopRoster), typeof(bool)],
                    null) as MethodInfo;

                if (targetMethod == null)
                {
                    ModLogger.Debug("[SubModule] DefaultPartyWageModel.GetTotalWage method not found - cannot remove patch");
                    return;
                }

                // Get the Postfix method from GarrisonWagesPatch
                MethodInfo? postfixMethod = typeof(GarrisonWagesPatch).GetMethod("GetTotalWage_Postfix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) as MethodInfo;

                if (postfixMethod == null)
                {
                    ModLogger.Debug("[SubModule] GarrisonWagesPatch.GetTotalWage_Postfix method not found - cannot remove patch");
                    return;
                }

                // Check if patch is actually applied before trying to remove it
                HarmonyLib.Patches? patchInfo = HarmonyLib.Harmony.GetPatchInfo(targetMethod);
                bool patchApplied = patchInfo?.Postfixes.Any(p => p.PatchMethod == postfixMethod ||
                        (p.PatchMethod.DeclaringType?.FullName == typeof(GarrisonWagesPatch).FullName &&
                         p.PatchMethod.Name == "GetTotalWage_Postfix")) == true;

                if (!patchApplied)
                {
                    ModLogger.Debug("[SubModule] GarrisonWagesPatch not applied - nothing to remove");
                    return;
                }

                // Try to remove the patch
                try
                {
                    HarmonyManager.Instance.Unpatch(targetMethod, postfixMethod);
                    ModLogger.Log("[SubModule] Successfully removed GarrisonWagesPatch (using CustomPartyWageModel instead)");
                }
                catch (Exception unpatchEx)
                {
                    // Patch might not be applied, which is fine
                    ModLogger.Debug($"[SubModule] Could not remove GarrisonWagesPatch (may not be applied): {unpatchEx.Message}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[SubModule] Error removing GarrisonWagesPatch: {ex.Message}");
                // Don't throw - this is not critical, just a cleanup operation
            }
        }

    }
}
