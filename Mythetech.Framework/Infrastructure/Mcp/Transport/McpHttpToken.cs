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
    private const int LockAttempts = 100;
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(20);

    public static string LoadOrCreate(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var turn = TakeTurn(path);

        // Files are only ever moved into place complete, so an empty one is left over from a crash.
        if (TryRead(path, out var existing) && existing.Length > 0)
        {
            RestrictToOwner(path);
            return existing;
        }

        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(temporary, OwnerOnlyOptions(FileMode.CreateNew, FileAccess.Write)))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(token);
            }
            File.Move(temporary, path, overwrite: true);
            return token;
        }
        finally
        {
            TryDelete(temporary);
        }
    }

    // Move without overwrite checks then renames on Unix, so instances starting together hold a side file in turn instead.
    private static FileStream TakeTurn(string path)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return new FileStream($"{path}.lock", OwnerOnlyOptions(FileMode.OpenOrCreate, FileAccess.ReadWrite));
            }
            catch (IOException) when (attempt < LockAttempts)
            {
                Thread.Sleep(LockRetryDelay);
            }
        }
    }

    private static FileStreamOptions OwnerOnlyOptions(FileMode mode, FileAccess access)
    {
        var options = new FileStreamOptions { Mode = mode, Access = access, Share = FileShare.None };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = OwnerOnly;
        }
        return options;
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
