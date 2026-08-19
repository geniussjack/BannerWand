namespace BannerWand.Constants
{
    /// <summary>
    /// String constants for user messages and notifications.
    /// </summary>
    /// <remarks>
    /// Centralizes all user-facing messages to make localization and
    /// message updates easier in the future.
    /// </remarks>
    public static class MessageConstants
    {
        #region Mod Initialization Messages

        /// <summary>
        /// Prefix for all BannerWand messages.
        /// </summary>
        public const string ModPrefix = "BannerWand";

        /// <summary>
        /// Message shown when mod initializes successfully with no cheats active.
        /// </summary>
        public const string InitializedNoCheats = "BannerWand Initialized (no cheats active)";

        /// <summary>
        /// Format string for initialization message with active cheat count.
        /// Use with string.Format(InitializedWithCheatsFormat, count).
        /// </summary>
        public const string InitializedWithCheatsFormat = "BannerWand Initialized ({0} cheats active)";

        /// <summary>
        /// Error message when settings are not available.
        /// </summary>
        public const string SettingsError = "BannerWand: Settings Error";

        /// <summary>
        /// Format string for mod initialization message with version.
        /// Use with string.Format(ModInitializedSuccessfullyFormat, version).
        /// </summary>
        public const string ModInitializedSuccessfullyFormat = "BannerWand {0} successfully initialized";

        /// <summary>
        /// Format string for cheat initialization message when player appears on campaign map.
        /// Use with string.Format(CheatsInitializedFormat, count).
        /// </summary>
        public const string CheatsInitializedFormat = "{0} cheats successfully initialized";

        #endregion

        #region Cheat Application Messages

        /// <summary>
        /// Format string for gold application message.
        /// Parameters: {0} = amount, {1} = hero count
        /// </summary>
        public const string GoldAppliedFormat = "Gold {0}: {1} to {2} heroes";

        /// <summary>
        /// Format string for influence application message.
        /// Parameters: {0} = amount, {1} = clan count
        /// </summary>
        public const string InfluenceAppliedFormat = "Influence {0}: {1} to {2} clans";

        /// <summary>
        /// Format string for attribute points application.
        /// Parameters: {0} = amount, {1} = hero count
        /// </summary>
        public const string AttributePointsAppliedFormat = "Attribute points {0}: {1} to {2} heroes";

        /// <summary>
        /// Format string for focus points application.
        /// Parameters: {0} = amount, {1} = hero count
        /// </summary>
        public const string FocusPointsAppliedFormat = "Focus points {0}: {1} to {2} heroes";

        /// <summary>
        /// Format string for wounded troops healed message.
        /// Parameters: {0} = troop count
        /// </summary>
        public const string WoundedTroopsHealedFormat = "Healed {0} wounded troops";

        #endregion

        #region Direction Labels

        /// <summary>
        /// Label for adding resources.
        /// </summary>
        public const string Added = "added";

        /// <summary>
        /// Label for removing resources.
        /// </summary>
        public const string Removed = "removed";

        #endregion

        #region Error and Warning Messages

        /// <summary>
        /// Warning when no targets are available for a cheat.
        /// </summary>
        public const string NoTargetsAvailable = "No valid targets available for this cheat";

        /// <summary>
        /// Warning when settings are null.
        /// </summary>
        public const string SettingsNull = "Settings are null";

        /// <summary>
        /// Warning when target settings are null.
        /// </summary>
        public const string TargetSettingsNull = "Target settings are null";

        /// <summary>
        /// Default string for unknown party names.
        /// </summary>
        public const string UnknownPartyName = "Unknown";

        /// <summary>
        /// Default string for unknown version.
        /// </summary>
        public const string UnknownVersion = "Unknown";

        #endregion

        #region Logging Messages

        /// <summary>
        /// Log message for cheat manager initialization.
        /// </summary>
        public const string CheatManagerInitialized = "CheatManager initialized successfully";

        /// <summary>
        /// Log message for cheat manager cleanup.
        /// </summary>
        public const string CheatManagerCleanup = "CheatManager cleanup completed";

        /// <summary>
        /// Format string for active cheats count log.
        /// Parameters: {0} = count
        /// </summary>
        public const string ActiveCheatsCountFormat = "Active cheats count: {0}";

        /// <summary>
        /// Header for localization check section in logs.
        /// </summary>
        public const string LocalizationCheckHeader = "=== Localization Check ===";

        /// <summary>
        /// Footer for localization check section in logs.
        /// </summary>
        public const string LocalizationCheckFooter = "=== End Localization Check ===";

        /// <summary>
        /// Warning message when localization is not working.
        /// </summary>
        public const string LocalizationNotWorking = "Localization NOT working - strings contain {=ID} tags!";

        /// <summary>
        /// Instruction message for localization folder structure.
        /// </summary>
        public const string LocalizationFolderStructure = "Make sure ModuleData/Languages/ folder structure is correct";

        /// <summary>
        /// Required files message for localization.
        /// </summary>
        public const string LocalizationRequiredFiles = "Required files: language_data.xml and strings.xml in each language folder";

        /// <summary>
        /// Success message when localization is working.
        /// </summary>
        public const string LocalizationWorking = "Localization working! Loaded text: '{0}'";

        /// <summary>
        /// Status text for enabled cheats.
        /// </summary>
        public const string CheatStatusEnabled = "ENABLED";

        /// <summary>
        /// Status text for disabled cheats.
        /// </summary>
        public const string CheatStatusDisabled = "DISABLED";

        #endregion
    }
}
