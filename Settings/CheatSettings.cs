#nullable enable
using BannerWand.Utils;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace BannerWand.Settings
{
    /// <summary>
    /// Main settings class for BannerWand cheat mod, managed by Mod Configuration Menu (MCM).
    /// Contains every cheat toggle, value, and target selection that players can configure in-game,
    /// all on a single MCM page.
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
    /// - NPC: The same combat and stat cheats, applied to allied heroes/troops
    /// - Smithy: Smithing stamina, materials, and crafting unlocks
    /// - Social: Barter, relationships, and persuasion
    /// - Inventory: Gold, items, carrying capacity
    /// - Stats: Skills, XP, renown, character development
    /// - Enemies: AI modifications
    /// - Settlements: Garrison, militia, food, prosperity, loyalty, security, construction speed
    /// - Game: Time, persuasion, construction speed
    /// - Target categories: Which heroes NPC-facing cheats above apply to
    /// </para>
    /// </remarks>
    public class CheatSettings : AttributeGlobalSettings<CheatSettings>
    {
        #region MCM Configuration

        /// <summary>
        /// Flag to track if GameSpeed has been explicitly set by the user.
        /// Once set, the user can set any value (including below 1.0).
        /// </summary>
        private bool _gameSpeedUserSet = false;

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
        [SettingPropertyFloatingInteger("{=BW_Player_MovementSpeed}Set Movement Speed", 0f, 16f, Order = 6, RequireRestart = false, HintText = "{=BW_Player_MovementSpeed_Hint}Only works on map, not in battle. Changes apply on the next in-game day.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public float MovementSpeed { get; set; } = 0f;

        /// <summary>
        /// Additional companion limit bonus for player and NPC clans (0-100).
        /// Implemented in <see cref="Models.CustomClanTierModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Player_ClanCompanionsLimit}Clan Companions Limit", 0, 100, Order = 7, RequireRestart = false, HintText = "{=BW_Player_ClanCompanionsLimit_Hint}Adds bonus to companion limit for player and NPC clans. Works with mods that allow NPCs to recruit companions.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public int ClanCompanionsLimit { get; set; } = 0;

        /// <summary>
        /// Additional party size limit bonus for player's party (0-500).
        /// Implemented in <see cref="Models.CustomPartyModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Player_PartySizeLimit}Party Size Limit Bonus", 0, 500, Order = 8, RequireRestart = false, HintText = "{=BW_Player_PartySizeLimit_Hint}Adds bonus to party size limit for player's party only. Does not affect clan parties or NPC parties.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public int PartySizeLimit { get; set; } = 0;

        /// <summary>
        /// Freezes the player character's age at whatever it is when the cheat is enabled.
        /// Implemented in <see cref="Patches.AgingPatch"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_StopPlayerAging}Stop Player Aging", Order = 9, RequireRestart = false, HintText = "{=BW_Player_StopPlayerAging_Hint}Prevents player character from aging.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool StopPlayerAging { get; set; } = false;

        /// <summary>
        /// Player's ships never take damage (War Sails DLC).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_InfiniteShipHealth}Infinite Ship HP", Order = 10, RequireRestart = false, HintText = "{=BW_Player_InfiniteShipHealth_Hint}Ships owned by the player's party never take damage. Requires War Sails DLC.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool InfiniteShipHealth { get; set; } = false;

        /// <summary>
        /// Whenever the player picks a perk that has an alternative, the alternative is granted
        /// too - no save reload needed. Implemented in <see cref="Patches.BothPerksPatch"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_AllowBothPerks}Allow Both Perks", Order = 11, RequireRestart = false, HintText = "{=BW_Player_AllowBothPerks_Hint}Grants the alternative perk automatically whenever you pick one from a pair, so you end up with both - takes effect immediately, no save reload needed.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool AllowBothPerks { get; set; } = false;

        #endregion

        #region NPC Category

        /// <summary>
        /// NPC heroes' health bar never decreases (based on max HP limit).
        /// Applies to allied heroes fighting on player's side; also covers regular soldiers if
        /// <see cref="NPCApplyToRegularTroops"/> is enabled.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedHP}Unlimited HP", Order = 0, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedHP_Hint}Allied NPC heroes' health bar never decreases. Only applies to heroes fighting on player's side unless Also Apply to Regular Soldiers is enabled below.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public bool NPCUnlimitedHP { get; set; } = false;

        /// <summary>
        /// Adds +9999 health to NPC heroes at the start of each battle.
        /// Applies to allied heroes fighting on player's side; also covers regular soldiers if
        /// <see cref="NPCApplyToRegularTroops"/> is enabled.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_InfiniteHP}Infinite HP", Order = 1, RequireRestart = false, HintText = "{=BW_NPC_InfiniteHP_Hint}Adds +9999 HP to allied NPC heroes at battle start. Only applies to heroes fighting on player's side unless Also Apply to Regular Soldiers is enabled below.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public bool NPCInfiniteHP { get; set; } = false;

        /// <summary>
        /// NPC heroes' mounts never lose health.
        /// Applies to allied heroes fighting on player's side; also covers regular soldiers if
        /// <see cref="NPCApplyToRegularTroops"/> is enabled.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedHorseHP}Unlimited Horse HP", Order = 2, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedHorseHP_Hint}Allied NPC heroes' horses take no damage. Only applies to heroes fighting on player's side unless Also Apply to Regular Soldiers is enabled below.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public bool NPCUnlimitedHorseHP { get; set; } = false;

        /// <summary>
        /// NPC heroes' shields never lose durability.
        /// Applies to allied heroes fighting on player's side; also covers regular soldiers if
        /// <see cref="NPCApplyToRegularTroops"/> is enabled.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedShieldHP}Unlimited Shield HP", Order = 3, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedShieldHP_Hint}Allied NPC heroes' shields take no damage. Only applies to heroes fighting on player's side unless Also Apply to Regular Soldiers is enabled below.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public bool NPCUnlimitedShieldHP { get; set; } = false;

        /// <summary>
        /// NPC heroes never run out of ammunition for ranged weapons.
        /// Applies to allied heroes fighting on player's side; also covers regular soldiers if
        /// <see cref="NPCApplyToRegularTroops"/> is enabled.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_UnlimitedAmmo}Unlimited Ammo", Order = 4, RequireRestart = false, HintText = "{=BW_NPC_UnlimitedAmmo_Hint}Allied NPC heroes' ammunition is maintained at max. Only applies to heroes fighting on player's side unless Also Apply to Regular Soldiers is enabled below.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public bool NPCUnlimitedAmmo { get; set; } = false;

        /// <summary>
        /// Campaign map movement speed multiplier for all parties on the map.
        /// Implemented in <see cref="Models.CustomPartySpeedModel"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_NPC_MovementSpeed}Set Movement Speed", 0f, 16f, Order = 5, RequireRestart = false, HintText = "{=BW_NPC_MovementSpeed_Hint}Applies to all parties on the map, not just player. Changes apply on the next in-game day.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public float NPCMovementSpeed { get; set; } = 0f;

        /// <summary>
        /// Add or remove gold for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditGold}Edit Gold", -1000000, 1000000, Order = 6, RequireRestart = false, HintText = "{=BW_NPC_EditGold_Hint}Add or remove gold for NPCs. Applied once when the value changes.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public int NPCEditGold { get; set; } = 0;

        /// <summary>
        /// Add or remove clan influence for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditInfluence}Edit Influence", -10000, 10000, Order = 7, RequireRestart = false, HintText = "{=BW_NPC_EditInfluence_Hint}Add or remove influence for NPCs. Applied once when the value changes.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public int NPCEditInfluence { get; set; } = 0;

        /// <summary>
        /// Add or remove unspent attribute points for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditAttributePoints}Edit Attribute Points", -1000, 1000, Order = 8, RequireRestart = false, HintText = "{=BW_NPC_EditAttributePoints_Hint}Add or remove attribute points for NPCs. Applied once when the value changes.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public int NPCEditAttributePoints { get; set; } = 0;

        /// <summary>
        /// Add or remove unspent focus points for NPCs (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_NPC_EditFocusPoints}Edit Focus Points", -1000, 1000, Order = 9, RequireRestart = false, HintText = "{=BW_NPC_EditFocusPoints_Hint}Add or remove focus points for NPCs. Applied once when the value changes.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public int NPCEditFocusPoints { get; set; } = 0;

        /// <summary>
        /// Freezes every NPC hero's age at whatever it is when the cheat is enabled, once they
        /// reach <see cref="Constants.GameConstants.MinimumAgeForStopAging"/>. Applies campaign-wide,
        /// not just to heroes fighting alongside the player.
        /// Implemented in <see cref="Patches.AgingPatch"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_StopNPCAging}Stop NPC Aging", Order = 10, RequireRestart = false, HintText = "{=BW_NPC_StopNPCAging_Hint}Prevents NPCs from aging once they reach 21 years old. Children under 21 will continue to grow normally.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public bool StopNPCAging { get; set; } = false;

        /// <summary>
        /// Widens Unlimited HP, Infinite HP, Unlimited Horse HP, Unlimited Shield HP, and
        /// Unlimited Ammo above (whichever are enabled) to also cover regular allied soldiers
        /// fighting on player's side, not just heroes.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/> and
        /// <see cref="Behaviors.Handlers.NPCCheatHandler"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_NPC_ApplyToRegularTroops}Also Apply to Regular Soldiers", Order = 11, RequireRestart = false, HintText = "{=BW_NPC_ApplyToRegularTroops_Hint}Extends the enabled NPC cheats above from allied heroes only to every allied soldier fighting on player's side. Can noticeably slow down large battles and make them drag on, since every friendly unit becomes hard to kill.")]
        [SettingPropertyGroup("{=BW_Category_NPC}NPC", GroupOrder = 1)]
        public bool NPCApplyToRegularTroops { get; set; } = false;

        #endregion

        #region Smithy Category

        /// <summary>
        /// Smithing and smelting actions cost no stamina.
        /// Implemented in <see cref="Models.CustomSmithingModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlimitedSmithyStamina}Unlimited Smithy Stamina", Order = 0, RequireRestart = false, HintText = "{=BW_Player_UnlimitedSmithyStamina_Hint}Smithing never consumes stamina.")]
        [SettingPropertyGroup("{=BW_Category_Smithy}Smithy", GroupOrder = 2)]
        public bool UnlimitedSmithyStamina { get; set; } = false;

        /// <summary>
        /// Unlocks every crafting piece for every weapon template in the Smithy screen, as if
        /// each one had already been discovered through smelting or completing crafting orders.
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_UnlockAllSmithyParts}Unlock All Smithy Parts", Order = 1, RequireRestart = false, HintText = "{=BW_Player_UnlockAllSmithyParts_Hint}Unlocks every crafting piece for every weapon type, as if you had already discovered them through smelting or crafting orders. Takes up to one in-game day to take effect after enabling.")]
        [SettingPropertyGroup("{=BW_Category_Smithy}Smithy", GroupOrder = 2)]
        public bool UnlockAllSmithyParts { get; set; } = false;

        /// <summary>
        /// Automatically replenishes iron and charcoal for smithing.
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_UnlimitedSmithyMaterials}Unlimited Smithy Materials", Order = 2, RequireRestart = false, HintText = "{=BW_Inventory_UnlimitedSmithyMaterials_Hint}Only works for materials that you already owned, it has no effect on materials that you didn't own.")]
        [SettingPropertyGroup("{=BW_Category_Smithy}Smithy", GroupOrder = 2)]
        public bool UnlimitedSmithyMaterials { get; set; } = false;

        /// <summary>
        /// Target quantity for unlimited smithy materials.
        /// Controls how many of each material to maintain when Unlimited Smithy Materials is enabled.
        /// </summary>
        [SettingPropertyInteger("{=BW_Inventory_SmithyMaterialsQuantity}Smithy Materials Quantity", 100, 9999, Order = 3, RequireRestart = false, HintText = "{=BW_Inventory_SmithyMaterialsQuantity_Hint}Set the target amount for smithy materials (100-9999). Only works when Unlimited Smithy Materials is enabled.")]
        [SettingPropertyGroup("{=BW_Category_Smithy}Smithy", GroupOrder = 2)]
        public int SmithyMaterialsQuantity { get; set; } = 9999;

        #endregion

        #region Social Category

        /// <summary>
        /// Any barter/trade offer is automatically accepted by NPCs.
        /// Implemented in <see cref="Models.CustomBarterModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_BarterAlwaysAccepted}Barter Offer Always Accepted", Order = 0, RequireRestart = false, HintText = "{=BW_Player_BarterAlwaysAccepted_Hint}All barter offers are automatically accepted.")]
        [SettingPropertyGroup("{=BW_Category_Social}Social", GroupOrder = 3)]
        public bool BarterAlwaysAccepted { get; set; } = false;

        /// <summary>
        /// Boosts any positive relationship change involving the player to +99.
        /// Implemented in <see cref="Patches.RelationshipBoostPatch"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_MaxCharacterRelationship}Max Character Relationship", Order = 1, RequireRestart = false, HintText = "{=BW_Player_MaxCharacterRelationship_Hint}Any positive relationship change with player is boosted to +99.")]
        [SettingPropertyGroup("{=BW_Category_Social}Social", GroupOrder = 3)]
        public bool MaxCharacterRelationship { get; set; } = false;

        /// <summary>
        /// Instantly maximizes player's relationship with ALL characters to 100.
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Player_MaxAllCharacterRelationships}Max All Character Relationships", Order = 2, RequireRestart = false, HintText = "{=BW_Player_MaxAllCharacterRelationships_Hint}Instantly sets relationship to 100 with all characters.")]
        [SettingPropertyGroup("{=BW_Category_Social}Social", GroupOrder = 3)]
        public bool MaxAllCharacterRelationships { get; set; } = false;

        /// <summary>
        /// All persuasion attempts automatically succeed.
        /// Implemented in <see cref="Models.CustomPersuasionModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_PersuasionAlwaysSucceed}Persuasion/Conversation Always Succeed", Order = 3, RequireRestart = false, HintText = "{=BW_Game_PersuasionAlwaysSucceed_Hint}All persuasion and conversation checks automatically succeed.")]
        [SettingPropertyGroup("{=BW_Category_Social}Social", GroupOrder = 3)]
        public bool PersuasionAlwaysSucceed { get; set; } = false;

        #endregion

        #region Inventory Category

        /// <summary>
        /// Add or remove gold (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Inventory_EditGold}Edit Gold", -1000000, 1000000, Order = 0, RequireRestart = false, HintText = "{=BW_Inventory_EditGold_Hint}Add or remove gold. Applied once when the value changes.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 4)]
        public int EditGold { get; set; } = 0;

        /// <summary>
        /// Add or remove clan influence (applies once when value changed).
        /// Implemented in <see cref="Behaviors.PlayerCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Inventory_EditInfluence}Edit Influence", -10000, 10000, Order = 1, RequireRestart = false, HintText = "{=BW_Inventory_EditInfluence_Hint}Add or remove influence. Applied once when the value changes.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 4)]
        public int EditInfluence { get; set; } = 0;

        /// <summary>
        /// Party never consumes food.
        /// Implemented in <see cref="Models.CustomMobilePartyFoodConsumptionModel"/> and <see cref="Behaviors.FoodCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_UnlimitedFood}Unlimited Food", Order = 2, RequireRestart = false, HintText = "{=BW_Inventory_UnlimitedFood_Hint}Party food never decreases.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 4)]
        public bool UnlimitedFood { get; set; } = false;

        /// <summary>
        /// Prevents items from being removed from player's inventory during barter/trade.
        /// Implemented via <see cref="Patches.ItemBarterablePatch"/> Harmony patch.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_TradeItemsNoDecrease}Trade/Exchange Items Don't Decrease", Order = 3, RequireRestart = false, HintText = "{=BW_Inventory_TradeItemsNoDecrease_Hint}Items stay in your inventory after trading them. Only applies to items you give away, not items you receive.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 4)]
        public bool TradeItemsNoDecrease { get; set; } = false;

        /// <summary>
        /// Sets carrying capacity to ~1 million (effectively unlimited).
        /// Implemented via <see cref="Patches.InventoryCapacityPatch"/> (Harmony patch).
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_MaxCarryingCapacity}Max Carrying Capacity", Order = 4, RequireRestart = false, HintText = "{=BW_Inventory_MaxCarryingCapacity_Hint}Party has unlimited carrying capacity.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 4)]
        public bool MaxCarryingCapacity { get; set; } = false;

        /// <summary>
        /// Enables a rebindable hotkey (Ctrl+X by default) that grants the amount set by
        /// <see cref="EditGold"/> whenever pressed on the campaign map, mirroring Mount &amp; Blade:
        /// Warband's cheat_mode hotkeys. Implemented in <see cref="Behaviors.HotkeyCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Inventory_EnableAddGoldHotkey}Enable Add Gold Hotkey", Order = 5, RequireRestart = false, HintText = "{=BW_Inventory_EnableAddGoldHotkey_Hint}Adds the amount set in Edit Gold above every time the hotkey is pressed. Default is Ctrl+X, rebindable in Options > Key Bindings. Only works on the campaign map, not in battle. Requires Edit Gold to be set to a non-zero value.")]
        [SettingPropertyGroup("{=BW_Category_Inventory}Inventory", GroupOrder = 4)]
        public bool EnableAddGoldHotkey { get; set; } = false;

        #endregion

        #region Stats Category

        /// <summary>
        /// Add or remove unspent attribute points (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Stats_EditAttributePoints}Edit Attribute Points", -1000, 1000, Order = 0, RequireRestart = false, HintText = "{=BW_Stats_EditAttributePoints_Hint}Takes effect when opening the character menu twice. To disable this option, you need to set the value to -1.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public int EditAttributePoints { get; set; } = 0;

        /// <summary>
        /// Add or remove unspent focus points (applies once when value changed).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Stats_EditFocusPoints}Edit Focus Points", -1000, 1000, Order = 1, RequireRestart = false, HintText = "{=BW_Stats_EditFocusPoints_Hint}Takes effect when opening the character menu twice. To disable this option, you need to set the value to -1.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public int EditFocusPoints { get; set; } = 0;

        /// <summary>
        /// Gradually increases clan renown to 10,000 (adds 999 renown/hour).
        /// Implemented in <see cref="Behaviors.NPCCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Stats_UnlimitedRenown}Unlimited Renown", Order = 2, RequireRestart = false, HintText = "{=BW_Stats_UnlimitedRenown_Hint}Note sometime you may not gain any renown even though the game says you gained renown. Especially when enemies are fled. This might be a game bug. Takes effect when gaining renown.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public bool UnlimitedRenown { get; set; } = false;

        /// <summary>
        /// Multiplies all renown gains by the specified multiplier.
        /// Implemented via <see cref="Patches.RenownMultiplierPatch"/> Harmony patch.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Stats_RenownMultiplier}Renown Multiplier", 0f, 16f, Order = 3, RequireRestart = false, HintText = "{=BW_Stats_RenownMultiplier_Hint}Multiplies all renown gains (0 = disabled). Note: The display value may not reflect the multiplied amount, but the actual renown gained is multiplied.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public float RenownMultiplier { get; set; } = 0f;

        /// <summary>
        /// Rapidly levels all skills to maximum (adds 999 XP/hour per skill).
        /// Implemented in <see cref="Behaviors.SkillXPCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Stats_UnlimitedSkillXP}Unlimited Skill XP", Order = 4, RequireRestart = false, HintText = "{=BW_Stats_UnlimitedSkillXP_Hint}Takes effect when a skill gains XP. Note skills have level caps based on their learning rate which affected by your attributes. If a skill's learning rate is 0, it won't gain any XP, you need to spend attribute points to raise your attributes in order to raise learning rate.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public bool UnlimitedSkillXP { get; set; } = false;

        /// <summary>
        /// Multiplies skill XP gains (1.0 = normal, 2.0 = double, etc.).
        /// Implemented in <see cref="Models.CustomGenericXpModel"/> (if available).
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Stats_SkillXPMultiplier}Skill XP Multiplier", 0f, 16f, Order = 5, RequireRestart = false, HintText = "{=BW_Stats_SkillXPMultiplier_Hint}Multiply skill XP gains (0 = disabled).")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public float SkillXPMultiplier { get; set; } = 0f;

        /// <summary>
        /// Rapidly levels all troops (adds 999 XP/hour per troop).
        /// Implemented in <see cref="Behaviors.SkillXPCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Stats_UnlimitedTroopsXP}Unlimited Troops XP", Order = 6, RequireRestart = false, HintText = "{=BW_Stats_UnlimitedTroopsXP_Hint}All troops gain XP without limit.")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public bool UnlimitedTroopsXP { get; set; } = false;

        /// <summary>
        /// Multiplies troop XP gains (1.0 = normal, 2.0 = double, etc.).
        /// Implemented in <see cref="Behaviors.SkillXPCheatBehavior"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Stats_TroopsXPMultiplier}Troops XP Multiplier", 0f, 16f, Order = 7, RequireRestart = false, HintText = "{=BW_Stats_TroopsXPMultiplier_Hint}Multiply troop XP gains (0 = disabled).")]
        [SettingPropertyGroup("{=BW_Category_Stats}Stats", GroupOrder = 5)]
        public float TroopsXPMultiplier { get; set; } = 0f;

        #endregion

        #region Enemies Category

        /// <summary>
        /// All enemy combatants die from a single hit.
        /// Implemented in <see cref="Behaviors.CombatCheatBehavior"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Enemies_OneHitKills}One-Hit Kills", Order = 0, RequireRestart = false, HintText = "{=BW_Enemies_OneHitKills_Hint}All enemy units die in one hit.")]
        [SettingPropertyGroup("{=BW_Category_Enemies}Enemies", GroupOrder = 6)]
        public bool OneHitKills { get; set; } = false;

        /// <summary>
        /// Troops die instead of being wounded/captured in every auto-resolved field battle on
        /// the map, not just battles the player personally fights.
        /// Implemented in <see cref="Models.CustomCombatSimulationModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Enemies_AllBattlesNoWounding}[WIP] All Battles No Wounding", Order = 1, RequireRestart = false, HintText = "{=BW_Enemies_AllBattlesNoWounding_Hint}Work in progress, does not fully work yet. Intended to make troops die instead of being wounded or captured in every auto-resolved field battle on the map, including battles you don't personally fight. Complements One-Hit Kills, which only affects battles you fight yourself. Does not affect sieges or captured heroes.")]
        [SettingPropertyGroup("{=BW_Category_Enemies}Enemies", GroupOrder = 6)]
        public bool AllBattlesNoWounding { get; set; } = false;

        #endregion

        #region Settlements Category

        /// <summary>
        /// Additive bonus for daily garrison recruitment (0-999).
        /// Implemented in <see cref="Models.CustomSettlementGarrisonModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_GarrisonRecruitmentMultiplier}Garrison Recruitment Bonus", 0, 999, Order = 0, RequireRestart = false, HintText = "{=BW_Settlements_GarrisonRecruitmentMultiplier_Hint}Adds soldiers to garrison each day. 0 = disabled, 10 = adds 10 soldiers per day, etc.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int GarrisonRecruitmentMultiplier { get; set; } = 0;

        /// <summary>
        /// Multiplier for garrison wages (1.0 = normal, 0 = free, greater than 1.0 = increase wages).
        /// Implemented in <see cref="Models.CustomPartyWageModel"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Settlements_GarrisonWagesMultiplier}Garrison Wages Multiplier", 0f, 10f, Order = 1, RequireRestart = false, HintText = "{=BW_Settlements_GarrisonWagesMultiplier_Hint}Controls garrison wage cost. 1.0 = normal, 0 = free, greater than 1.0 = increase wages.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public float GarrisonWagesMultiplier { get; set; } = 1f;

        /// <summary>
        /// Additive bonus for daily militia recruitment (0-999).
        /// Implemented in <see cref="Models.CustomSettlementMilitiaModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_MilitiaRecruitmentMultiplier}Militia Recruitment Bonus", 0, 999, Order = 2, RequireRestart = false, HintText = "{=BW_Settlements_MilitiaRecruitmentMultiplier_Hint}Adds militiamen each day. 0 = disabled, 10 = adds 10 militiamen per day, etc.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int MilitiaRecruitmentMultiplier { get; set; } = 0;

        /// <summary>
        /// Chance percentage for veteran militiamen to appear.
        /// Implemented in <see cref="Models.CustomSettlementMilitiaModel"/>.
        /// </summary>
        [SettingPropertyFloatingInteger("{=BW_Settlements_MilitiaVeteranChance}Militia Veteran Chance", 0f, 100f, Order = 3, RequireRestart = false, HintText = "{=BW_Settlements_MilitiaVeteranChance_Hint}Percentage chance for veteran militiamen to appear (0-100%).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public float MilitiaVeteranChance { get; set; } = 0f;

        /// <summary>
        /// Numerical addition to daily food growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementFoodModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_FoodIncreaseMultiplier}Food Increase Multiplier", 0, 999, Order = 4, RequireRestart = false, HintText = "{=BW_Settlements_FoodIncreaseMultiplier_Hint}Adds the specified value to daily food growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int FoodIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily prosperity growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementProsperityModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_ProsperityIncreaseMultiplier}Prosperity Increase Multiplier", 0, 999, Order = 5, RequireRestart = false, HintText = "{=BW_Settlements_ProsperityIncreaseMultiplier_Hint}Adds the specified value to daily prosperity growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int ProsperityIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily hearth growth for villages (0-999).
        /// Implemented in <see cref="Models.CustomSettlementProsperityModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_HearthIncreaseMultiplier}Hearth Increase Multiplier", 0, 999, Order = 6, RequireRestart = false, HintText = "{=BW_Settlements_HearthIncreaseMultiplier_Hint}Adds the specified value to daily hearth growth for villages (0-999). Hearth is the village equivalent of prosperity.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int HearthIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily loyalty growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementLoyaltyModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_LoyaltyIncreaseMultiplier}Loyalty Increase Multiplier", 0, 999, Order = 7, RequireRestart = false, HintText = "{=BW_Settlements_LoyaltyIncreaseMultiplier_Hint}Adds the specified value to daily loyalty growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int LoyaltyIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// Numerical addition to daily security growth (0-999).
        /// Implemented in <see cref="Models.CustomSettlementSecurityModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_SecurityIncreaseMultiplier}Security Increase Multiplier", 0, 999, Order = 8, RequireRestart = false, HintText = "{=BW_Settlements_SecurityIncreaseMultiplier_Hint}Adds the specified value to daily security growth (0-999).")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int SecurityIncreaseMultiplier { get; set; } = 0;

        /// <summary>
        /// All settlement buildings complete construction in one day.
        /// Implemented in <see cref="Models.CustomBuildingConstructionModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_OneDaySettlementsConstruction}One Day Settlements Construction", Order = 9, RequireRestart = false, HintText = "{=BW_Game_OneDaySettlementsConstruction_Hint}Only one building can be constructed at a time. Building/upgrading multiple buildings still requires multiple days.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public bool OneDaySettlementsConstruction { get; set; } = false;

        /// <summary>
        /// Additive bonus to the maximum number of troops a garrison can hold (0-999).
        /// Implemented in <see cref="Models.CustomPartyLimitModel"/>.
        /// </summary>
        [SettingPropertyInteger("{=BW_Settlements_GarrisonCapacity}Garrison Capacity Bonus", 0, 999, Order = 10, RequireRestart = false, HintText = "{=BW_Settlements_GarrisonCapacity_Hint}Adds to the maximum number of troops a garrison can hold. 0 = disabled.")]
        [SettingPropertyGroup("{=BW_Category_Settlements}Settlements", GroupOrder = 7)]
        public int GarrisonCapacity { get; set; } = 0;

        #endregion

        #region Game Category

        /// <summary>
        /// Siege equipment builds instantly for player.
        /// Implemented in <see cref="Models.CustomSiegeEventModel"/>.
        /// </summary>
        [SettingPropertyBool("{=BW_Game_InstantSiegeConstruction}Instant Siege Construction", Order = 0, RequireRestart = false, HintText = "{=BW_Game_InstantSiegeConstruction_Hint}Note this option affects both sides.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 8)]
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
        [SettingPropertyFloatingInteger("{=BW_Game_GameSpeed}Set Game Speed Multiplier", 0.1f, 10f, Order = 1, RequireRestart = false, HintText = "{=BW_Game_GameSpeed_Hint}Multiplies the speed of Play (1x) and Fast Forward (4x) buttons. 1.0 = normal, 2.0 = double speed, etc.")]
        [SettingPropertyGroup("{=BW_Category_Game}Game", GroupOrder = 8)]
        public float GameSpeed
        {
            get
            {
                // On first access, ensure default value is 1.0, not 0.1 (minimum value from attribute)
                // This fixes the issue where MCM might use the minimum value (0.1f) as default
                // Only apply this fix if the user hasn't explicitly set a value yet
                if (!_gameSpeedUserSet && (field <= 0.1f || field == 0f))
                {
                    field = 1.0f;
                }
                return field;
            }
            set
            {
                // Mark that the user has explicitly set a value
                _gameSpeedUserSet = true;

                // Allow any value within the attribute range (0.1f to 10f)
                // User can now set any value they want, including below 1.0
                field = value;
            }
        } = 1.0f;

        #endregion

        #region Target: Player & Clan Category

        /// <summary>
        /// Apply cheats to the player character (the main hero).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayer}Apply to Player", Order = 0, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayer_Hint}Applies selected cheats to the player character.")]
        [SettingPropertyGroup("{=BW_Category_TargetPlayer}Target: Player & Clan", GroupOrder = 9)]
        public bool ApplyToPlayer { get; set; } = false;

        /// <summary>
        /// Apply cheats to all members of the player's clan (companions, family).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerClanMembers}Apply to Player's Clan Members", Order = 1, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerClanMembers_Hint}Applies selected cheats to all members of the player's clan.")]
        [SettingPropertyGroup("{=BW_Category_TargetPlayer}Target: Player & Clan", GroupOrder = 9)]
        public bool ApplyToPlayerClanMembers { get; set; } = false;

        #endregion

        #region Target: Kingdoms Category

        /// <summary>
        /// Apply cheats to all kingdom rulers (kings/queens of all kingdoms).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToKingdomRulers}Apply to Kingdom Rulers", Order = 0, RequireRestart = false, HintText = "{=BW_Target_ApplyToKingdomRulers_Hint}Applies selected cheats to rulers of all kingdoms.")]
        [SettingPropertyGroup("{=BW_Category_TargetKingdoms}Target: Kingdoms", GroupOrder = 10)]
        public bool ApplyToKingdomRulers { get; set; } = false;

        /// <summary>
        /// Apply cheats to the ruler of the player's current kingdom (if player is a vassal or the ruler).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerKingdomRuler}Apply to Player Kingdom Ruler", Order = 1, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerKingdomRuler_Hint}Applies cheats to the ruler of the player's current kingdom.")]
        [SettingPropertyGroup("{=BW_Category_TargetKingdoms}Target: Kingdoms", GroupOrder = 10)]
        public bool ApplyToPlayerKingdomRuler { get; set; } = false;

        /// <summary>
        /// Apply cheats to all vassal clan leaders of any kingdom.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToKingdomVassals}Apply to Kingdom Vassals", Order = 2, RequireRestart = false, HintText = "{=BW_Target_ApplyToKingdomVassals_Hint}Applies selected cheats to all vassal clan leaders of any kingdom.")]
        [SettingPropertyGroup("{=BW_Category_TargetKingdoms}Target: Kingdoms", GroupOrder = 10)]
        public bool ApplyToKingdomVassals { get; set; } = false;

        /// <summary>
        /// Apply cheats to all vassal clan leaders of the player's kingdom.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerKingdomVassals}Apply to Player Kingdom Vassals", Order = 3, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerKingdomVassals_Hint}Applies selected cheats to all vassal clan leaders of the player's kingdom.")]
        [SettingPropertyGroup("{=BW_Category_TargetKingdoms}Target: Kingdoms", GroupOrder = 10)]
        public bool ApplyToPlayerKingdomVassals { get; set; } = false;

        /// <summary>
        /// Apply cheats to all noble heroes of any kingdom (including clan members).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToKingdomNobles}Apply to Kingdom Nobles", Order = 4, RequireRestart = false, HintText = "{=BW_Target_ApplyToKingdomNobles_Hint}Applies selected cheats to all noble heroes of any kingdom.")]
        [SettingPropertyGroup("{=BW_Category_TargetKingdoms}Target: Kingdoms", GroupOrder = 10)]
        public bool ApplyToKingdomNobles { get; set; } = false;

        /// <summary>
        /// Apply cheats to all noble heroes of the player's kingdom.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerKingdomNobles}Apply to Player Kingdom Nobles", Order = 5, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerKingdomNobles_Hint}Applies selected cheats to all noble heroes of the player's kingdom.")]
        [SettingPropertyGroup("{=BW_Category_TargetKingdoms}Target: Kingdoms", GroupOrder = 10)]
        public bool ApplyToPlayerKingdomNobles { get; set; } = false;

        #endregion

        #region Target: Minor Clans Category

        /// <summary>
        /// Apply cheats to leaders of all minor factions (clans listed in Encyclopedia → Clans → Minor).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToMinorClanLeaders}Apply to Minor Clan Leaders", Order = 0, RequireRestart = false, HintText = "{=BW_Target_ApplyToMinorClanLeaders_Hint}Applies selected cheats to leaders of all minor factions.")]
        [SettingPropertyGroup("{=BW_Category_TargetMinorClans}Target: Minor Clans", GroupOrder = 11)]
        public bool ApplyToMinorClanLeaders { get; set; } = false;

        /// <summary>
        /// Apply cheats to all noble heroes from minor clans.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToMinorClanMembers}Apply to Minor Clan Members", Order = 1, RequireRestart = false, HintText = "{=BW_Target_ApplyToMinorClanMembers_Hint}Applies selected cheats to all noble heroes from minor clans.")]
        [SettingPropertyGroup("{=BW_Category_TargetMinorClans}Target: Minor Clans", GroupOrder = 11)]
        public bool ApplyToMinorClanMembers { get; set; } = false;

        #endregion

        #region Target: Info Category

        /// <summary>
        /// Static informational text displayed at the bottom of the MCM page.
        /// The group name itself serves as the informational message.
        /// This is a placeholder property to ensure the group is displayed.
        /// </summary>
        [SettingPropertyGroup("{=BW_Target_InfoText}Multiple options can be combined. Duplicate applications are automatically prevented.", GroupOrder = 12)]
        public bool InfoTextPlaceholder { get; set; } = false;

        #endregion

        #region Debug Category

        /// <summary>
        /// Enables detailed debug logging for troubleshooting and development.
        /// When enabled, detailed logs are written for all cheat operations.
        /// When disabled, only initialization and error logs are written.
        /// </summary>
        [SettingPropertyBool("{=BW_Debug_DebugMode}Debug Mode", Order = 0, RequireRestart = false, HintText = "{=BW_Debug_DebugMode_Hint}Enables detailed debug logging for all cheat operations. Disable to reduce log file size.")]
        [SettingPropertyGroup("{=BW_Category_Debug}Debug", GroupOrder = 13)]
        public bool DebugMode { get; set; } = false;

        #endregion

        #region Target Helper Methods

        /// <summary>
        /// Collects all target heroes based on current settings into a HashSet to prevent duplicates.
        /// </summary>
        /// <returns>A HashSet containing all heroes that should receive cheat effects.</returns>
        /// <remarks>
        /// <para>
        /// This method iterates through all relevant game entities and collects heroes
        /// based on enabled target options. Uses HashSet to automatically prevent duplicates
        /// (e.g., when player is also the kingdom ruler).
        /// </para>
        /// <para>
        /// Performance: O(n) where n is the number of heroes/clans/kingdoms in the game.
        /// Should be called once per cheat application, not every frame.
        /// </para>
        /// <para>
        /// All null checks are performed to prevent crashes when game state is invalid
        /// (e.g., player not in a kingdom, clan without leader, etc.).
        /// </para>
        /// </remarks>
        public HashSet<Hero> CollectTargetHeroes()
        {
            HashSet<Hero> targets = [];

            try
            {
                // Cache player kingdom once to avoid repeated lookups
                Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;

                // Category: Player
                if (ApplyToPlayer && Hero.MainHero != null)
                {
                    _ = targets.Add(Hero.MainHero);
                }

                if (ApplyToPlayerClanMembers && Hero.MainHero?.Clan != null)
                {
                    foreach (Hero hero in Hero.MainHero.Clan.Heroes)
                    {
                        if (hero != null)
                        {
                            _ = targets.Add(hero);
                        }
                    }
                }

                // Category: Kingdoms
                if (ApplyToKingdomRulers)
                {
                    // OPTIMIZED: Use cached collection instead of direct Kingdom.All enumeration
                    List<Kingdom>? allKingdoms = CampaignDataCache.AllKingdoms;
                    if (allKingdoms is null)
                    {
                        return targets;
                    }

                    foreach (Kingdom kingdom in allKingdoms)
                    {
                        if (kingdom?.Leader != null)
                        {
                            _ = targets.Add(kingdom.Leader);
                        }
                    }
                }

                if (ApplyToPlayerKingdomRuler)
                {
                    if (playerKingdom?.Leader != null)
                    {
                        _ = targets.Add(playerKingdom.Leader);
                    }
                }

                if (ApplyToKingdomVassals)
                {
                    // OPTIMIZED: Use cached collection instead of direct Kingdom.All enumeration
                    List<Kingdom>? allKingdoms = CampaignDataCache.AllKingdoms;
                    if (allKingdoms is null)
                    {
                        return targets;
                    }

                    foreach (Kingdom kingdom in allKingdoms)
                    {
                        if (kingdom != null)
                        {
                            foreach (Clan clan in kingdom.Clans)
                            {
                                // Exclude ruling clan (ruler is handled by ApplyToKingdomRulers)
                                if (clan != null && clan != kingdom.RulingClan && clan.Leader != null)
                                {
                                    _ = targets.Add(clan.Leader);
                                }
                            }
                        }
                    }
                }

                if (ApplyToPlayerKingdomVassals)
                {
                    if (playerKingdom != null)
                    {
                        foreach (Clan clan in playerKingdom.Clans)
                        {
                            // Exclude player clan and ruling clan
                            if (clan != null && clan != Clan.PlayerClan && clan != playerKingdom.RulingClan && clan.Leader != null)
                            {
                                _ = targets.Add(clan.Leader);
                            }
                        }
                    }
                }

                if (ApplyToKingdomNobles)
                {
                    // OPTIMIZED: Use cached collection instead of direct Kingdom.All enumeration
                    List<Kingdom>? allKingdoms = CampaignDataCache.AllKingdoms;
                    if (allKingdoms is null)
                    {
                        return targets;
                    }

                    foreach (Kingdom kingdom in allKingdoms)
                    {
                        if (kingdom != null)
                        {
                            foreach (Clan clan in kingdom.Clans)
                            {
                                // Exclude player clan - it's handled by ApplyToPlayerClanMembers
                                if (clan != null && clan != Clan.PlayerClan)
                                {
                                    foreach (Hero hero in clan.Heroes)
                                    {
                                        if (hero != null && hero.CharacterObject?.IsHero == true)
                                        {
                                            _ = targets.Add(hero);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (ApplyToPlayerKingdomNobles)
                {
                    if (playerKingdom != null)
                    {
                        foreach (Clan clan in playerKingdom.Clans)
                        {
                            // Exclude player clan - it's handled by ApplyToPlayerClanMembers
                            if (clan != null && clan != Clan.PlayerClan)
                            {
                                foreach (Hero hero in clan.Heroes)
                                {
                                    if (hero != null && hero.CharacterObject?.IsHero == true)
                                    {
                                        _ = targets.Add(hero);
                                    }
                                }
                            }
                        }
                    }
                }

                // Category: Minor Clans
                if (ApplyToMinorClanLeaders)
                {
                    // OPTIMIZED: Use cached collection instead of direct Clan.All enumeration
                    List<Clan>? allClans = CampaignDataCache.AllClans;
                    if (allClans is null)
                    {
                        return targets;
                    }

                    foreach (Clan clan in allClans)
                    {
                        if (clan?.IsMinorFaction == true && clan.Leader != null)
                        {
                            _ = targets.Add(clan.Leader);
                        }
                    }
                }

                if (ApplyToMinorClanMembers)
                {
                    // OPTIMIZED: Use cached collection instead of direct Clan.All enumeration
                    List<Clan>? allClans = CampaignDataCache.AllClans;
                    if (allClans is null)
                    {
                        return targets;
                    }

                    foreach (Clan clan in allClans)
                    {
                        if (clan?.IsMinorFaction == true)
                        {
                            foreach (Hero hero in clan.Heroes)
                            {
                                if (hero != null && hero.CharacterObject?.IsHero == true)
                                {
                                    _ = targets.Add(hero);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatSettings] Error in CollectTargetHeroes: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }

            return targets;
        }

        /// <summary>
        /// Checks if any NPC target option is enabled.
        /// Used for early-exit optimizations in cheat code.
        /// </summary>
        /// <returns>True if at least one NPC target option is enabled.</returns>
        public bool HasAnyNPCTargetEnabled()
        {
            try
            {
                return ApplyToPlayerClanMembers ||
                       ApplyToKingdomRulers ||
                       ApplyToPlayerKingdomRuler ||
                       ApplyToKingdomVassals ||
                       ApplyToPlayerKingdomVassals ||
                       ApplyToKingdomNobles ||
                       ApplyToPlayerKingdomNobles ||
                       ApplyToMinorClanLeaders ||
                       ApplyToMinorClanMembers;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatSettings] Error in HasAnyNPCTargetEnabled: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the player target is enabled.
        /// Convenience method for clearer code readability.
        /// </summary>
        /// <returns>True if player target is enabled, false otherwise.</returns>
        public bool IsPlayerTargetEnabled()
        {
            try
            {
                return ApplyToPlayer;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatSettings] Error in IsPlayerTargetEnabled: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        #endregion
    }
}
