using System;
using System.IO;

namespace AutoFlow.Library.Files;

/// <summary>
/// Provides path validation to prevent path traversal attacks.
/// </summary>
public static class PathValidator
{
    /// <summary>
    /// Validates that the path is within the allowed base directory.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="basePath">The allowed base directory. If null, uses current directory.</param>
    /// <returns>A tuple indicating if the path is valid and the full path.</returns>
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
            // Normalize base path
            var allowedBase = basePath switch
            {
                not null => Path.GetFullPath(basePath),
                null => Directory.GetCurrentDirectory()
            };

            // Get full path of the target
            var fullPath = Path.GetFullPath(Path.Combine(allowedBase, path));

            // Ensure the resolved path is within the allowed base directory
            if (!fullPath.StartsWith(allowedBase, StringComparison.OrdinalIgnoreCase))
            {
                return (false, null, $"Access denied: path '{path}' is outside the allowed directory.");
            }

            // Check for suspicious path components
            var suspiciousPatterns = new[] { "..", "//", "~" };
            var suspiciousPattern = suspiciousPatterns.FirstOrDefault(p => path.Contains(p));
            
            if (suspiciousPattern is not null)
            {
                return (false, null, $"Path contains suspicious pattern: '{suspiciousPattern}'");
            }

            return (true, fullPath, null);
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return (false, null, $"Invalid path: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the allowed base path from configuration or defaults to current directory.
    /// </summary>
    public static string GetAllowedBasePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Directory.GetCurrentDirectory();
        }

        return Path.GetFullPath(configuredPath);
    }
}
