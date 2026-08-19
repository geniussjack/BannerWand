#nullable enable
// System namespaces
using System;
// Project namespaces
using BannerWand.Constants;
using BannerWand.Input;
using BannerWand.Settings;
using BannerWand.Utils;
// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Behaviors
{
    /// <summary>
    /// Campaign behavior that polls BannerWand's custom hotkeys (registered via
    /// <see cref="BannerWandHotkeyCategory"/>) and applies the matching cheat when pressed.
    /// </summary>
    /// <remarks>
    /// Mirrors Mount &amp; Blade: Warband's cheat_mode hotkeys (Ctrl+X for an instant gold top-up),
    /// but implemented through the game's native rebindable hotkey system instead of a hardcoded
    /// key check, so the player can rebind it from Options -&gt; Key Bindings.
    /// </remarks>
    public class HotkeyCheatBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Input context used to poll BannerWand's hotkeys. <see cref="HotKey"/>/<see cref="GameKey"/>
        /// only expose their press-state checks as non-public members, so polling goes through this
        /// <see cref="InputContext"/> instead, the same public entry point the game's own screens and
        /// view models use for hotkey checks.
        /// </summary>
        private readonly InputContext _inputContext = new()
        {
            IsKeysAllowed = true,
        };

        /// <summary>
        /// Whether <see cref="_inputContext"/> has picked up <see cref="BannerWandHotkeyCategory"/>
        /// from <see cref="HotKeyManager"/> yet. The category may not be registered on the very
        /// first tick, so this is retried until it succeeds instead of being done once up front.
        /// </summary>
        private bool _hotkeyCategoryRegistered;

        /// <inheritdoc />
        public override void RegisterEvents()
        {
            try
            {
                CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HotkeyCheatBehavior] Error in RegisterEvents: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public override void SyncData(IDataStore dataStore)
        {
            // No persistent data to sync - hotkey state is derived from settings and input each frame.
        }

        /// <summary>
        /// Called every real-time frame while the campaign map is active. Checks whether the
        /// "add gold" hotkey was just pressed and, if so, applies the configured gold amount.
        /// </summary>
        /// <param name="dt">Time in seconds since the last frame (unused, required by the event signature).</param>
        private void OnTick(float dt)
        {
            try
            {
                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings is null || targetSettings is null)
                {
                    return;
                }

                if (!settings.EnableAddGoldHotkey || settings.EditGold == 0 || !targetSettings.ApplyToPlayer)
                {
                    return;
                }

                // Hotkeys are only meaningful on the campaign map - a Mission (battle, dialogue
                // scene, etc.) has its own input handling and CampaignEvents.TickEvent does not
                // fire in one anyway, but the check is kept explicit to match the documented
                // "not in battle" behavior even if that assumption ever changes.
                if (Mission.Current is not null || Hero.MainHero is null)
                {
                    return;
                }

                if (!_hotkeyCategoryRegistered)
                {
                    GameKeyContext? category = HotKeyManager.GetCategory(BannerWandHotkeyCategory.CategoryId);
                    if (category is null)
                    {
                        // Registered in SubModule.OnSubModuleLoad; not available yet this tick.
                        return;
                    }

                    _inputContext.RegisterHotKeyCategory(category);
                    _hotkeyCategoryRegistered = true;
                }

                if (_inputContext.IsHotKeyPressed(BannerWandHotkeyCategory.AddGoldHotKeyId))
                {
                    Hero.MainHero.ChangeHeroGold(settings.EditGold);
                    string sign = settings.EditGold > 0 ? "+" : string.Empty;
                    InformationManager.DisplayMessage(new InformationMessage($"{sign}{settings.EditGold} Gold", GameConstants.SuccessColor));
                    ModLogger.LogCheat("Add Gold Hotkey", true, settings.EditGold, "player");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HotkeyCheatBehavior] Error in {nameof(OnTick)}: {ex.Message}", ex);
            }
        }
    }
}
