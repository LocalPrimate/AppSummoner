using System.Diagnostics;
using System.Runtime.InteropServices;
using AppSummoner.Data;
using AppSummoner.Domain.Loggers;

// ReSharper disable InconsistentNaming

namespace AppSummoner.Domain.Handlers;

public static class WindowHandler
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);

    private enum GetWindowType : uint
    {
         GW_HWNDFIRST = 0,
         GW_HWNDLAST = 1,
         GW_HWNDNEXT = 2,
         GW_HWNDPREV = 3,
         GW_OWNER = 4,
         GW_CHILD = 5,
         GW_ENABLEDPOPUP = 6
    }

    private const int SW_HIDE = 0;
    private const int SW_SHOWMINIMIZED = 2;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    private static readonly IntPtr HWND_TOP = new IntPtr(0);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const byte VK_MENU = 0x12; // Alt key
    private const uint KEYEVENTF_KEYUP = 0x0002;


    //*** PUBLIC METHODS
    //*******************************************************************

    // TODO: Don't just check for "Contains", but make a direct comparison. Determine if that works in 100% of cases or not.
    public static bool IsProcessInForeground(string processName)
    {
        var foregroundProcess = WinProcess.GetForegroundWindowProcess();
        return foregroundProcess is not null 
               && foregroundProcess.ProcessName.Contains(processName, StringComparison.CurrentCultureIgnoreCase);
    }

    // TODO: Don't just check for "Contains", but make a direct comparison. Determine if that works in 100% of cases or not.
    public static bool IsProcessRunning(string processName)
    {
        Process[] processList = Process.GetProcesses();
        return processList.Any(process => process.ProcessName.Contains(processName, StringComparison.CurrentCultureIgnoreCase));
    }

    public static void MinimizeForegroundWindow()
    {
        IntPtr foregroundWindowHandle = WinProcess.GetForegroundWindowHandle();

        if (foregroundWindowHandle == IntPtr.Zero) return;

        ShowWindow(foregroundWindowHandle, SW_SHOWMINIMIZED);
    }

    public static void HideForegroundWindow()
    {
        IntPtr foregroundWindowHandle = WinProcess.GetForegroundWindowHandle();

        if (foregroundWindowHandle == IntPtr.Zero) return;

        ShowWindow(foregroundWindowHandle, SW_HIDE);
    }

    public static void FocusWindow(string processName)
    {
        Process[] processList = Process.GetProcesses();
        foreach (Process process in processList)
        {
            if (!process.ProcessName.Contains(processName, StringComparison.CurrentCultureIgnoreCase)) continue;
            // SetForegroundWindow(process.MainWindowHandle);
            // ShowWindows(process.Id, SW_SHOW);
            
            // Step 1. Identify the main window
            GetMainWindowHandle(process.Id);
            
            return;
        }
    }

    public static void LaunchApplication(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath });
            // TODO: Add delay before this method can be called again for the same filepath
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
        }
    }

    
    //*** PRIVATE METHODS
    //*******************************************************************

    private static void GetMainWindowHandle(int processId)
    {
        IEnumerable<nint>? windowHandles = WinProcess.EnumerateProcessWindowHandles(processId);
        if (windowHandles is null) return;

        foreach (nint windowHandle in windowHandles)
        {
            // Only process top-level windows with no owner that are visible
            if (GetWindow(windowHandle, GetWindowType.GW_OWNER) == 0 && IsWindowVisible(windowHandle))
            {
                Console.WriteLine($"Found main window handle: {windowHandle}");
                ForceForegroundWindow(windowHandle);
                return;
            }
        }
    }

    private static void ForceForegroundWindow(IntPtr hWnd)
    {
        // Only restore if window is minimized (iconic)
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        // Simulate Alt key press to allow SetForegroundWindow to succeed
        // This is a documented workaround for SetForegroundWindow restrictions
        keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);

        // Bring window to top and set as foreground
        BringWindowToTop(hWnd);
        SetWindowPos(hWnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        SetForegroundWindow(hWnd);

        // Release Alt key
        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
    
    private static void ShowWindows(int processId, int nCmdShow)
    {
        IEnumerable<nint>? windowHandles = WinProcess.EnumerateProcessWindowHandles(processId);
        
        if (windowHandles is null) return;

        foreach (nint windowHandle in windowHandles)
        {
            ShowWindow(windowHandle, nCmdShow);
        }
    }
}
