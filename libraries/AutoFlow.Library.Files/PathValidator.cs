using System;
using System.IO;

namespace AutoFlow.Library.Files;

public static class PathValidator
{
    public static (bool IsValid, string? FullPath, string? ErrorMessage) ValidatePath(
        string path,
        string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, null, "Path cannot be empty.");
        }

        try
        {
            var decodedPath = DecodePath(path);

            if (ContainsSuspiciousPatterns(decodedPath))
            {
                return (false, null, "Path contains suspicious traversal patterns and may resolve outside the allowed directory.");
            }

            var allowedBase = basePath switch
            {
                not null => Path.GetFullPath(basePath),
                null => Directory.GetCurrentDirectory()
            };

            string fullPath;

            if (Path.IsPathRooted(decodedPath))
            {
                fullPath = Path.GetFullPath(decodedPath);
                if (!IsWithinDirectory(fullPath, allowedBase))
                {
                    return (false, null, $"Access denied: absolute path is outside the allowed directory.");
                }
            }
            else
            {
#pragma warning disable CA5360
                var resolvedPath = Path.Combine(allowedBase, decodedPath);
#pragma warning restore CA5360
                fullPath = Path.GetFullPath(resolvedPath);
            }

            if (!IsWithinDirectory(fullPath, allowedBase))
            {
                return (false, null, $"Access denied: path is outside the allowed directory.");
            }

            return (true, fullPath, null);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return (false, null, $"Invalid path: {ex.Message}");
        }
    }

    private static string DecodePath(string path)
    {
        var decoded = path;

        try
        {
            decoded = Uri.UnescapeDataString(path);
        }
        catch
        {
        }

        decoded = decoded.Replace('\\', '/');

        return decoded;
    }

    private static bool ContainsSuspiciousPatterns(string path)
    {
        var normalized = path.ToLowerInvariant();

        var patterns = new[]
        {
            "..",
            "~",
            "/./",
            "//"
        };

        foreach (var pattern in patterns)
        {
            if (normalized.Contains(pattern))
                return true;
        }

        if (normalized.StartsWith("./") || normalized.StartsWith("~/"))
            return true;

        if (normalized.Contains("%2e") || normalized.Contains("%252e"))
            return true;

        return false;
    }

    private static bool IsWithinDirectory(string fullPath, string baseDirectory)
    {
        var normalizedFull = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedBase = Path.GetFullPath(baseDirectory).TrimEnd(Path.DirectorySeparatorChar);

        return normalizedFull.StartsWith(normalizedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               normalizedFull.Equals(normalizedBase, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetAllowedBasePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Directory.GetCurrentDirectory();
        }

        return Path.GetFullPath(configuredPath);
    }
}
