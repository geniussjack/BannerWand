// Third-party namespaces
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace BannerWand.Settings
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
        /// Private backing field for GameSpeed property.
        /// Used to ensure default value is 1.0, not 0.1 (minimum value from attribute).
        /// </summary>

        /// <summary>
        /// Flag to track if GameSpeed has been explicitly set by the user.
        /// Once set, the user can set any value (including below 1.0).
        /// </summary>
        private bool _gameSpeedUserSet = false;

        /// <summary>
        /// Backing field for GameSpeed property.
        /// </summary>
        private float _gameSpeed = 1.0f;

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

        #region Debug Category

        /// <summary>
        /// Enables detailed debug logging for troubleshooting and development.
        /// When enabled, detailed logs are written for all cheat operations.
        /// When disabled, only initialization and error logs are written.
        /// </summary>
        [SettingPropertyBool("{=BW_Debug_DebugMode}Debug Mode", Order = 0, RequireRestart = false, HintText = "{=BW_Debug_DebugMode_Hint}Enables detailed debug logging for all cheat operations. Disable to reduce log file size.")]
        [SettingPropertyGroup("{=BW_Category_Debug}Debug", GroupOrder = 7)]
        public bool DebugMode { get; set; } = false;

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
        [SettingPropertyBool("{=BW_Player_InfiniteHealth}Infinite HP", Order = 1, RequireRestart = false, HintText = "{=BW_Player_InfiniteHealth_Hint}Adds +9999 HP at battle start. Prevents one-shot kills.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool InfiniteHealth { get; set; } = false;

        /// <summary>
        /// Player's mount never loses health.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedHorseHealth}Unlimited Horse HP", Order = 2, RequireRestart = false, HintText = "{=BW_Player_UnlimitedHorseHealth_Hint}Player's horse takes no damage.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool UnlimitedHorseHealth { get; set; } = false;

        /// <summary>
        /// Player's shield never breaks.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedShieldDurability}Unlimited Shield HP", Order = 3, RequireRestart = false, HintText = "{=BW_Player_UnlimitedShieldDurability_Hint}Takes effect when you block enemy attacks.")]
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
        [SettingPropertyBool("{=BW_Player_MaxMorale}Max Morale", Order = 5, RequireRestart = false, HintText = "{=BW_Player_MaxMorale_Hint}Player's party always has maximum morale.")]
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
        /// Additional companion limit bonus for player and NPC clans (0-100).
        /// Implemented in <see cref="Models.CustomClanTierModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Player_ClanCompanionsLimit}Clan Companions Limit", 0, 100, Order = 6, RequireRestart = false, HintText = "{=BW_Player_ClanCompanionsLimit_Hint}Adds bonus to companion limit for player and NPC clans. Works with mods that allow NPCs to recruit companions.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public int ClanCompanionsLimit { get; set; } = 0;

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

        #region NPC Category

        /// <summary>
        /// NPC heroes' health bar never decreases (based on max HP limit).
        /// Only applies to allied heroes fighting on player's side, not regular soldiers.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedHP}Unlimited HP", Order = 0, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedHP_Hint}Allied NPC heroes' health bar never decreases. Only applies to heroes fighting on player's side, not regular soldiers.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public bool NPCUnlimitedHP { get; set; } = false;

        /// <summary>
        /// Adds +9999 health to NPC heroes at the start of each battle.
        /// Only applies to allied heroes fighting on player's side, not regular soldiers.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_InfiniteHP}Infinite HP", Order = 1, RequireRestart = false, HintText = "{=BW_NPC_InfiniteHP_Hint}Adds +9999 HP to allied NPC heroes at battle start. Only applies to heroes fighting on player's side, not regular soldiers.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public bool NPCInfiniteHP { get; set; } = false;

        /// <summary>
        /// NPC heroes' mounts never lose health.
        /// Only applies to allied heroes fighting on player's side, not regular soldiers.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedHorseHP}Unlimited Horse HP", Order = 2, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedHorseHP_Hint}Allied NPC heroes' horses take no damage. Only applies to heroes fighting on player's side, not regular soldiers.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public bool NPCUnlimitedHorseHP { get; set; } = false;

        /// <summary>
        /// NPC heroes' shields never lose durability.
        /// Only applies to allied heroes fighting on player's side, not regular soldiers.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedShieldHP}Unlimited Shield HP", Order = 3, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedShieldHP_Hint}Allied NPC heroes' shields take no damage. Only applies to heroes fighting on player's side, not regular soldiers.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public bool NPCUnlimitedShieldHP { get; set; } = false;

        /// <summary>
        /// NPC heroes never run out of ammunition for ranged weapons.
        /// Only applies to allied heroes fighting on player's side, not regular soldiers.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedAmmo}Unlimited Ammo", Order = 5, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedAmmo_Hint}Allied NPC heroes' ammunition is maintained at max. Only applies to heroes fighting on player's side, not regular soldiers.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public bool NPCUnlimitedAmmo { get; set; } = false;

        /// <summary>
        /// Campaign map movement speed multiplier for all parties on the map.
        /// Implemented in <see cref="Models.CustomPartySpeedModel"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_NPC_MovementSpeed}Set Movement Speed", 0f, 16f, Order = 6, RequireRestart = false, HintText = "{=BW_NPC_MovementSpeed_Hint}Applies to all parties on the map, not just player. Changes apply on the next in-game day.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public float NPCMovementSpeed { get; set; } = 0f;

        /// <summary>
        /// Add or remove gold for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditGold}Edit Gold", -1000000, 1000000, Order = 7, RequireRestart = false, HintText = "{=BW_NPC_EditGold_Hint}Add or remove gold for NPCs (applied once when value changed).")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public int NPCEditGold { get; set; } = 0;

        /// <summary>
        /// Add or remove clan influence for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditInfluence}Edit Influence", -10000, 10000, Order = 8, RequireRestart = false, HintText = "{=BW_NPC_EditInfluence_Hint}Add or remove influence for NPCs (applied once when value changed).")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public int NPCEditInfluence { get; set; } = 0;

        /// <summary>
        /// Add or remove unspent attribute points for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditAttributePoints}Edit Attribute Points", -1000, 1000, Order = 9, RequireRestart = false, HintText = "{=BW_NPC_EditAttributePoints_Hint}Add or remove attribute points for NPCs (applied once when value changed).")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public int NPCEditAttributePoints { get; set; } = 0;

        /// <summary>
        /// Add or remove unspent focus points for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditFocusPoints}Edit Focus Points", -1000, 1000, Order = 10, RequireRestart = false, HintText = "{=BW_NPC_EditFocusPoints_Hint}Add or remove focus points for NPCs (applied once when value changed).")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 4)]
        public int NPCEditFocusPoints { get; set; } = 0;

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
        /// Prevents items from being removed from player's inventory during barter/trade.
        /// Implemented via <see cref="Patches.ItemBarterablePatch"/> Harmony patch.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_TradeItemsNoDecrease}Trade/Exchange Items Don't Decrease", Order = 3, RequireRestart = false, HintText = "{=BW_Inventory_TradeItemsNoDecrease_Hint}Items stay in your inventory after trading them. Only applies to items you give away, not items you receive.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public bool TradeItemsNoDecrease { get; set; } = false;

        /// <summary>
        /// Sets carrying capacity to ~1 million (effectively unlimited).
        /// Implemented via <see cref="Patches.InventoryCapacityPatch"/> (Harmony patch).
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
        /// Target quantity for unlimited smithy materials.
        /// Controls how many of each material to maintain when Unlimited Smithy Materials is enabled.
        /// </summary>
        [SettingPropertyInteger("{=BW_Inventory_SmithyMaterialsQuantity}Smithy Materials Quantity", 100, 9999, Order = 6, RequireRestart = false, HintText = "{=BW_Inventory_SmithyMaterialsQuantity_Hint}Set the target amount for smithy materials (100-9999). Only works when Unlimited Smithy Materials is enabled.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 1)]
        public int SmithyMaterialsQuantity { get; set; } = 9999;


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
        /// Multiplies all renown gains by the specified multiplier.
        /// Implemented via <see cref="Patches.RenownMultiplierPatch"/> Harmony patch.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Stats_RenownMultiplier}Renown Multiplier", 0f, 16f, Order = 3, RequireRestart = false, HintText = "{=BW_Stats_RenownMultiplier_Hint}Multiplies all renown gains (0 = disabled). Note: The display value may not reflect the multiplied amount, but the actual renown gained is multiplied.")]
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
        /// All enemy combatants die from a single hit.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Enemies_OneHitKills}One-Hit Kills", Order = 1, RequireRestart = false, HintText = "{=BW_Enemies_OneHitKills_Hint}All enemy units die in one hit.")]
        [SettingPropertyGroup("{=BW_Category_Enemies}Enemies", GroupOrder = 3)]
        public bool OneHitKills { get; set; } = false;

        #endregion

        #region Game Category


        /// <summary>
        /// All persuasion attempts automatically succeed.
        /// Implemented in <see cref="Models.CustomPersuasionModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_PersuasionAlwaysSucceed}Persuasion/Conversation Always Succeed", Order = 1, RequireRestart = false, HintText = "{=BW_Game_PersuasionAlwaysSucceed_Hint}All persuasion and conversation checks automatically succeed.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 6)]
        public bool PersuasionAlwaysSucceed { get; set; } = false;


        /// <summary>
        /// Siege equipment builds instantly for player.
        /// Implemented in <see cref="Models.CustomSiegeEventModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_InstantSiegeConstruction}Instant Siege Construction", Order = 3, RequireRestart = false, HintText = "{=BW_Game_InstantSiegeConstruction_Hint}Note this option affects both sides.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 6)]
        public bool InstantSiegeConstruction { get; set; } = false;

        /// <summary>
        /// Game speed multiplier for Play and Fast Forward buttons.
        /// Multiplies the speed provided by Play (1x) and Fast Forward (4x) buttons.
        /// Example: 2.0 means Play becomes 2x speed, Fast Forward becomes 8x speed.
        /// </summary>
        /// <remarks>
        /// Default value is enforced to be 1.0, not 0.1 (minimum value from attribute).
        /// This prevents MCM from using the minimum value (0.1f) as default.
        /// </remarks>
        [SettingPropertyFloatingInteger("{=BW_Game_GameSpeed}Set Game Speed Multiplier", 0.1f, 10f, Order = 4, RequireRestart = false, HintText = "{=BW_Game_GameSpeed_Hint}Multiplies the speed of Play (1x) and Fast Forward (4x) buttons. 1.0 = normal, 2.0 = double speed, etc.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 6)]
        public float GameSpeed
        {
            get
            {
                // On first access, ensure default value is 1.0, not 0.1 (minimum value from attribute)
                // This fixes the issue where MCM might use the minimum value (0.1f) as default
                // Only apply this fix if the user hasn't explicitly set a value yet
                if (!_gameSpeedUserSet && (_gameSpeed <= 0.1f || _gameSpeed == 0f))
                {
                    _gameSpeed = 1.0f;
                }
                return _gameSpeed;
            }
            set
            {
                // Mark that the user has explicitly set a value
                _gameSpeedUserSet = true;

                // Allow any value within the attribute range (0.1f to 10f)
                // User can now set any value they want, including below 1.0
                _gameSpeed = value;
            }
        }

        #endregion

        #region Settlements Category

        /// <summary>
        /// Additive bonus for daily garrison recruitment (0-999).
        /// Implemented in <see cref="Models.CustomSettlementGarrisonModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_GarrisonRecruitmentMultiplier}Garrison Recruitment Bonus", 0, 999, Order = 1, RequireRestart = false, HintText = "{=BW_Settlements_GarrisonRecruitmentMultiplier_Hint}Adds soldiers to garrison each day. 0 = disabled, 10 = adds 10 soldiers per day, etc.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public int GarrisonRecruitmentMultiplier { get; set; } = 0;

        /// <summary>
        /// Multiplier for garrison wages (1.0 = normal, 0 = free, greater than 1.0 = increase wages).
        /// Implemented in <see cref="Models.CustomPartyWageModel"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Settlements_GarrisonWagesMultiplier}Garrison Wages Multiplier", 0f, 10f, Order = 2, RequireRestart = false, HintText = "{=BW_Settlements_GarrisonWagesMultiplier_Hint}Controls garrison wage cost. 1.0 = normal, 0 = free, greater than 1.0 = increase wages.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public float GarrisonWagesMultiplier { get; set; } = 1f;

        /// <summary>
        /// Additive bonus for daily militia recruitment (0-999).
        /// Implemented in <see cref="Models.CustomSettlementMilitiaModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_MilitiaRecruitmentMultiplier}Militia Recruitment Bonus", 0, 999, Order = 3, RequireRestart = false, HintText = "{=BW_Settlements_MilitiaRecruitmentMultiplier_Hint}Adds militiamen each day. 0 = disabled, 10 = adds 10 militiamen per day, etc.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public int MilitiaRecruitmentMultiplier { get; set; } = 0;

        /// <summary>
        /// Chance percentage for veteran militiamen to appear.
        /// Implemented in <see cref="Models.CustomSettlementMilitiaModel"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Settlements_MilitiaVeteranChance}Militia Veteran Chance", 0f, 100f, Order = 4, RequireRestart = false, HintText = "{=BW_Settlements_MilitiaVeteranChance_Hint}Percentage chance for veteran militiamen to appear (0-100%).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public float MilitiaVeteranChance { get; set; } = 0f;

        /// <summary>
        /// Numerical addition to daily food growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementFoodModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_FoodIncreaseMultiplier}Food Increase Multiplier", 0, 999, Order = 5, RequireRestart = false, HintText = "{=BW_Settlements_FoodIncreaseMultiplier_Hint}Adds the specified value to daily food growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public int FoodIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily prosperity growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementProsperityModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_ProsperityIncreaseMultiplier}Prosperity Increase Multiplier", 0, 999, Order = 6, RequireRestart = false, HintText = "{=BW_Settlements_ProsperityIncreaseMultiplier_Hint}Adds the specified value to daily prosperity growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public int ProsperityIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily hearth growth for villages (0-999).
        /// Implemented in <see cref="Models.CustomSettlementProsperityModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_HearthIncreaseMultiplier}Hearth Increase Multiplier", 0, 999, Order = 7, RequireRestart = false, HintText = "{=BW_Settlements_HearthIncreaseMultiplier_Hint}Adds the specified value to daily hearth growth for villages (0-999). Hearth is the village equivalent of prosperity.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public int HearthIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily loyalty growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementLoyaltyModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_LoyaltyIncreaseMultiplier}Loyalty Increase Multiplier", 0, 999, Order = 8, RequireRestart = false, HintText = "{=BW_Settlements_LoyaltyIncreaseMultiplier_Hint}Adds the specified value to daily loyalty growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public int LoyaltyIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily security growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementSecurityModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_SecurityIncreaseMultiplier}Security Increase Multiplier", 0, 999, Order = 9, RequireRestart = false, HintText = "{=BW_Settlements_SecurityIncreaseMultiplier_Hint}Adds the specified value to daily security growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public int SecurityIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// All settlement buildings complete construction in one day.
        /// Implemented in <see cref="Models.CustomBuildingConstructionModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_OneDaySettlementsConstruction}One Day Settlements Construction", Order = 10, RequireRestart = false, HintText = "{=BW_Game_OneDaySettlementsConstruction_Hint}Only one building can be constructed at a time. Building/upgrading multiple buildings still requires multiple days.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 5)]
        public bool OneDaySettlementsConstruction { get; set; } = false;

        #endregion

    }
}
