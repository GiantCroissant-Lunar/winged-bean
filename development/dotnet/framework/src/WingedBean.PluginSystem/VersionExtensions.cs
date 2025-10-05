using NuGet.Versioning;

namespace WingedBean.PluginSystem;

/// <summary>
/// Extension methods and helpers for working with NuGet.Versioning types
/// </summary>
public static class VersionExtensions
{
    /// <summary>
    /// Convert custom SemanticVersion to NuGetVersion (for backward compatibility)
    /// </summary>
    [Obsolete("This method is only for backward compatibility. Use NuGetVersion directly.")]
    public static NuGetVersion ToNuGetVersion(this SemanticVersion version)
    {
        return NuGetVersion.Parse(version.ToString());
    }

    /// <summary>
    /// Convert NuGetVersion to custom SemanticVersion (for backward compatibility)
    /// </summary>
    [Obsolete("This method is only for backward compatibility. Use NuGetVersion directly.")]
    public static SemanticVersion ToSemanticVersion(this NuGetVersion version)
    {
        return SemanticVersion.Parse(version.ToString());
    }

    /// <summary>
    /// Parse a version string with enhanced error messages
    /// </summary>
    /// <param name="version">Version string to parse (e.g., "1.2.3", "1.2.3-beta.1", "1.2.3+build.123")</param>
    /// <returns>Parsed NuGetVersion</returns>
    /// <exception cref="ArgumentException">Thrown when version string is invalid</exception>
    public static NuGetVersion ParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version string cannot be null or empty", nameof(version));

        if (NuGetVersion.TryParse(version, out var result))
            return result;

        throw new ArgumentException(
            $"Invalid semantic version: '{version}'. Must follow SemVer 2.0 format (e.g., 1.2.3, 1.2.3-beta.1, 1.2.3+build.123)",
            nameof(version));
    }

    /// <summary>
    /// Try to parse a version string
    /// </summary>
    /// <param name="version">Version string to parse</param>
    /// <param name="result">Parsed version or null if parsing failed</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseVersion(string? version, out NuGetVersion? result)
    {
        return NuGetVersion.TryParse(version, out result);
    }

    /// <summary>
    /// Parse a version range string with support for npm-style ranges (^, ~)
    /// </summary>
    /// <param name="range">Version range string (e.g., "1.2.3", "^1.2.0", "~1.2.3", "[1.0.0, 2.0.0)")</param>
    /// <returns>Parsed VersionRange</returns>
    /// <exception cref="ArgumentException">Thrown when range string is invalid</exception>
    public static NuGet.Versioning.VersionRange ParseVersionRange(string range)
    {
        if (string.IsNullOrWhiteSpace(range))
            throw new ArgumentException("Version range string cannot be null or empty", nameof(range));

        var trimmedRange = range.Trim();

        // Handle npm-style caret (^) - compatible within same major version
        // ^1.2.3 means >=1.2.3 <2.0.0
        if (trimmedRange.StartsWith("^"))
        {
            var versionString = trimmedRange[1..];
            var version = ParseVersion(versionString);

            return new NuGet.Versioning.VersionRange(
                minVersion: version,
                includeMinVersion: true,
                maxVersion: new NuGetVersion(version.Major + 1, 0, 0),
                includeMaxVersion: false);
        }

        // Handle npm-style tilde (~) - compatible within same minor version
        // ~1.2.3 means >=1.2.3 <1.3.0
        if (trimmedRange.StartsWith("~"))
        {
            var versionString = trimmedRange[1..];
            var version = ParseVersion(versionString);

            return new NuGet.Versioning.VersionRange(
                minVersion: version,
                includeMinVersion: true,
                maxVersion: new NuGetVersion(version.Major, version.Minor + 1, 0),
                includeMaxVersion: false);
        }

        // Try standard NuGet range notation: [1.0.0, 2.0.0), (1.0.0, ), etc.
        if (NuGet.Versioning.VersionRange.TryParse(trimmedRange, out var nugetRange))
            return nugetRange;

        // Treat as exact version
        var exactVersion = ParseVersion(trimmedRange);
        return new NuGet.Versioning.VersionRange(exactVersion, true, exactVersion, true);
    }

    /// <summary>
    /// Try to parse a version range string
    /// </summary>
    /// <param name="range">Version range string</param>
    /// <param name="result">Parsed range or null if parsing failed</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseVersionRange(string? range, out NuGet.Versioning.VersionRange? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(range))
            return false;

        try
        {
            result = ParseVersionRange(range);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a version satisfies a range
    /// </summary>
    /// <param name="range">Version range to check against</param>
    /// <param name="version">Version to check</param>
    /// <returns>True if version satisfies the range</returns>
    public static bool Satisfies(this NuGet.Versioning.VersionRange range, NuGetVersion version)
    {
        return range.Satisfies(version);
    }

    /// <summary>
    /// Get a user-friendly string representation of a version range
    /// </summary>
    public static string ToFriendlyString(this NuGet.Versioning.VersionRange range)
    {
        if (range.MinVersion == range.MaxVersion && range.IsMinInclusive && range.IsMaxInclusive)
            return range.MinVersion?.ToString() ?? "*";

        if (range.MinVersion != null && range.MaxVersion == null)
            return $">={range.MinVersion}";

        if (range.MinVersion == null && range.MaxVersion != null)
            return $"<{(range.IsMaxInclusive ? "=" : "")}{range.MaxVersion}";

        return range.ToString();
    }
}
