#nullable enable
// Third-party namespaces
using TaleWorlds.InputSystem;

namespace BannerWand.Input
{
    /// <summary>
    /// Registers BannerWand's own hotkeys with the game's native key-binding system, so they
    /// show up under Options -&gt; Key Bindings and can be rebound by the player like any other
    /// game hotkey.
    /// </summary>
    /// <remarks>
    /// Modelled after the game's own <c>PartyHotKeyCategory</c>/<c>CheatsHotKeyCategory</c>: a
    /// <see cref="GameKeyContext"/> subclass that registers its <see cref="HotKey"/>s from its
    /// constructor and is handed to <see cref="HotKeyManager.RegisterContext"/> once at startup.
    /// </remarks>
    public class BannerWandHotkeyCategory : GameKeyContext
    {
        /// <summary>
        /// The category id this context registers under with <see cref="HotKeyManager"/>.
        /// </summary>
        public const string CategoryId = "BannerWandHotkeyCategory";

        /// <summary>
        /// The string id of the "add gold" hotkey, used to look it up again via
        /// <see cref="GameKeyContext.GetHotKey"/> once registered.
        /// </summary>
        public const string AddGoldHotKeyId = "BWAddGold";

        /// <summary>
        /// Initializes the category and registers its hotkeys.
        /// </summary>
        /// <remarks>
        /// <see cref="GameKeyContextType.AuxiliarySerializedAndShownInOptions"/> makes the game
        /// list this category in the Key Bindings screen and persist any rebinding the player does.
        /// </remarks>
        public BannerWandHotkeyCategory()
            : base(CategoryId, 0, GameKeyContextType.AuxiliarySerializedAndShownInOptions)
        {
            RegisterHotKey(
                new HotKey(AddGoldHotKeyId, CategoryId, InputKey.X, HotKey.Modifiers.Control, HotKey.Modifiers.None),
                addIfMissing: true);
        }
    }
}
