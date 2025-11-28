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

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

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

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();


    //*** PUBLIC METHODS
    //*******************************************************************

    // TODO: Don't just check for "Contains", but make a direct comparison. Determine if that works in 100% of cases or not.
    public static bool IsProcessInForeground(string processName)
    {
        var foregroundProcess = WinProcess.GetForegroundWindowProcess();
        if (foregroundProcess is null)
        {
            Console.WriteLine($"[WindowHandler] IsProcessInForeground({processName}): No foreground process");
            return false;
        }

        Console.WriteLine($"[WindowHandler] Foreground process: {foregroundProcess.ProcessName}");

        if (!foregroundProcess.ProcessName.Contains(processName, StringComparison.CurrentCultureIgnoreCase))
        {
            Console.WriteLine($"[WindowHandler] Process name doesn't match");
            return false;
        }

        // Also check that the foreground window is actually visible and not minimized
        IntPtr foregroundWindowHandle = WinProcess.GetForegroundWindowHandle();
        if (foregroundWindowHandle == IntPtr.Zero)
        {
            Console.WriteLine($"[WindowHandler] No foreground window handle");
            return false;
        }

        bool isMinimized = IsIconic(foregroundWindowHandle);
        bool isVisible = IsWindowVisible(foregroundWindowHandle);
        Console.WriteLine($"[WindowHandler] Window state: Minimized={isMinimized}, Visible={isVisible}");

        // If the window is minimized, it's not really "in foreground" for our purposes
        if (isMinimized)
        {
            Console.WriteLine($"[WindowHandler] Window is minimized, returning false");
            return false;
        }

        // If the window is not visible, it's not in foreground
        if (!isVisible)
        {
            Console.WriteLine($"[WindowHandler] Window is not visible, returning false");
            return false;
        }

        Console.WriteLine($"[WindowHandler] Returning true");
        return true;
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

        // Find the next window in Z-order that should receive focus (before minimizing)
        IntPtr nextFocusWindow = FindNextFocusableWindow(foregroundWindowHandle);

        ShowWindow(foregroundWindowHandle, SW_SHOWMINIMIZED);

        // Small delay to let Windows process the minimize
        System.Threading.Thread.Sleep(50);

        // Switch focus to the previously identified window
        if (nextFocusWindow != IntPtr.Zero)
        {
            ForceFocusWindow(nextFocusWindow);
        }
    }

    public static void HideForegroundWindow()
    {
        IntPtr foregroundWindowHandle = WinProcess.GetForegroundWindowHandle();

        if (foregroundWindowHandle == IntPtr.Zero) return;

        // Find the next window in Z-order that should receive focus (before hiding)
        IntPtr nextFocusWindow = FindNextFocusableWindow(foregroundWindowHandle);

        ShowWindow(foregroundWindowHandle, SW_HIDE);

        // Small delay to let Windows process the hide
        System.Threading.Thread.Sleep(50);

        // Switch focus to the previously identified window
        if (nextFocusWindow != IntPtr.Zero)
        {
            ForceFocusWindow(nextFocusWindow);
        }
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
            // Use cmd.exe with START command to break parent-child process relationship
            // This ensures the launched app continues running even after AppSummoner exits
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C start \"\" \"{filePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
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
        // Use AttachThreadInput for more reliable focusing
        IntPtr foregroundWindow = GetForegroundWindow();
        uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
        uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);

        bool attached = false;
        try
        {
            // Attach the foreground thread to the target thread's input queue
            if (foregroundThreadId != targetThreadId && foregroundThreadId != 0 && targetThreadId != 0)
            {
                attached = AttachThreadInput(foregroundThreadId, targetThreadId, true);
                Console.WriteLine($"[WindowHandler] AttachThreadInput: foreground={foregroundThreadId} -> target={targetThreadId}, success={attached}");
            }

            // Restore if minimized
            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
                System.Threading.Thread.Sleep(50);
            }

            // Bring to top and focus
            BringWindowToTop(hWnd);
            SetWindowPos(hWnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            bool success = SetForegroundWindow(hWnd);

            Console.WriteLine($"[WindowHandler] SetForegroundWindow called for handle: {hWnd}, success={success}");

            // Give Windows time to process the focus change
            System.Threading.Thread.Sleep(20);
        }
        finally
        {
            // Detach from the thread input queue
            if (attached && foregroundThreadId != 0 && targetThreadId != 0)
            {
                AttachThreadInput(foregroundThreadId, targetThreadId, false);
            }
        }
    }

    private static IntPtr FindNextFocusableWindow(IntPtr currentWindow)
    {
        // Get the next window in Z-order (the window right behind the current one)
        IntPtr nextWindow = GetWindow(currentWindow, GetWindowType.GW_HWNDNEXT);

        while (nextWindow != IntPtr.Zero)
        {
            // Find the first visible, non-minimized window
            if (IsWindowVisible(nextWindow) && !IsIconic(nextWindow))
            {
                return nextWindow;
            }

            nextWindow = GetWindow(nextWindow, GetWindowType.GW_HWNDNEXT);
        }

        // Fallback: Return the shell/desktop window
        return GetShellWindow();
    }

    public static void ForceFocusWindow(IntPtr hWnd)
    {
        // Use AttachThreadInput for more reliable focusing
        IntPtr foregroundWindow = GetForegroundWindow();
        uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
        uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);

        bool attached = false;
        try
        {
            // Attach the foreground thread to the target thread's input queue
            if (foregroundThreadId != targetThreadId && foregroundThreadId != 0 && targetThreadId != 0)
            {
                attached = AttachThreadInput(foregroundThreadId, targetThreadId, true);
                Console.WriteLine($"[WindowHandler] ForceFocusWindow AttachThreadInput: foreground={foregroundThreadId} -> target={targetThreadId}, success={attached}");
            }

            // Restore if minimized
            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
                System.Threading.Thread.Sleep(50);
            }

            // Bring to top and focus
            BringWindowToTop(hWnd);
            SetWindowPos(hWnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            bool success = SetForegroundWindow(hWnd);

            Console.WriteLine($"[WindowHandler] ForceFocusWindow SetForegroundWindow called for handle: {hWnd}, success={success}");

            // Give Windows time to process the focus change
            System.Threading.Thread.Sleep(20);
        }
        finally
        {
            // Detach from the thread input queue
            if (attached && foregroundThreadId != 0 && targetThreadId != 0)
            {
                AttachThreadInput(foregroundThreadId, targetThreadId, false);
            }
        }
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
