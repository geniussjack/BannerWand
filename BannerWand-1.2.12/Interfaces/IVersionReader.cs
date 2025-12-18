#nullable enable
namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for reading the mod version from SubModule.xml file.
    /// </summary>
    /// <remarks>
    /// This interface abstracts version reading functionality, allowing for different implementations
    /// such as static implementations, dependency injection scenarios, or mock implementations for testing.
    /// </remarks>
    public interface IVersionReader
    {
        /// <summary>
        /// Gets the mod version from SubModule.xml file.
        /// </summary>
        /// <returns>
        /// The version string (e.g., "v1.0.8") from SubModule.xml, or "Unknown" if the version cannot be read.
        /// </returns>
        /// <remarks>
        /// The version is cached after the first read to avoid repeated file I/O operations.
        /// The version is read from the &lt;Version value="..." /&gt; element in SubModule.xml.
        /// </remarks>
        string GetModVersion();

        /// <summary>
        /// Gets the mod version without the "v" prefix (e.g., "1.0.8" instead of "v1.0.8").
        /// </summary>
        /// <returns>
        /// The version string without the "v" prefix, or "Unknown" if the version cannot be read.
        /// </returns>
        string GetModVersionWithoutPrefix();
    }
}
