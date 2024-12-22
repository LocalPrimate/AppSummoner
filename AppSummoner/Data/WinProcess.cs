using System.Diagnostics;
using System.Runtime.InteropServices;
using AppSummoner.Domain.Loggers;

namespace AppSummoner.Data;

public static class WinProcess
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId); 

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    public static Process? GetForegroundWindowProcess()
    {
        try
        {
            nint fgHandle = GetForegroundWindow();
            _ = GetWindowThreadProcessId(fgHandle, out var lpdwProcessId);
            Process fgProcess = Process.GetProcessById(Convert.ToInt32(lpdwProcessId));
            return fgProcess;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex, "failed to get foreground window process");
            return null;
        }
    }

    public static IEnumerable<IntPtr>? EnumerateProcessWindowHandles(int processId)
    {
        try
        {
            List<IntPtr> handles = [];
            ProcessThreadCollection threads = Process.GetProcessById(processId).Threads;
            foreach (ProcessThread thread in threads)
            {
                EnumThreadWindows(
                    thread.Id,
                    (threadWindowHandle, lParam) =>
                        {
                            handles.Add(threadWindowHandle); 
                            return true;
                        },
                    IntPtr.Zero);
            }
            return handles;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex, "failed to get process child window handles");
            return null;
        }
    }
}
