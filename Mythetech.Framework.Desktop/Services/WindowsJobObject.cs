using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// A Windows job object that kills every process in it when it is terminated or when its handle closes,
/// including when the app itself dies.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsJobObject : IDisposable
{
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
    private const int JobObjectExtendedLimitInformation = 9;

    private readonly SafeJobHandle _handle;

    private WindowsJobObject(SafeJobHandle handle) => _handle = handle;

    /// <summary>
    /// Creates an unnamed job whose processes are killed when its handle closes. The handle is not inheritable,
    /// so a process in the job can't keep the job alive.
    /// </summary>
    /// <exception cref="Win32Exception">The job could not be created or configured.</exception>
    public static WindowsJobObject Create()
    {
        var handle = CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new Win32Exception(error);
        }

        var limits = default(JOBOBJECT_EXTENDED_LIMIT_INFORMATION);
        limits.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        if (!SetInformationJobObject(
                handle,
                JobObjectExtendedLimitInformation,
                ref limits,
                (uint)Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
        {
            var error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new Win32Exception(error);
        }

        return new WindowsJobObject(handle);
    }

    /// <summary>
    /// Puts a process that was just started in a new job, then resumes it if it was started suspended, so
    /// nothing it starts runs outside the job. Null when no job could be made; the process then runs without one.
    /// </summary>
    /// <param name="process">The process, just started.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="createJob">Makes the job; null for <see cref="Create"/>.</param>
    /// <exception cref="Win32Exception">
    /// The process could not be resumed. It has been killed, or a warning logged if the kill failed too.
    /// </exception>
    public static WindowsJobObject? JoinNewJob(Process process, ILogger? logger, Func<WindowsJobObject>? createJob = null)
    {
        WindowsJobObject? job = null;
        try
        {
            job = (createJob ?? Create)();
            job.Assign(process.SafeHandle);
        }
        catch (Exception ex)
        {
            job?.Dispose();
            job = null;
            LogQuietly(() => logger?.LogWarning(
                ex, "Could not put process {ProcessId} in a job object, so it runs without one", process.Id));
        }

        if (!process.StartInfo.StartSuspended)
            return job;

        try
        {
            process.SafeHandle.Resume();
            return job;
        }
        catch
        {
            // A process left suspended would never run or exit; closing its job kills it.
            if (job is not null)
            {
                job.Dispose();
            }
            else
            {
                try
                {
                    process.Kill();
                }
                catch (Exception killFailure)
                {
                    LogQuietly(() => logger?.LogWarning(
                        killFailure, "Could not kill process {ProcessId}, which failed to resume", process.Id));
                }
            }
            throw;
        }
    }

    // A logger that throws must not keep a suspended process from being resumed or killed.
    private static void LogQuietly(Action log)
    {
        try
        {
            log();
        }
        catch (Exception)
        {
            // There is nowhere left to report it.
        }
    }

    /// <summary>
    /// Puts <paramref name="process"/> in the job. Every process it starts from then on is in the job too.
    /// </summary>
    /// <exception cref="Win32Exception">The process could not be assigned.</exception>
    public void Assign(SafeProcessHandle process)
    {
        if (!AssignProcessToJobObject(_handle, process))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    /// <summary>
    /// Ends every process in the job with <paramref name="exitCode"/>. A closed job has already killed its processes,
    /// so this does nothing once the job is disposed.
    /// </summary>
    /// <exception cref="Win32Exception">The job could not be terminated.</exception>
    public void Terminate(uint exitCode)
    {
        if (_handle.IsClosed)
            return;

        if (!TerminateJobObject(_handle, exitCode))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    /// <summary>
    /// Closes the job's handle, which kills every process still in it.
    /// </summary>
    public void Dispose() => _handle.Dispose();

    // winnt.h: LARGE_INTEGER is long, SIZE_T and ULONG_PTR are nuint, DWORD is uint.
    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }

    private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeJobHandle() : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle() => CloseHandle(handle);
    }

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeJobHandle CreateJobObjectW(IntPtr lpJobAttributes, IntPtr lpName);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(
        SafeJobHandle hJob,
        int jobObjectInformationClass,
        ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInformation,
        uint cbJobObjectInformationLength);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(SafeJobHandle hJob, SafeProcessHandle hProcess);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateJobObject(SafeJobHandle hJob, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);
}
