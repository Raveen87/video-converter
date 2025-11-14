namespace VideoConverter.Api.Features.Integrations.Ffmpeg;

internal static class NativeMethods
{
    // Windows
    internal const int ProcessSuspendResumeAccess = 0x0800;

    // Unix
    internal const int SigStop = 19;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
    internal static extern int NativeKill(int processId, int signal);
}
