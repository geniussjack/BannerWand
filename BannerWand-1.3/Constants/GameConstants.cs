using TaleWorlds.Library;

namespace BannerWand.Constants
{
    /// <summary>
    /// Game-related constants for cheats, thresholds, and limits.
    /// </summary>
    /// <remarks>
    /// This class centralizes all magic numbers and game-specific values
    /// used throughout the mod, making them easier to maintain and modify.
    /// </remarks>
    public static class GameConstants
    {
        #region Hero and Relationship Constants

        /// <summary>
        /// Maximum relationship value between heroes.
        /// </summary>
        public const int MaxRelationship = 100;

        /// <summary>
        /// Maximum skill level achievable.
        /// </summary>
        public const int MaxSkillLevel = 330;

        /// <summary>
        /// Maximum number of heroes to process per daily tick for relationship improvements.
        /// </summary>
        /// <remarks>
        /// Performance limit to avoid frame drops with large hero counts.
        /// </remarks>
        public const int MaxHeroesProcessedPerDay = 20;

        #endregion

        #region Experience and Progression Constants

        /// <summary>
        /// Amount of skill XP added per hour when Unlimited Skill XP is enabled.
        /// </summary>
        public const int UnlimitedSkillXPPerHour = 999;

        /// <summary>
        /// Amount of troop XP added per hour when Unlimited Troops XP is enabled.
        /// </summary>
        public const int UnlimitedTroopXPPerHour = 999;

        /// <summary>
        /// Base XP amount used for troop XP multiplier calculations.
        /// </summary>
        public const int TroopXPBaseAmount = 100;

        /// <summary>
        /// Amount of renown added per hour when Unlimited Renown is enabled.
        /// </summary>
        public const float UnlimitedRenownPerHour = 999f;

        /// <summary>
        /// Target renown cap for unlimited renown cheat.
        /// </summary>
        public const float MaxUnlimitedRenown = 10000f;

        #endregion

        #region Inventory and Resources Constants

        /// <summary>
        /// Amount of smithing materials to restore when threshold is reached.
        /// </summary>
        public const int SmithingMaterialReplenishAmount = 1000;

        /// <summary>
        /// Threshold below which smithing materials are replenished.
        /// </summary>
        public const int SmithingMaterialThreshold = 500;

        /// <summary>
        /// Target amount for smithing materials replenishment.
        /// </summary>
        public const int SmithingMaterialTargetAmount = 9999;

        /// <summary>
        /// Minimum food threshold before replenishment.
        /// </summary>
        public const int MinFoodThreshold = 10;

        /// <summary>
        /// Amount of food (grain) to add when replenishing.
        /// </summary>
        public const int FoodReplenishAmount = 100;

        /// <summary>
        /// Maximum carrying capacity for unlimited inventory.
        /// </summary>
        public const int MaxCarryingCapacity = 999999;

        #endregion

        #region Barter and Trade Constants

        /// <summary>
        /// Penalty value added to barter to force acceptance.
        /// </summary>
        /// <remarks>
        /// Large negative value makes any barter automatically accepted.
        /// </remarks>
        public const int BarterAutoAcceptPenalty = -999999;

        #endregion

        #region Construction and Building Constants

        /// <summary>
        /// Construction power value for instant building completion.
        /// </summary>
        public const int InstantConstructionPower = 999999;

        /// <summary>
        /// Siege construction progress per hour for instant siege equipment.
        /// </summary>
        public const int InstantSiegeConstructionRate = 999;

        #endregion

        #region Performance and Safety Limits

        /// <summary>
        /// Maximum safe value for game speed multiplier.
        /// </summary>
        /// <remarks>
        /// Matches the maximum value in MCM settings (16f).
        /// Values above this are considered potentially unstable.
        /// </remarks>
        public const float MaxSafeGameSpeed = 16f;

        /// <summary>
        /// Absolute maximum game speed multiplier.
        /// </summary>
        public const float AbsoluteMaxGameSpeed = 16f;

        /// <summary>
        /// Maximum safe value for renown multiplier.
        /// </summary>
        public const float MaxSafeRenownMultiplier = 10f;

        #endregion

        #region Combat Constants

        /// <summary>
        /// Health bonus for Infinite Health cheat.
        /// </summary>
        public const float InfiniteHealthBonus = 9999f;

        /// <summary>
        /// Minimum health threshold for one-hit kill enemies.
        /// </summary>
        public const float OneHitKillHealthThreshold = 1f;

        /// <summary>
        /// Instant kill health value.
        /// </summary>
        public const float InstantKillHealth = 0f;

        /// <summary>
        /// Target ammunition count for unlimited ammo cheat.
        /// </summary>
        /// <remarks>
        /// Set to 999 to provide effectively unlimited ammunition while still showing
        /// a realistic count in the UI. Going higher might cause UI display issues.
        /// </remarks>
        public const short UnlimitedAmmoTarget = 999;

        /// <summary>
        /// Maximum morale base value for Max Morale cheat.
        /// </summary>
        public const float MaxMoraleBaseValue = 999f;

        /// <summary>
        /// Low morale value for enemy parties when one-hit kills is enabled.
        /// </summary>
        public const float LowEnemyMoraleValue = 10f;

        /// <summary>
        /// Unlimited XP multiplier for rapid skill progression.
        /// </summary>
        public const float UnlimitedXpMultiplier = 999f;

        /// <summary>
        /// Instant siege construction progress per hour.
        /// </summary>
        public const float InstantSiegeConstructionProgress = 999999f;

        #endregion

        #region UI and Display Constants

        /// <summary>
        /// Color for error messages.
        /// </summary>
        public static readonly Color ErrorColor = Colors.Red;

        /// <summary>
        /// Color for success messages.
        /// </summary>
        public static readonly Color SuccessColor = Colors.Green;

        /// <summary>
        /// Color for info messages.
        /// </summary>
        public static readonly Color InfoColor = Colors.Cyan;

        /// <summary>
        /// Color for warning messages.
        /// </summary>
        public static readonly Color WarningColor = Colors.Yellow;

        #endregion
    }
}
