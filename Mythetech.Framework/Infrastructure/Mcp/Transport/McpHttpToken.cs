using System.Buffers.Text;
using System.Security.Cryptography;

namespace Mythetech.Framework.Infrastructure.Mcp.Transport;

/// <summary>
/// The HTTP transport's bearer token, kept in one owner-only file so every instance on the machine and every
/// configured client agree on it across restarts.
/// </summary>
internal static class McpHttpToken
{
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const int Attempts = 10;

    public static string LoadOrCreate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (TryRead(path, out var existing))
            {
                if (existing.Length > 0)
                {
                    RestrictToOwner(path);
                    return existing;
                }

                // Files are only ever moved into place complete, so an empty one is left over from a crash.
                TryDelete(path);
            }

            var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
            var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
            try
            {
                var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = OwnerOnly;
                }
                using (var stream = new FileStream(temporary, options))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(token);
                }
                File.Move(temporary, path, overwrite: false);
                return token;
            }
            catch (IOException)
            {
                // Another instance moved its token into place first; the next pass reads it.
                TryDelete(temporary);
            }
        }

        throw new IOException("The MCP token file could not be created or read.");
    }

    private static bool TryRead(string path, out string token)
    {
        try
        {
            token = File.ReadAllText(path).Trim();
            return true;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            token = string.Empty;
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    private static void RestrictToOwner(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        if ((File.GetUnixFileMode(path) & ~OwnerOnly) != 0)
        {
            File.SetUnixFileMode(path, OwnerOnly);
        }
    }

    public static bool Matches(string expected, string? authorizationHeader)
    {
        const string scheme = "Bearer ";
        if (authorizationHeader is null || !authorizationHeader.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var presented = System.Text.Encoding.UTF8.GetBytes(authorizationHeader[scheme.Length..].Trim());
        return CryptographicOperations.FixedTimeEquals(presented, System.Text.Encoding.UTF8.GetBytes(expected));
    }
}
