using System.Text.RegularExpressions;

namespace WingedBean.Host;

/// <summary>
/// Semantic version implementation supporting comparison and range matching
/// </summary>
public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    private static readonly Regex VersionRegex = new(@"^(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$");

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? PreRelease { get; }
    public string? Build { get; }

    public SemanticVersion(int major, int minor, int patch, string? preRelease = null, string? build = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        Build = build;
    }

    public static SemanticVersion Parse(string version)
    {
        if (!TryParse(version, out var result))
            throw new ArgumentException($"Invalid semantic version: {version}");
        return result;
    }

    public static bool TryParse(string version, out SemanticVersion? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(version))
            return false;

        var match = VersionRegex.Match(version.Trim());
        if (!match.Success)
            return false;

        var major = int.Parse(match.Groups[1].Value);
        var minor = int.Parse(match.Groups[2].Value);
        var patch = int.Parse(match.Groups[3].Value);
        var preRelease = match.Groups[4].Success ? match.Groups[4].Value : null;
        var build = match.Groups[5].Success ? match.Groups[5].Value : null;

        result = new SemanticVersion(major, minor, patch, preRelease, build);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;

        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;

        var minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0) return minorCompare;

        var patchCompare = Patch.CompareTo(other.Patch);
        if (patchCompare != 0) return patchCompare;

        // Pre-release versions have lower precedence than normal versions
        return (PreRelease, other.PreRelease) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            var (p1, p2) => string.Compare(p1, p2, StringComparison.Ordinal)
        };
    }

    public bool Equals(SemanticVersion? other)
    {
        if (other is null) return false;
        return Major == other.Major && 
               Minor == other.Minor && 
               Patch == other.Patch && 
               PreRelease == other.PreRelease;
    }

    public override bool Equals(object? obj) => Equals(obj as SemanticVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(PreRelease))
            version += $"-{PreRelease}";
        if (!string.IsNullOrEmpty(Build))
            version += $"+{Build}";
        return version;
    }

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) => 
        left?.Equals(right) ?? right is null;

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) => 
        !(left == right);

    public static bool operator <(SemanticVersion? left, SemanticVersion? right) => 
        left?.CompareTo(right) < 0;

    public static bool operator >(SemanticVersion? left, SemanticVersion? right) => 
        left?.CompareTo(right) > 0;

    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) => 
        left?.CompareTo(right) <= 0;

    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) => 
        left?.CompareTo(right) >= 0;
}

/// <summary>
/// Version range specification supporting npm-style version requirements
/// </summary>
public class VersionRange
{
    private static readonly Regex RangeRegex = new(@"^([\^~])?(\d+(?:\.\d+)?(?:\.\d+)?(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?)$");

    public enum RangeType
    {
        Exact,      // 1.2.3
        Compatible, // ^1.2.3 (compatible within major version)
        Tilde       // ~1.2.3 (compatible within minor version)
    }

    public RangeType Type { get; }
    public SemanticVersion Version { get; }

    public VersionRange(RangeType type, SemanticVersion version)
    {
        Type = type;
        Version = version;
    }

    public static VersionRange Parse(string range)
    {
        if (!TryParse(range, out var result))
            throw new ArgumentException($"Invalid version range: {range}");
        return result;
    }

    public static bool TryParse(string range, out VersionRange? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(range))
            return false;

        var match = RangeRegex.Match(range.Trim());
        if (!match.Success)
            return false;

        var prefix = match.Groups[1].Value;
        var versionStr = match.Groups[2].Value;

        if (!SemanticVersion.TryParse(versionStr, out var version))
            return false;

        var type = prefix switch
        {
            "^" => RangeType.Compatible,
            "~" => RangeType.Tilde,
            _ => RangeType.Exact
        };

        result = new VersionRange(type, version);
        return true;
    }

    public bool Satisfies(SemanticVersion version)
    {
        return Type switch
        {
            RangeType.Exact => Version.Equals(version),
            RangeType.Compatible => IsCompatible(version),
            RangeType.Tilde => IsTildeCompatible(version),
            _ => false
        };
    }

    private bool IsCompatible(SemanticVersion version)
    {
        // ^1.2.3 allows >=1.2.3 but <2.0.0
        return version >= Version && version.Major == Version.Major;
    }

    private bool IsTildeCompatible(SemanticVersion version)
    {
        // ~1.2.3 allows >=1.2.3 but <1.3.0
        return version >= Version && 
               version.Major == Version.Major && 
               version.Minor == Version.Minor;
    }

    public override string ToString()
    {
        var prefix = Type switch
        {
            RangeType.Compatible => "^",
            RangeType.Tilde => "~",
            _ => ""
        };
        return prefix + Version.ToString();
    }
}
