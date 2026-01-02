using System.Runtime.InteropServices;

namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// PInvoke declarations for native secret storage APIs across platforms
/// </summary>
internal static class NativeSecretManagerInterop
{
    #region macOS Security Framework

    private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    internal const int errSecSuccess = 0;
    internal const int errSecItemNotFound = -25300;
    internal const int errSecAuthFailed = -25293;
    internal const int errSecDuplicateItem = -25299;

    [DllImport(SecurityFramework)]
    internal static extern int SecKeychainAddGenericPassword(
        IntPtr keychain,           // NULL for default keychain
        uint serviceNameLength,
        string serviceName,
        uint accountNameLength,
        string accountName,
        uint passwordLength,
        byte[] passwordData,
        out IntPtr itemRef);

    [DllImport(SecurityFramework)]
    internal static extern int SecKeychainFindGenericPassword(
        IntPtr keychainOrArray,    // NULL for default keychain
        uint serviceNameLength,
        string serviceName,
        uint accountNameLength,
        string accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        out IntPtr itemRef);

    [DllImport(SecurityFramework)]
    internal static extern int SecKeychainItemDelete(IntPtr itemRef);

    [DllImport(SecurityFramework)]
    internal static extern int SecKeychainItemModifyAttributesAndData(
        IntPtr itemRef,
        IntPtr attrList,           // NULL to not modify attributes
        uint length,
        byte[] data);

    [DllImport(SecurityFramework)]
    internal static extern int SecKeychainItemFreeContent(
        IntPtr attrList,           // NULL if not freeing attributes
        IntPtr data);

    [DllImport(CoreFoundation)]
    internal static extern void CFRelease(IntPtr cf);

    #endregion

    #region Windows Credential Manager

    private const string Advapi32 = "advapi32.dll";

    internal const int CRED_TYPE_GENERIC = 1;
    internal const int CRED_PERSIST_LOCAL_MACHINE = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CredRead(
        string target,
        uint type,
        uint reservedFlag,
        out IntPtr credential);

    [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CredWrite(
        ref CREDENTIAL credential,
        uint flags);

    [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CredDelete(
        string target,
        uint type,
        uint flags);

    [DllImport(Advapi32, SetLastError = true)]
    internal static extern void CredFree(IntPtr buffer);

    #endregion

    #region Linux libsecret

    // Note: libsecret PInvoke can be complex due to GLib dependencies.
    // Using CLI fallback with 'secret-tool' may be more reliable.

    private const string LibSecret = "libsecret-1.so.0";
    private const string GLib = "libglib-2.0.so.0";

    [DllImport(LibSecret)]
    internal static extern IntPtr secret_password_lookup_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        string attribute1_name,
        string attribute1_value,
        IntPtr terminator);

    [DllImport(LibSecret)]
    internal static extern bool secret_password_store_sync(
        IntPtr schema,
        string collection,
        string label,
        string password,
        IntPtr cancellable,
        out IntPtr error,
        string attribute1_name,
        string attribute1_value,
        IntPtr terminator);

    [DllImport(LibSecret)]
    internal static extern bool secret_password_clear_sync(
        IntPtr schema,
        IntPtr cancellable,
        out IntPtr error,
        string attribute1_name,
        string attribute1_value,
        IntPtr terminator);

    [DllImport(LibSecret)]
    internal static extern void secret_password_free(IntPtr password);

    [DllImport(GLib)]
    internal static extern void g_error_free(IntPtr error);

    #endregion
}
