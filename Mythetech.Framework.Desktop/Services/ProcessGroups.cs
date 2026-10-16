using System.Runtime.InteropServices;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// POSIX process group queries through libc. Unix only.
/// </summary>
internal static class ProcessGroups
{
    private const int ESRCH = 3;

    /// <summary>
    /// Whether no process is left in the group. A group the caller may not signal (EPERM) still has members.
    /// </summary>
    public static bool IsEmpty(int processGroupId)
    {
        if (kill(-processGroupId, 0) == 0)
            return false;

        return Marshal.GetLastWin32Error() == ESRCH;
    }

    /// <summary>
    /// The process group of <paramref name="pid"/>, or -1 when no such process exists.
    /// </summary>
    public static int GroupOf(int pid) => getpgid(pid);

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    [DllImport("libc", SetLastError = true)]
    private static extern int getpgid(int pid);
}
