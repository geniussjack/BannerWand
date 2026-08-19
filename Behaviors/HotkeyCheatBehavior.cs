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
    /// Polls BannerWand's custom hotkeys (registered via <see cref="BannerWandHotkeyCategory"/>)
    /// and applies the matching cheat when pressed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrors Mount &amp; Blade: Warband's cheat_mode hotkeys (Ctrl+X for an instant gold top-up),
    /// but implemented through the game's native rebindable hotkey system instead of a hardcoded
    /// key check, so the player can rebind it from Options -&gt; Key Bindings.
    /// </para>
    /// <para>
    /// This is a plain class ticked from <see cref="Core.SubModule.OnApplicationTick"/>, not a
    /// <see cref="CampaignBehaviorBase"/>: <see cref="CampaignEvents.TickEvent"/> and
    /// <see cref="CampaignEvents.HourlyTickEvent"/> only fire while campaign time is actually
    /// running, so a hotkey wired to either of them silently stops responding whenever the
    /// campaign is paused (e.g. in a menu screen) - exactly where a player is most likely to want
    /// to use one. <c>OnApplicationTick</c> fires every engine frame regardless of campaign pause
    /// state, matching how the game's own cheat hotkeys behave.
    /// </para>
    /// </remarks>
    public class HotkeyCheatBehavior
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

        /// <summary>
        /// Checks whether the "add gold" hotkey was just pressed and, if so, applies the
        /// configured gold amount. Intended to be called once per engine frame.
        /// </summary>
        /// <param name="dt">Time in seconds since the last frame (unused, kept for symmetry with other per-frame hooks).</param>
        public void Tick(float dt)
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

                // Hotkeys are only meaningful on the campaign map. Hero.MainHero is null outside
                // an active campaign (main menu, character creation), and Mission.Current is set
                // while a battle/dialogue/other scene is running.
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
                ModLogger.Error($"[HotkeyCheatBehavior] Error in {nameof(Tick)}: {ex.Message}", ex);
            }
        }
    }
}
