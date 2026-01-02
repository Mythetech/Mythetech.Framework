using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Mythetech.Framework.Infrastructure.Secrets;

namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// Native OS secret manager implementation using platform-specific APIs.
/// - macOS: Keychain Services
/// - Windows: Credential Manager
/// - Linux: libsecret (with secret-tool CLI fallback)
/// </summary>
public class NativeSecretManager : ISecretManager, ISecretWriter
{
    private readonly string _serviceName;
    private readonly bool _useLinuxCliFallback;

    /// <summary>
    /// Creates a new NativeSecretManager with the specified service name.
    /// </summary>
    /// <param name="serviceName">Service name for secret storage scope. Defaults to "mythetech/{entryAssemblyName}".</param>
    /// <param name="useLinuxCliFallback">Use secret-tool CLI on Linux instead of libsecret PInvoke.</param>
    public NativeSecretManager(string? serviceName = null, bool useLinuxCliFallback = true)
    {
        _serviceName = serviceName ?? GetDefaultServiceName();
        _useLinuxCliFallback = useLinuxCliFallback;
    }

    /// <inheritdoc />
    public string Name
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "macOS Keychain";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows Credential Manager";
            return "Linux Secret Service";
        }
    }

    /// <inheritdoc />
    public Task<SecretOperationResult<Secret>> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(SecretOperationResult<Secret>.Fail(
                "Key cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey));
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Task.FromResult(GetSecretMacOS(key));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Task.FromResult(GetSecretWindows(key));
            }

            // Linux
            if (_useLinuxCliFallback)
            {
                return GetSecretLinuxCliAsync(key, cancellationToken);
            }
            return Task.FromResult(GetSecretLinuxLibsecret(key));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SecretOperationResult<Secret>.Fail(
                $"Failed to get secret: {ex.Message}",
                SecretOperationErrorKind.Unknown));
        }
    }

    /// <inheritdoc />
    public Task<SecretOperationResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Task.FromResult(TestConnectionMacOS());
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Task.FromResult(TestConnectionWindows());
            }

            // Linux
            if (_useLinuxCliFallback)
            {
                return TestConnectionLinuxCliAsync(cancellationToken);
            }
            return Task.FromResult(TestConnectionLinuxLibsecret());
        }
        catch (Exception ex)
        {
            return Task.FromResult(SecretOperationResult.Fail(
                $"Failed to test connection: {ex.Message}",
                SecretOperationErrorKind.Unknown));
        }
    }

    /// <inheritdoc />
    public Task<SecretOperationResult> SetSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(SecretOperationResult.Fail(
                "Key cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey));
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Task.FromResult(SetSecretMacOS(key, value));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Task.FromResult(SetSecretWindows(key, value));
            }

            // Linux
            if (_useLinuxCliFallback)
            {
                return SetSecretLinuxCliAsync(key, value, cancellationToken);
            }
            return Task.FromResult(SetSecretLinuxLibsecret(key, value));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SecretOperationResult.Fail(
                $"Failed to set secret: {ex.Message}",
                SecretOperationErrorKind.Unknown));
        }
    }

    /// <inheritdoc />
    public Task<SecretOperationResult> DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(SecretOperationResult.Fail(
                "Key cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey));
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Task.FromResult(DeleteSecretMacOS(key));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Task.FromResult(DeleteSecretWindows(key));
            }

            // Linux
            if (_useLinuxCliFallback)
            {
                return DeleteSecretLinuxCliAsync(key, cancellationToken);
            }
            return Task.FromResult(DeleteSecretLinuxLibsecret(key));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SecretOperationResult.Fail(
                $"Failed to delete secret: {ex.Message}",
                SecretOperationErrorKind.Unknown));
        }
    }

    #region macOS Implementation

    private SecretOperationResult<Secret> GetSecretMacOS(string key)
    {
        var status = NativeSecretManagerInterop.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)_serviceName.Length, _serviceName,
            (uint)key.Length, key,
            out var passwordLength,
            out var passwordData,
            out var itemRef);

        if (status == NativeSecretManagerInterop.errSecItemNotFound)
        {
            return SecretOperationResult<Secret>.Fail(
                $"Secret '{key}' not found in Keychain.",
                SecretOperationErrorKind.NotFound);
        }

        if (status == NativeSecretManagerInterop.errSecAuthFailed)
        {
            return SecretOperationResult<Secret>.Fail(
                "Keychain access denied. Please unlock your Keychain.",
                SecretOperationErrorKind.AccessDenied);
        }

        if (status != NativeSecretManagerInterop.errSecSuccess)
        {
            return SecretOperationResult<Secret>.Fail(
                $"Keychain error: {status}",
                SecretOperationErrorKind.Unknown);
        }

        try
        {
            var passwordBytes = new byte[passwordLength];
            Marshal.Copy(passwordData, passwordBytes, 0, (int)passwordLength);
            var value = Encoding.UTF8.GetString(passwordBytes);

            return SecretOperationResult<Secret>.Ok(new Secret
            {
                Key = key,
                Value = value,
                Name = key
            });
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                NativeSecretManagerInterop.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }
            if (itemRef != IntPtr.Zero)
            {
                NativeSecretManagerInterop.CFRelease(itemRef);
            }
        }
    }

    private SecretOperationResult SetSecretMacOS(string key, string value)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(value);

        // First try to find existing item
        var findStatus = NativeSecretManagerInterop.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)_serviceName.Length, _serviceName,
            (uint)key.Length, key,
            out _, out var passwordData, out var itemRef);

        if (passwordData != IntPtr.Zero)
        {
            NativeSecretManagerInterop.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
        }

        if (findStatus == NativeSecretManagerInterop.errSecSuccess && itemRef != IntPtr.Zero)
        {
            // Update existing item
            var updateStatus = NativeSecretManagerInterop.SecKeychainItemModifyAttributesAndData(
                itemRef,
                IntPtr.Zero,
                (uint)passwordBytes.Length,
                passwordBytes);

            NativeSecretManagerInterop.CFRelease(itemRef);

            if (updateStatus != NativeSecretManagerInterop.errSecSuccess)
            {
                return SecretOperationResult.Fail(
                    $"Failed to update Keychain item: {updateStatus}",
                    SecretOperationErrorKind.Unknown);
            }

            return SecretOperationResult.Ok();
        }

        // Add new item
        var addStatus = NativeSecretManagerInterop.SecKeychainAddGenericPassword(
            IntPtr.Zero,
            (uint)_serviceName.Length, _serviceName,
            (uint)key.Length, key,
            (uint)passwordBytes.Length, passwordBytes,
            out var newItemRef);

        if (newItemRef != IntPtr.Zero)
        {
            NativeSecretManagerInterop.CFRelease(newItemRef);
        }

        if (addStatus == NativeSecretManagerInterop.errSecAuthFailed)
        {
            return SecretOperationResult.Fail(
                "Keychain access denied. Please unlock your Keychain.",
                SecretOperationErrorKind.AccessDenied);
        }

        if (addStatus != NativeSecretManagerInterop.errSecSuccess)
        {
            return SecretOperationResult.Fail(
                $"Failed to add Keychain item: {addStatus}",
                SecretOperationErrorKind.Unknown);
        }

        return SecretOperationResult.Ok();
    }

    private SecretOperationResult DeleteSecretMacOS(string key)
    {
        var findStatus = NativeSecretManagerInterop.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)_serviceName.Length, _serviceName,
            (uint)key.Length, key,
            out _, out var passwordData, out var itemRef);

        if (passwordData != IntPtr.Zero)
        {
            NativeSecretManagerInterop.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
        }

        if (findStatus == NativeSecretManagerInterop.errSecItemNotFound)
        {
            return SecretOperationResult.Fail(
                $"Secret '{key}' not found in Keychain.",
                SecretOperationErrorKind.NotFound);
        }

        if (findStatus != NativeSecretManagerInterop.errSecSuccess || itemRef == IntPtr.Zero)
        {
            return SecretOperationResult.Fail(
                $"Failed to find Keychain item: {findStatus}",
                SecretOperationErrorKind.Unknown);
        }

        var deleteStatus = NativeSecretManagerInterop.SecKeychainItemDelete(itemRef);
        NativeSecretManagerInterop.CFRelease(itemRef);

        if (deleteStatus != NativeSecretManagerInterop.errSecSuccess)
        {
            return SecretOperationResult.Fail(
                $"Failed to delete Keychain item: {deleteStatus}",
                SecretOperationErrorKind.Unknown);
        }

        return SecretOperationResult.Ok();
    }

    private SecretOperationResult TestConnectionMacOS()
    {
        // Try to access the keychain with a non-existent item to verify it's available
        var status = NativeSecretManagerInterop.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)_serviceName.Length, _serviceName,
            8, "__test__",
            out _, out var passwordData, out var itemRef);

        if (passwordData != IntPtr.Zero)
        {
            NativeSecretManagerInterop.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
        }
        if (itemRef != IntPtr.Zero)
        {
            NativeSecretManagerInterop.CFRelease(itemRef);
        }

        // errSecItemNotFound is expected and means keychain is working
        if (status == NativeSecretManagerInterop.errSecItemNotFound ||
            status == NativeSecretManagerInterop.errSecSuccess)
        {
            return SecretOperationResult.Ok();
        }

        if (status == NativeSecretManagerInterop.errSecAuthFailed)
        {
            return SecretOperationResult.Fail(
                "Keychain is locked. Please unlock your Keychain.",
                SecretOperationErrorKind.AccessDenied);
        }

        return SecretOperationResult.Fail(
            $"Keychain error: {status}",
            SecretOperationErrorKind.ConnectionFailed);
    }

    #endregion

    #region Windows Implementation

    private SecretOperationResult<Secret> GetSecretWindows(string key)
    {
        var targetName = GetWindowsTargetName(key);

        if (!NativeSecretManagerInterop.CredRead(targetName, NativeSecretManagerInterop.CRED_TYPE_GENERIC, 0, out var credPtr))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == 1168) // ERROR_NOT_FOUND
            {
                return SecretOperationResult<Secret>.Fail(
                    $"Secret '{key}' not found in Credential Manager.",
                    SecretOperationErrorKind.NotFound);
            }

            return SecretOperationResult<Secret>.Fail(
                $"Credential Manager error: {error}",
                SecretOperationErrorKind.Unknown);
        }

        try
        {
            var cred = Marshal.PtrToStructure<NativeSecretManagerInterop.CREDENTIAL>(credPtr);
            var passwordBytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, passwordBytes, 0, (int)cred.CredentialBlobSize);
            var value = Encoding.Unicode.GetString(passwordBytes);

            return SecretOperationResult<Secret>.Ok(new Secret
            {
                Key = key,
                Value = value,
                Name = key
            });
        }
        finally
        {
            NativeSecretManagerInterop.CredFree(credPtr);
        }
    }

    private SecretOperationResult SetSecretWindows(string key, string value)
    {
        var targetName = GetWindowsTargetName(key);
        var passwordBytes = Encoding.Unicode.GetBytes(value);

        var cred = new NativeSecretManagerInterop.CREDENTIAL
        {
            Type = NativeSecretManagerInterop.CRED_TYPE_GENERIC,
            TargetName = targetName,
            CredentialBlobSize = (uint)passwordBytes.Length,
            CredentialBlob = Marshal.AllocHGlobal(passwordBytes.Length),
            Persist = NativeSecretManagerInterop.CRED_PERSIST_LOCAL_MACHINE,
            UserName = key
        };

        try
        {
            Marshal.Copy(passwordBytes, 0, cred.CredentialBlob, passwordBytes.Length);

            if (!NativeSecretManagerInterop.CredWrite(ref cred, 0))
            {
                var error = Marshal.GetLastWin32Error();
                return SecretOperationResult.Fail(
                    $"Failed to write credential: {error}",
                    SecretOperationErrorKind.Unknown);
            }

            return SecretOperationResult.Ok();
        }
        finally
        {
            Marshal.FreeHGlobal(cred.CredentialBlob);
        }
    }

    private SecretOperationResult DeleteSecretWindows(string key)
    {
        var targetName = GetWindowsTargetName(key);

        if (!NativeSecretManagerInterop.CredDelete(targetName, NativeSecretManagerInterop.CRED_TYPE_GENERIC, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error == 1168) // ERROR_NOT_FOUND
            {
                return SecretOperationResult.Fail(
                    $"Secret '{key}' not found in Credential Manager.",
                    SecretOperationErrorKind.NotFound);
            }

            return SecretOperationResult.Fail(
                $"Failed to delete credential: {error}",
                SecretOperationErrorKind.Unknown);
        }

        return SecretOperationResult.Ok();
    }

    private SecretOperationResult TestConnectionWindows()
    {
        // Try to read a non-existent credential to verify the API is available
        var targetName = GetWindowsTargetName("__test__");
        NativeSecretManagerInterop.CredRead(targetName, NativeSecretManagerInterop.CRED_TYPE_GENERIC, 0, out var credPtr);

        if (credPtr != IntPtr.Zero)
        {
            NativeSecretManagerInterop.CredFree(credPtr);
        }

        // If we got here without an exception, the API is available
        return SecretOperationResult.Ok();
    }

    private string GetWindowsTargetName(string key) => $"{_serviceName}/{key}";

    #endregion

    #region Linux Implementation (CLI fallback)

    private async Task<SecretOperationResult<Secret>> GetSecretLinuxCliAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteSecretToolAsync($"lookup service {_serviceName} key {key}", cancellationToken);

            if (result.ExitCode != 0)
            {
                if (result.StdErr.Contains("No such secret"))
                {
                    return SecretOperationResult<Secret>.Fail(
                        $"Secret '{key}' not found.",
                        SecretOperationErrorKind.NotFound);
                }

                return SecretOperationResult<Secret>.Fail(
                    $"secret-tool error: {result.StdErr}",
                    SecretOperationErrorKind.Unknown);
            }

            return SecretOperationResult<Secret>.Ok(new Secret
            {
                Key = key,
                Value = result.StdOut.TrimEnd('\n'),
                Name = key
            });
        }
        catch (Exception ex) when (ex.Message.Contains("secret-tool"))
        {
            return SecretOperationResult<Secret>.Fail(
                "secret-tool is not installed. Please install libsecret-tools.",
                SecretOperationErrorKind.ConnectionFailed);
        }
    }

    private async Task<SecretOperationResult> SetSecretLinuxCliAsync(string key, string value, CancellationToken cancellationToken)
    {
        try
        {
            // secret-tool store requires input from stdin for the password
            var result = await ExecuteSecretToolWithInputAsync(
                $"store --label=\"{key}\" service {_serviceName} key {key}",
                value,
                cancellationToken);

            if (result.ExitCode != 0)
            {
                return SecretOperationResult.Fail(
                    $"secret-tool error: {result.StdErr}",
                    SecretOperationErrorKind.Unknown);
            }

            return SecretOperationResult.Ok();
        }
        catch (Exception ex) when (ex.Message.Contains("secret-tool"))
        {
            return SecretOperationResult.Fail(
                "secret-tool is not installed. Please install libsecret-tools.",
                SecretOperationErrorKind.ConnectionFailed);
        }
    }

    private async Task<SecretOperationResult> DeleteSecretLinuxCliAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecuteSecretToolAsync($"clear service {_serviceName} key {key}", cancellationToken);

            if (result.ExitCode != 0)
            {
                return SecretOperationResult.Fail(
                    $"secret-tool error: {result.StdErr}",
                    SecretOperationErrorKind.Unknown);
            }

            return SecretOperationResult.Ok();
        }
        catch (Exception ex) when (ex.Message.Contains("secret-tool"))
        {
            return SecretOperationResult.Fail(
                "secret-tool is not installed. Please install libsecret-tools.",
                SecretOperationErrorKind.ConnectionFailed);
        }
    }

    private async Task<SecretOperationResult> TestConnectionLinuxCliAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Just try to run secret-tool to see if it's available
            var result = await ExecuteSecretToolAsync("--version", cancellationToken);

            if (result.ExitCode != 0 && !result.StdOut.Contains("secret-tool"))
            {
                return SecretOperationResult.Fail(
                    "secret-tool is not installed or not working properly.",
                    SecretOperationErrorKind.ConnectionFailed);
            }

            return SecretOperationResult.Ok();
        }
        catch
        {
            return SecretOperationResult.Fail(
                "secret-tool is not installed. Please install libsecret-tools.",
                SecretOperationErrorKind.ConnectionFailed);
        }
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> ExecuteSecretToolAsync(
        string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "secret-tool",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, stdout, stderr);
    }

    private async Task<(int ExitCode, string StdOut, string StdErr)> ExecuteSecretToolWithInputAsync(
        string arguments, string input, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "secret-tool",
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, stdout, stderr);
    }

    #endregion

    #region Linux Implementation (libsecret PInvoke - placeholder)

    private SecretOperationResult<Secret> GetSecretLinuxLibsecret(string key)
    {
        // libsecret PInvoke implementation would go here
        // For now, return not supported as CLI fallback is more reliable
        return SecretOperationResult<Secret>.Fail(
            "libsecret PInvoke is not yet implemented. Use CLI fallback instead.",
            SecretOperationErrorKind.NotSupported);
    }

    private SecretOperationResult SetSecretLinuxLibsecret(string key, string value)
    {
        return SecretOperationResult.Fail(
            "libsecret PInvoke is not yet implemented. Use CLI fallback instead.",
            SecretOperationErrorKind.NotSupported);
    }

    private SecretOperationResult DeleteSecretLinuxLibsecret(string key)
    {
        return SecretOperationResult.Fail(
            "libsecret PInvoke is not yet implemented. Use CLI fallback instead.",
            SecretOperationErrorKind.NotSupported);
    }

    private SecretOperationResult TestConnectionLinuxLibsecret()
    {
        return SecretOperationResult.Fail(
            "libsecret PInvoke is not yet implemented. Use CLI fallback instead.",
            SecretOperationErrorKind.NotSupported);
    }

    #endregion

    private static string GetDefaultServiceName()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "app";
        return $"mythetech/{assemblyName}";
    }
}
