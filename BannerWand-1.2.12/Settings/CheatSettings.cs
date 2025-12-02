using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace BannerWandRetro.Settings
{
    /// <summary>
    /// Main settings class for BannerWand cheat mod, managed by Mod Configuration Menu (MCM).
    /// Contains all cheat toggles and values that players can configure in-game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class integrates with MCM (Mod Configuration Menu) to provide an in-game settings UI.
    /// Players can enable/disable cheats and adjust multipliers without editing files or restarting.
    /// </para>
    /// <para>
    /// Settings are persisted automatically by MCM to:
    /// Documents/Mount and Blade II Bannerlord/Configs/ModSettings/Global/BannerWand/settings.json
    /// </para>
    /// <para>
    /// All settings have default values (false for booleans, 0 for numbers) to ensure
    /// the mod starts in a "clean" state with no cheats active.
    /// </para>
    /// <para>
    /// Settings Organization:
    /// - Player: Health, movement, combat-related cheats
    /// - Inventory: Gold, items, carrying capacity
    /// - Stats: Skills, XP, renown, character development
    /// - Enemies: AI modifications
    /// - Game: Time, persuasion, construction speed
    /// </para>
    /// </remarks>
    public class CheatSettings : AttributeGlobalSettings<CheatSettings>
    {
        #region MCM Configuration

        /// <summary>
        /// Gets the unique identifier for this settings instance.
        /// Used by MCM to distinguish this mod from others.
        /// </summary>
        public override string Id => "BannerWand";

        /// <summary>
        /// Gets the display name shown in MCM settings menu.
        /// </summary>
        public override string DisplayName => "BannerWand";

        /// <summary>
        /// Gets the folder name for settings persistence.
        /// Settings are stored in Documents/Mount and Blade II Bannerlord/Configs/ModSettings/Global/BannerWand/
        /// </summary>
        public override string FolderName => "BannerWand";

        /// <summary>
        /// Gets the settings file format version.
        /// Increment this if settings structure changes significantly to trigger migration.
        /// </summary>
        public override string FormatType => "json2";

        #endregion

        #region Player Category

        /// <summary>
        /// Player health bar never decreases (based on max HP limit).
        /// WARNING: Can still die from one-shot damage exceeding max HP.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedHealth}Unlimited HP", Order = 0, RequireRestart = false, HintText = "{=BW_Player_UnlimitedHealth_Hint}Health bar never decreases. Can still die from damage exceeding max HP.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool UnlimitedHealth { get; set; } = false;

        /// <summary>
        /// Adds +9999 health at the start of each battle.
        /// Prevents death from high damage attacks.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_InfiniteHealth}Infinite Health", Order = 1, RequireRestart = false, HintText = "{=BW_Player_InfiniteHealth_Hint}Adds +9999 HP at battle start. Prevents one-shot kills.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool InfiniteHealth { get; set; } = false;

        /// <summary>
        /// Player's mount never loses health.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedHorseHealth}Unlimited Horse Health", Order = 2, RequireRestart = false, HintText = "{=BW_Player_UnlimitedHorseHealth_Hint}Player's horse takes no damage.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool UnlimitedHorseHealth { get; set; } = false;

        /// <summary>
        /// Player's shield never breaks.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedShieldDurability}Unlimited Shield Durability", Order = 3, RequireRestart = false, HintText = "{=BW_Player_UnlimitedShieldDurability_Hint}Takes effect when you block enemy attacks.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool UnlimitedShieldDurability { get; set; } = false;

        /// <summary>
        /// Player never runs out of ammunition for ranged weapons (bows, crossbows, throwables).
        /// Ammunition is maintained at 999 for all ranged weapons.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedAmmo}Unlimited Ammo", Order = 4, RequireRestart = false, HintText = "{=BW_Player_UnlimitedAmmo_Hint}Ammunition for bows, crossbows, and throwables is maintained at 999.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool UnlimitedAmmo { get; set; } = false;

        /// <summary>
        /// Party morale locked at maximum (100).
        /// Implemented in <see cref="Models.CustomPartyMoraleModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_MaxMorale}Max Morale", Order = 4, RequireRestart = false, HintText = "{=BW_Player_MaxMorale_Hint}Player's party always has maximum morale.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool MaxMorale { get; set; } = false;

        /// <summary>
        /// Campaign map movement speed multiplier (1.0 = normal, 2.0 = double).
        /// Implemented in <see cref="Models.CustomPartySpeedModel"/>.
        /// NOTE: Changes apply on the next in-game day.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Player_MovementSpeed}Set Movement Speed", 0f, 16f, Order = 5, RequireRestart = false, HintText = "{=BW_Player_MovementSpeed_Hint}Only works on map, not in battle. Changes apply on the next in-game day.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public float MovementSpeed { get; set; } = 0f;

        /// <summary>
        /// Any barter/trade offer is automatically accepted by NPCs.
        /// Implemented in <see cref="Models.CustomBarterModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_BarterAlwaysAccepted}Barter Offer Always Accepted", Order = 6, RequireRestart = false, HintText = "{=BW_Player_BarterAlwaysAccepted_Hint}All barter offers are automatically accepted.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool BarterAlwaysAccepted { get; set; } = false;

        /// <summary>
        /// Smithing and smelting actions cost no stamina.
        /// Implemented in <see cref="Models.CustomSmithingModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedSmithyStamina}Unlimited Smithy Stamina", Order = 7, RequireRestart = false, HintText = "{=BW_Player_UnlimitedSmithyStamina_Hint}Smithing never consumes stamina.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool UnlimitedSmithyStamina { get; set; } = false;

        /// <summary>
        /// Boosts any positive relationship change involving the player to +99.
        /// Implemented in <see cref="Patches.RelationshipBoostPatch"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_MaxCharacterRelationship}Max Character Relationship", Order = 8, RequireRestart = false, HintText = "{=BW_Player_MaxCharacterRelationship_Hint}Any positive relationship change with player is boosted to +99.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool MaxCharacterRelationship { get; set; } = false;

        /// <summary>
        /// Instantly maximizes player's relationship with ALL characters to 100.
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_MaxAllCharacterRelationships}Max All Character Relationships", Order = 9, RequireRestart = false, HintText = "{=BW_Player_MaxAllCharacterRelationships_Hint}Instantly sets relationship to 100 with all characters.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool MaxAllCharacterRelationships { get; set; } = false;

        #endregion

        #region Inventory Category

        /// <summary>
        /// Add or remove gold (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Inventory_EditGold}Edit Gold", -1000000, 1000000, Order = 0, RequireRestart = false, HintText = "{=BW_Inventory_EditGold_Hint}Add or remove gold (applied once when value changed).")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public int EditGold { get; set; } = 0;

        /// <summary>
        /// Add or remove clan influence (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Inventory_EditInfluence}Edit Influence", -10000, 10000, Order = 1, RequireRestart = false, HintText = "{=BW_Inventory_EditInfluence_Hint}Add or remove influence (applied once when value changed).")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public int EditInfluence { get; set; } = 0;

        /// <summary>
        /// Party never consumes food.
        /// Implemented in <see cref="Models.CustomMobilePartyFoodConsumptionModel"/> and <see cref="Behaviors.FoodCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_UnlimitedFood}Unlimited Food", Order = 2, RequireRestart = false, HintText = "{=BW_Inventory_UnlimitedFood_Hint}Party food never decreases.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public bool UnlimitedFood { get; set; } = false;

        /// <summary>
        /// [WIP] Trade items would not decrease when sold/given.
        /// Currently non-functional - requires further implementation.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_TradeItemsNoDecrease}[WIP] Trade/Exchange Items Don't Decrease", Order = 3, RequireRestart = false, HintText = "{=BW_Inventory_TradeItemsNoDecrease_Hint}[WORK IN PROGRESS] This feature is not yet implemented.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public bool TradeItemsNoDecrease { get; set; } = false;

        /// <summary>
        /// Sets carrying capacity to ~1 million (effectively unlimited).
        /// Implemented in <see cref="Models.CustomInventoryCapacityModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_MaxCarryingCapacity}Max Carrying Capacity", Order = 4, RequireRestart = false, HintText = "{=BW_Inventory_MaxCarryingCapacity_Hint}Party has unlimited carrying capacity.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public bool MaxCarryingCapacity { get; set; } = false;

        /// <summary>
        /// Automatically replenishes iron and charcoal for smithing.
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_UnlimitedSmithyMaterials}Unlimited Smithy Materials", Order = 5, RequireRestart = false, HintText = "{=BW_Inventory_UnlimitedSmithyMaterials_Hint}Only works for materials that you already owned, it has no effect on materials that you didn't own.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public bool UnlimitedSmithyMaterials { get; set; } = false;

        /// <summary>
        /// [WIP] Unlocks all smithing parts/recipes.
        /// NOTE: In Bannerlord 1.2.12 requires manual activation via console commands.
        /// Check the log file for instructions.
        /// </summary>
        //
        [SettingPropertyBool("{=BW_Inventory_UnlockAllSmithyParts}[WIP] Unlock All Smithy Parts", Order = 6, RequireRestart = false, HintText = "{=BW_Inventory_UnlockAllSmithyParts_Hint}[WORK IN PROGRESS] All smithing parts are unlocked.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public bool UnlockAllSmithyParts { get; set; } = false;

        #endregion

        #region Stats Category

        /// <summary>
        /// Add or remove unspent attribute points (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Stats_EditAttributePoints}Edit Attribute Points", -1000, 1000, Order = 0, RequireRestart = false, HintText = "{=BW_Stats_EditAttributePoints_Hint}Takes effect when opening the character menu twice. To disable this option, you need to set the value to -1.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public int EditAttributePoints { get; set; } = 0;

        /// <summary>
        /// Add or remove unspent focus points (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Stats_EditFocusPoints}Edit Focus Points", -1000, 1000, Order = 1, RequireRestart = false, HintText = "{=BW_Stats_EditFocusPoints_Hint}Takes effect when opening the character menu twice. To disable this option, you need to set the value to -1.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public int EditFocusPoints { get; set; } = 0;

        /// <summary>
        /// Gradually increases clan renown to 10,000 (adds 999 renown/hour).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Stats_UnlimitedRenown}Unlimited Renown", Order = 2, RequireRestart = false, HintText = "{=BW_Stats_UnlimitedRenown_Hint}Note sometime you may not gain any renown even though the game says you gained renown. Especially when enemies are fled. This might be a game bug. Takes effect when gaining renown.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public bool UnlimitedRenown { get; set; } = false;

        /// <summary>
        /// WARNING: Currently non-functional - use Unlimited Renown instead.
        /// Would multiply renown gains.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Stats_RenownMultiplier}Renown Multiplier", 0f, 16f, Order = 3, RequireRestart = false, HintText = "{=BW_Stats_RenownMultiplier_Hint}Note sometime you may not gain any renown even though the game says you gained renown. Especially when enemies are fled. This might be a game bug. The display value is not affected, the actual amount you're gaining is multiplied.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public float RenownMultiplier { get; set; } = 0f;

        /// <summary>
        /// Rapidly levels all skills to maximum (adds 999 XP/hour per skill).
        /// Implemented in <see cref="Behaviors.SkillXPCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Stats_UnlimitedSkillXP}Unlimited Skill XP", Order = 4, RequireRestart = false, HintText = "{=BW_Stats_UnlimitedSkillXP_Hint}Takes effect when a skill gains XP. Note skills have level caps based on their learning rate which affected by your attributes. If a skill's learning rate is 0, it won't gain any XP, you need to spend attribute points to raise your attributes in order to raise learning rate.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public bool UnlimitedSkillXP { get; set; } = false;

        /// <summary>
        /// Multiplies skill XP gains (1.0 = normal, 2.0 = double, etc.).
        /// Implemented in <see cref="Models.CustomGenericXpModel"/> (if available).
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Stats_SkillXPMultiplier}Skill XP Multiplier", 0f, 16f, Order = 5, RequireRestart = false, HintText = "{=BW_Stats_SkillXPMultiplier_Hint}Multiply skill XP gains (0 = disabled).")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public float SkillXPMultiplier { get; set; } = 0f;

        /// <summary>
        /// Rapidly levels all troops (adds 999 XP/hour per troop).
        /// Implemented in <see cref="Behaviors.SkillXPCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Stats_UnlimitedTroopsXP}Unlimited Troops XP", Order = 6, RequireRestart = false, HintText = "{=BW_Stats_UnlimitedTroopsXP_Hint}All troops gain XP without limit.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public bool UnlimitedTroopsXP { get; set; } = false;

        /// <summary>
        /// Multiplies troop XP gains (1.0 = normal, 2.0 = double, etc.).
        /// Implemented in <see cref="Behaviors.SkillXPCheatBehavior"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Stats_TroopsXPMultiplier}Troops XP Multiplier", 0f, 16f, Order = 7, RequireRestart = false, HintText = "{=BW_Stats_TroopsXPMultiplier_Hint}Multiply troop XP gains (0 = disabled).")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 2)]
        public float TroopsXPMultiplier { get; set; } = 0f;

        #endregion

        #region Enemies Category

        /// <summary>
        /// [WIP] Reduces all AI party speeds to 50% of normal.
        /// Currently has accuracy issues - may not reduce speed precisely.
        /// </summary>
        [SettingPropertyBool("{=BW_Enemies_SlowAIMovementSpeed}[WIP] Slow AI Movement Speed", Order = 0, RequireRestart = false, HintText = "{=BW_Enemies_SlowAIMovementSpeed_Hint}[WORK IN PROGRESS] Reduces enemy speed but accuracy needs improvement.")]
        [SettingPropertyGroup("{=BW_Category_Enemies}Enemies", GroupOrder = 3)]
        public bool SlowAIMovementSpeed { get; set; } = false;

        /// <summary>
        /// All enemy combatants die from a single hit.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Enemies_OneHitKills}One-Hit Kills", Order = 1, RequireRestart = false, HintText = "{=BW_Enemies_OneHitKills_Hint}All enemy units die in one hit.")]
        [SettingPropertyGroup("{=BW_Category_Enemies}Enemies", GroupOrder = 3)]
        public bool OneHitKills { get; set; } = false;

        #endregion

        #region Game Category

        /// <summary>
        /// [WIP] Stops time progression on campaign map.
        /// Currently pauses the entire game instead of just time.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_FreezeDaytime}[WIP] Freeze Daytime", Order = 0, RequireRestart = false, HintText = "{=BW_Game_FreezeDaytime_Hint}[WORK IN PROGRESS] Currently freezes entire game, not just time.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 4)]
        public bool FreezeDaytime { get; set; } = false;

        /// <summary>
        /// All persuasion attempts automatically succeed.
        /// Implemented in <see cref="Models.CustomPersuasionModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_PersuasionAlwaysSucceed}Persuasion/Conversation Always Succeed", Order = 1, RequireRestart = false, HintText = "{=BW_Game_PersuasionAlwaysSucceed_Hint}All persuasion and conversation checks automatically succeed.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 4)]
        public bool PersuasionAlwaysSucceed { get; set; } = false;

        /// <summary>
        /// All settlement buildings complete construction in one day.
        /// Implemented in <see cref="Models.CustomBuildingConstructionModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_OneDaySettlementsConstruction}One Day Settlements Construction", Order = 2, RequireRestart = false, HintText = "{=BW_Game_OneDaySettlementsConstruction_Hint}Only one building can be constructed at a time. Building/upgrading multiple buildings still requires multiple days.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 4)]
        public bool OneDaySettlementsConstruction { get; set; } = false;

        /// <summary>
        /// Siege equipment builds instantly for player.
        /// Implemented in <see cref="Models.CustomSiegeEventModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_InstantSiegeConstruction}Instant Siege Construction", Order = 3, RequireRestart = false, HintText = "{=BW_Game_InstantSiegeConstruction_Hint}Note this option affects both sides.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 4)]
        public bool InstantSiegeConstruction { get; set; } = false;

        /// <summary>
        /// Campaign time flow speed (0 = paused, 1 = normal, 4 = fast forward).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Game_GameSpeed}[WIP] Set Game Speed", 0f, 16f, Order = 4, RequireRestart = false, HintText = "{=BW_Game_GameSpeed_Hint}[WORK IN PROGRESS] Set campaign map time speed multiplier (0 = disabled).")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 4)]
        public float GameSpeed { get; set; } = 0f;

        #endregion

    }
}
