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
    private const int Attempts = 50;

    public static string LoadOrCreate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (TryRead(path) is { } existing)
            {
                RestrictToOwner(path);
                return existing;
            }

            var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
            try
            {
                var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = OwnerOnly;
                }
                using (var stream = new FileStream(path, options))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(token);
                }
                return token;
            }
            catch (IOException)
            {
                // Another instance created the file first; read its token once it has been written.
                Thread.Sleep(10);
            }
        }

        throw new IOException("The MCP token file exists but stayed empty.");
    }

    private static string? TryRead(string path)
    {
        try
        {
            var text = File.ReadAllText(path).Trim();
            return text.Length > 0 ? text : null;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or IOException)
        {
            return null;
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
