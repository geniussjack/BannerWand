#nullable enable
using System;
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerWand.Core
{
    /// <summary>
    /// Initializes and tears down BannerWand's cheat systems at campaign start and end.
    /// </summary>
    public static class CheatManager
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatSettings? TargetSettings => CheatSettings.Instance;

        /// <summary>
        /// Initializes the cheat manager when a campaign starts.
        /// </summary>
        /// <remarks>
        /// This method is called from <see cref="SubModule.OnGameStart"/> and validates that
        /// settings are available before the rest of the mod starts relying on them.
        /// </remarks>
        public static void Initialize()
        {
            try
            {
                if (Settings == null || TargetSettings == null)
                {
                    ModLogger.Error("Failed to initialize CheatManager - settings are null");
                    TextObject errorMessage = new("{=BW_Message_SettingsError}BannerWand: Settings Error");
                    InformationManager.DisplayMessage(new InformationMessage(errorMessage.ToString(), GameConstants.ErrorColor));
                    return;
                }

                ModLogger.Log(MessageConstants.CheatManagerInitialized);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatManager] Error in {nameof(Initialize)}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cleans up the cheat manager when a campaign ends.
        /// </summary>
        /// <remarks>
        /// Called from <see cref="SubModule.OnGameEnd"/>.
        /// </remarks>
        public static void Cleanup()
        {
            try
            {
                ModLogger.Log(MessageConstants.CheatManagerCleanup);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatManager] Error in {nameof(Cleanup)}: {ex.Message}", ex);
            }
        }
    }
}
