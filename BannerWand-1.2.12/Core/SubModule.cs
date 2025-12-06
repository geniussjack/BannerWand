using BannerWandRetro.Behaviors;
using BannerWandRetro.Constants;
using BannerWandRetro.Models;
using BannerWandRetro.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Core
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
    /// Compatible with: .NET Framework 4.7.2, Bannerlord 1.2.12 ONLY
    /// Mod Version: 1.0.9
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
                // In Bannerlord 1.2.12, ApplicationVersion API differs from 1.3
                // Try to get version using available API
                TaleWorlds.Library.ApplicationVersion appVersion = TaleWorlds.Library.ApplicationVersion.FromParametersFile();
                ModLogger.Log($"Game Version: {appVersion}");
            }
            catch
            {
                // Fallback if version detection fails
                ModLogger.Log("Game Version: 1.2.12 (fallback)");
            }
            ModLogger.Log("Build Configuration: VERSION_1_2_12");
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
            ModLogger.Log("=== Localization Check ===");

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
                    ModLogger.Warning("Localization NOT working - strings contain {=ID} tags!");
                    ModLogger.Warning("Make sure ModuleData/Languages/ folder structure is correct");
                    ModLogger.Warning("Required files: language_data.xml and strings.xml in each language folder");
                }
                else
                {
                    ModLogger.Log($"Localization working! Loaded text: '{playerString}'");
                }
            }
            catch (Exception exception)
            {
                ModLogger.Error($"Exception in SubModule.cs - {exception}: {exception.Message}");
                ModLogger.Error($"Stack trace: {exception.StackTrace}");
                ModLogger.Error($"Error checking localization: {exception.Message}");
                ModLogger.Log("=== End Localization Check ===");
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
        /// 1. Auto-reset dangerous settings (safety measure for crash recovery)
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
                    string initMessage = string.Format(MessageConstants.ModInitializedSuccessfullyFormat, modVersion);
                    InformationManager.DisplayMessage(new InformationMessage(initMessage, GameConstants.SuccessColor));
                }
                catch (Exception imEx)
                {
                    ModLogger.Error($"Failed to display initialization message: {imEx.Message}");
                }

                // Step 1: Initialize cheat manager (silently, no duplicate messages - SubModule already showed init message)
                CheatManager.Initialize(showMessage: false);
                ModLogger.Log("CheatManager initialized successfully");

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
        /// 3. <see cref="CustomInventoryCapacityModel"/> - Controls carrying capacity limits
        /// 4. <see cref="CustomBarterModel"/> - Controls barter success rates
        /// 5. <see cref="CustomPersuasionModel"/> - Controls persuasion difficulty
        /// 6. <see cref="CustomBuildingConstructionModel"/> - Controls settlement building speed
        /// 7. <see cref="CustomSiegeEventModel"/> - Controls siege equipment construction speed
        /// 8. <see cref="CustomSmithingModel"/> - Controls smithing stamina costs
        /// 9. <see cref="CustomGenericXpModel"/> - Controls skill XP gain rates (optional)
        /// </para>
        /// </remarks>
        private void RegisterCustomModels(CampaignGameStarter campaignStarter)
        {
            ModLogger.Log("Registering custom game models...");

            // Movement speed - replaces DefaultPartySpeedCalculatingModel
            campaignStarter.AddModel(new CustomPartySpeedModel());
            ModLogger.LogModelRegistration(nameof(CustomPartySpeedModel), "Controls party movement speed and AI slowdown");

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

            // Siege construction - replaces DefaultSiegeEventModel
            campaignStarter.AddModel(new CustomSiegeEventModel());
            ModLogger.LogModelRegistration(nameof(CustomSiegeEventModel), "Controls siege equipment construction speed");

            // Smithing - replaces DefaultSmithingModel
            campaignStarter.AddModel(new CustomSmithingModel());
            ModLogger.LogModelRegistration(nameof(CustomSmithingModel), "Controls smithing stamina consumption");

            // Prisoner recruitment - DISABLED due to API changes in Bannerlord 1.2.12+
            // GetDailyRecruitedPrisoners method no longer exists in DefaultPrisonerRecruitmentCalculationModel
            // TODO: Find alternative API for prisoner recruitment control
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

                // Add combat cheat behavior to all missions
                mission.AddMissionBehavior(new CombatCheatBehavior());
                ModLogger.Debug($"CombatCheatBehavior added to mission: {mission.SceneName}");

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in SubModule.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[SubModule] Error in OnMissionBehaviorInitialize: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
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
                ModLogger.Error($"Exception in SubModule.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[SubModule] Error in OnGameLoaded: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
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

                ModLogger.Log("Game ending - cleaning up BannerWandRetro...");

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
                ModLogger.Error($"Exception in SubModule.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[SubModule] Error in OnGameEnd: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
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

    }
}
