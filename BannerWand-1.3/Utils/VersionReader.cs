#nullable enable
// System namespaces
using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

// Project namespaces
using BannerWand.Constants;

namespace BannerWand.Utils
{
    /// <summary>
    /// Utility class for reading the mod version from SubModule.xml file.
    /// </summary>
    /// <remarks>
    /// This class provides a centralized way to read the mod version from the SubModule.xml file,
    /// ensuring that version information is consistent across all parts of the mod (logs, messages, etc.).
    /// </remarks>
    public static class VersionReader
    {
        private static string? _cachedVersion;

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
        public static string GetModVersion()
        {
            if (_cachedVersion != null)
            {
                return _cachedVersion;
            }

            try
            {
                // Get the assembly location to find the module directory
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                string? assemblyLocation = executingAssembly.Location;

                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    ModLogger.Warning("Cannot read version: Assembly location is empty");
                    _cachedVersion = MessageConstants.UnknownVersion;
                    return _cachedVersion;
                }

                // Navigate from DLL location to module root
                // DLL is in: ...\Modules\BannerWand\bin\Win64_Shipping_Client\BannerWand.dll
                // Module root is: ...\Modules\BannerWand\
                string? dllDirectory = Path.GetDirectoryName(assemblyLocation);
                if (string.IsNullOrEmpty(dllDirectory))
                {
                    ModLogger.Warning("Cannot read version: DLL directory is empty");
                    _cachedVersion = MessageConstants.UnknownVersion;
                    return _cachedVersion;
                }

                // Navigate up: bin\Win64_Shipping_Client -> bin -> BannerWand
                string? moduleDirectory = Path.GetDirectoryName(Path.GetDirectoryName(dllDirectory));
                if (string.IsNullOrEmpty(moduleDirectory))
                {
                    ModLogger.Warning("Cannot read version: Module directory is empty");
                    _cachedVersion = MessageConstants.UnknownVersion;
                    return _cachedVersion;
                }

                // SubModule.xml is in the module root directory
                string subModuleXmlPath = Path.Combine(moduleDirectory, "SubModule.xml");
                if (!File.Exists(subModuleXmlPath))
                {
                    ModLogger.Warning($"Cannot read version: SubModule.xml not found at {subModuleXmlPath}");
                    _cachedVersion = MessageConstants.UnknownVersion;
                    return _cachedVersion;
                }

                // Read and parse the XML file
                XDocument doc = XDocument.Load(subModuleXmlPath);
                XElement? moduleElement = doc.Element("Module");
                XElement? versionElement = moduleElement?.Element("Version");

                if (versionElement == null)
                {
                    ModLogger.Warning("Cannot read version: <Version> element not found in SubModule.xml");
                    _cachedVersion = MessageConstants.UnknownVersion;
                    return _cachedVersion;
                }

                XAttribute? versionAttribute = versionElement.Attribute("value");
                if (versionAttribute == null || string.IsNullOrEmpty(versionAttribute.Value))
                {
                    ModLogger.Warning("Cannot read version: 'value' attribute not found or empty in <Version> element");
                    _cachedVersion = MessageConstants.UnknownVersion;
                    return _cachedVersion;
                }

                _cachedVersion = versionAttribute.Value;
                ModLogger.Debug($"Mod version read from SubModule.xml: {_cachedVersion}");
                return _cachedVersion;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Failed to read version from SubModule.xml: {ex.Message}", ex);
                _cachedVersion = "Unknown";
                return _cachedVersion;
            }
        }

        /// <summary>
        /// Gets the mod version without the "v" prefix (e.g., "1.0.8" instead of "v1.0.8").
        /// </summary>
        /// <returns>
        /// The version string without the "v" prefix, or "Unknown" if the version cannot be read.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method calls <see cref="GetModVersion"/> and removes the "v" prefix if present.
        /// Useful for display purposes where the prefix is not desired.
        /// </para>
        /// <para>
        /// The version is cached after the first read, so subsequent calls are O(1).
        /// </para>
        /// </remarks>
        public static string GetModVersionWithoutPrefix()
        {
            string version = GetModVersion();
            return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version.Substring(1) : version;
        }
    }
}

