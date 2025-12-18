#nullable enable
namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for managing cheat operations in BannerWand mod.
    /// Provides centralized interface for applying cheats across different game entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts the cheat management system, allowing for different implementations
    /// such as static implementations, dependency injection scenarios, or mock implementations for testing.
    /// </para>
    /// <para>
    /// The cheat manager acts as a facade for common cheat operations, providing a centralized
    /// interface for applying cheats across different game entities. It handles validation,
    /// logging, and performance tracking for all cheat operations.
    /// </para>
    /// <para>
    /// See <see cref="Core.CheatManager"/> for the default static implementation.
    /// </para>
    /// </remarks>
    public interface ICheatManager
    {
        /// <summary>
        /// Initializes the cheat manager when a campaign starts.
        /// </summary>
        /// <param name="showMessage">Whether to show the initialization message to the user. Default is true.</param>
        /// <remarks>
        /// This method is called from <see cref="Core.SubModule.OnGameStart"/> and performs
        /// initial setup, validation, and logging.
        /// </remarks>
        void Initialize(bool showMessage = true);

        /// <summary>
        /// Gets the count of currently active cheats.
        /// </summary>
        /// <returns>The number of cheats that are currently enabled and active.</returns>
        /// <remarks>
        /// This method counts all cheats that are enabled in settings and have valid targets.
        /// Used for user feedback and logging purposes.
        /// </remarks>
        int GetActiveCheatCount();

        /// <summary>
        /// Checks if any cheat is currently active.
        /// </summary>
        /// <returns><c>true</c> if at least one cheat is active; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This is a performance-optimized check that returns early on the first active cheat found.
        /// </remarks>
        bool IsAnyCheatActive();

        /// <summary>
        /// Applies attribute points to the player and/or NPCs based on target settings.
        /// </summary>
        /// <param name="amount">Amount of attribute points to add (positive) or remove (negative).</param>
        /// <remarks>
        /// Negative amounts are automatically clamped to prevent negative attribute points.
        /// </remarks>
        void ApplyAttributePoints(int amount);

        /// <summary>
        /// Applies focus points to the player and/or NPCs based on target settings.
        /// </summary>
        /// <param name="amount">Amount of focus points to add (positive) or remove (negative).</param>
        /// <remarks>
        /// Negative amounts are automatically clamped to prevent negative focus points.
        /// </remarks>
        void ApplyFocusPoints(int amount);

        /// <summary>
        /// Heals all wounded troops in the player's party.
        /// </summary>
        /// <remarks>
        /// This method iterates through all party members and heals any wounded troops.
        /// Used by the "Heal Wounded Troops" cheat functionality.
        /// </remarks>
        void HealWoundedTroops();
    }
}
