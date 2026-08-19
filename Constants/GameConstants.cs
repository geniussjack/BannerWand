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
        /// Minimum age for NPC aging to be stopped (21 years old).
        /// Children under this age will continue to grow normally.
        /// </summary>
        /// <remarks>
        /// Used by aging prevention cheats to determine when NPCs should stop aging.
        /// </remarks>
        public const int MinimumAgeForStopAging = 21;

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
        /// Increased to 2000 for more noticeable effect.
        /// Typical troop upgrade requires 1000-5000 XP depending on tier.
        /// With multiplier x16, this gives 32000 XP per hour, which should be very noticeable.
        /// </summary>
        public const int TroopXPBaseAmount = 2000;

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
        /// Threshold below which smithing materials are replenished during smithing sessions.
        /// Set to 9990 to restore materials if used during smithing (10 below target amount).
        /// </summary>
        public const int SmithingMaterialReplenishThreshold = 9990;

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
        /// Safe minimum ammunition count to prevent game from blocking shots.
        /// Game blocks shots when ammo is 0 or 1, so we maintain at least this amount.
        /// </summary>
        /// <remarks>
        /// Set to 5 to provide safety margin. Game checks for ammo before shooting,
        /// and if it sees 0-1, it blocks the shot. By maintaining 5+, we ensure shots are never blocked.
        /// </remarks>
        public const short SafeMinimumAmmo = 5;

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

        /// <summary>
        /// Mission tick interval for stealth cheat status logging (every 5 seconds at 60 FPS).
        /// </summary>
        public const int StealthStatusCheckTickInterval = 300;

        /// <summary>
        /// Minimum ammunition amount threshold for throwing weapons to trigger weight compensation.
        /// </summary>
        public const int ThrowingWeaponMinAmmoThreshold = 10;

        /// <summary>
        /// Weight reduction divisor for speed multiplier calculation.
        /// </summary>
        public const float SpeedBoostWeightDivisor = 50.0f;

        /// <summary>
        /// Maximum speed boost multiplier cap (base 1.0f + 2.0f = 3.0x total speed).
        /// </summary>
        public const float MaxSpeedBoostMultiplier = 2.0f;

        /// <summary>
        /// Base movement speed multiplier (1.0 = normal speed).
        /// </summary>
        public const float BaseSpeedMultiplier = 1.0f;

        /// <summary>
        /// Movement detection threshold for velocity checks (minimum change to consider movement).
        /// </summary>
        public const float MovementDetectionThreshold = 0.01f;

        /// <summary>
        /// Maximum number of times to log the same message to avoid log spam.
        /// </summary>
        public const int MaxRepeatLogCount = 3;

        /// <summary>
        /// Interval for logging troop XP progress (log every Nth call).
        /// Used to reduce log spam when game speed is high.
        /// </summary>
        public const int TroopXPLogInterval = 10;

        /// <summary>
        /// AI slowdown factor (reduces speed to 50%).
        /// </summary>
        /// <remarks>
        /// AddFactor(-0.5) reduces speed by 50% (half speed).
        /// </remarks>
        public const float AiSlowdownFactor = -0.5f;

        /// <summary>
        /// Minimum base speed threshold for speed calculations.
        /// </summary>
        /// <remarks>
        /// Used to avoid division by zero when base speed is too low.
        /// </remarks>
        public const float MinBaseSpeedThreshold = 0.01f;

        /// <summary>
        /// Epsilon value for float comparisons.
        /// </summary>
        /// <remarks>
        /// Used to check if float values are effectively zero or non-zero.
        /// </remarks>
        public const float FloatEpsilon = 0.01f;

        /// <summary>
        /// Health bonus threshold multiplier for Infinite Health detection.
        /// </summary>
        /// <remarks>
        /// Used to check if Infinite Health bonus has been applied (90% of bonus value).
        /// Prevents re-applying the bonus if it's already been applied.
        /// </remarks>
        public const float InfiniteHealthBonusThresholdMultiplier = 0.9f;

        /// <summary>
        /// Maximum speed multiplier cap to prevent extreme values.
        /// </summary>
        /// <remarks>
        /// Used in speed calculations to prevent distortion when base speed is very small.
        /// Maximum multiplier of 100x prevents distortion while still allowing significant speed boosts.
        /// </remarks>
        public const float MaxSpeedMultiplier = 100f;

        /// <summary>
        /// Speed change detection threshold for logging.
        /// </summary>
        /// <remarks>
        /// Used to determine if speed has changed significantly enough to log.
        /// Prevents excessive logging for minor speed fluctuations.
        /// </remarks>
        public const float SpeedChangeDetectionThreshold = 0.01f;

        /// <summary>
        /// Multiplier factor base value (1.0 = no change).
        /// </summary>
        /// <remarks>
        /// Used when calculating factor to add: factorToAdd = multiplier - 1.0f.
        /// </remarks>
        public const float MultiplierFactorBase = 1.0f;

        #endregion

        #region Settlement Constants

        /// <summary>
        /// Maximum value for numerical settlement bonuses (Food, Prosperity, Loyalty, Security).
        /// </summary>
        public const int MaxSettlementBonusValue = 999;

        /// <summary>
        /// Default value for garrison wages multiplier (1.0 = normal wages).
        /// </summary>
        public const float DefaultGarrisonWagesMultiplier = 1.0f;

        /// <summary>
        /// Maximum multiplier for garrison wages (10.0 = 10x wages).
        /// </summary>
        public const float MaxGarrisonWagesMultiplier = 10.0f;

        /// <summary>
        /// Minimum multiplier for garrison wages (-10.0 = negative wages, income for owner).
        /// </summary>
        public const float MinGarrisonWagesMultiplier = -10.0f;

        /// <summary>
        /// Maximum multiplier for recruitment rates (100.0 = 100x recruitment).
        /// </summary>
        public const float MaxRecruitmentMultiplier = 100.0f;

        /// <summary>
        /// Maximum chance percentage for veteran militiamen (100.0 = 100% chance).
        /// </summary>
        public const float MaxMilitiaVeteranChance = 100.0f;

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
