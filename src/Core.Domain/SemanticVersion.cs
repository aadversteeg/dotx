namespace Core.Domain;

public sealed class SemanticVersion : IComparable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? Prerelease { get; }

    private SemanticVersion(int major, int minor, int patch, string? prerelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
    }

    public static bool TryParse(string? version, out SemanticVersion? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        string? prerelease = null;
        var dashIndex = version.IndexOf('-');
        if (dashIndex > 0)
        {
            prerelease = version[(dashIndex + 1)..];
            version = version[..dashIndex];
        }

        var parts = version.Split('.');
        if (parts.Length < 1 || parts.Length > 4)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var major))
        {
            return false;
        }

        var minor = 0;
        if (parts.Length >= 2 && !int.TryParse(parts[1], out minor))
        {
            return false;
        }

        var patch = 0;
        if (parts.Length >= 3 && !int.TryParse(parts[2], out patch))
        {
            return false;
        }

        result = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0)
        {
            return majorCompare;
        }

        var minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0)
        {
            return minorCompare;
        }

        var patchCompare = Patch.CompareTo(other.Patch);
        if (patchCompare != 0)
        {
            return patchCompare;
        }

        // Prerelease versions have lower precedence than release versions
        if (Prerelease == null && other.Prerelease != null)
        {
            return 1;
        }

        if (Prerelease != null && other.Prerelease == null)
        {
            return -1;
        }

        if (Prerelease != null && other.Prerelease != null)
        {
            return string.Compare(Prerelease, other.Prerelease, StringComparison.OrdinalIgnoreCase);
        }

        return 0;
    }

    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;
    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;
    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;
    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;

    public override string ToString() => Prerelease != null
        ? $"{Major}.{Minor}.{Patch}-{Prerelease}"
        : $"{Major}.{Minor}.{Patch}";
}
