using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;
using AppSummoner.Data;
using AppSummoner.Data.Models;
using AppSummoner.Domain.Handlers;
using AppSummoner.Domain.Interfaces;
using AppSummoner.Domain.Loggers;

namespace AppSummoner.Domain.Services;

public class FocusStealingSuppressor : IFocusStealingSuppressor
{
    private readonly IConfigManager _configManager;
    private readonly DispatcherTimer _zOrderMonitorTimer;
    private IntPtr _lastForegroundWindow;
    private string _lastForegroundProcessName = string.Empty;
    private IntPtr _lastClickedWindow = IntPtr.Zero;
    private DateTime _lastClickTime = DateTime.MinValue;
    private bool _userInitiatedWindowSwitch = false;
    private DateTime _lastWindowSwitchTime = DateTime.MinValue;
    private bool _taskbarClickDetected = false;
    private DateTime _lastTaskbarClickTime = DateTime.MinValue;
    private bool _isEnabled;
    private IntPtr _winEventHook = IntPtr.Zero;

    // Windows hook for detecting mouse clicks and keyboard shortcuts
    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private LowLevelMouseProc? _mouseProc;
    private LowLevelKeyboardProc? _keyboardProc;

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // Z-order management
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // Windows event hook for real-time foreground window changes
    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint GW_HWNDNEXT = 2;
    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_ASYNCWINDOWPOS = 0x4000;

    // Mouse messages
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;

    // Keyboard messages and virtual key codes
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_TAB = 0x09;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_MENU = 0x12; // Alt key
    private const int VK_LMENU = 0xA4; // Left Alt
    private const int VK_RMENU = 0xA5; // Right Alt

    // Windows event constants
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    public FocusStealingSuppressor(IConfigManager configManager)
    {
        _configManager = configManager;

        // Create a timer to monitor Z-order (less critical, can be slower)
        _zOrderMonitorTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250) // Check every 250ms
        };
        _zOrderMonitorTimer.Tick += MonitorZOrder;
    }

    public void Start()
    {
        if (_isEnabled) return;

        _isEnabled = true;
        _lastForegroundWindow = WinProcess.GetForegroundWindowHandle();
        _lastForegroundProcessName = WinProcess.GetForegroundWindowProcess()?.ProcessName ?? string.Empty;

        // Set up mouse hook to detect clicks on specific windows
        _mouseProc = MouseHookCallback;
        _mouseHookId = SetHookMouse(WH_MOUSE_LL, _mouseProc);

        // Set up keyboard hook to detect Alt+Tab and other window-switching shortcuts
        _keyboardProc = KeyboardHookCallback;
        _keyboardHookId = SetHookKeyboard(WH_KEYBOARD_LL, _keyboardProc);

        // Set up Windows event hook for real-time foreground window change detection
        _winEventHook = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND,
            EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            WinEventProc,
            0,
            0,
            WINEVENT_OUTOFCONTEXT);

        _zOrderMonitorTimer.Start();
    }

    public void Stop()
    {
        if (!_isEnabled) return;

        _isEnabled = false;
        _zOrderMonitorTimer.Stop();

        // Unhook mouse hook
        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        // Unhook keyboard hook
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        // Unhook Windows event hook
        if (_winEventHook != IntPtr.Zero)
        {
            UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        }
    }

    public void AddBlockedExecutable(BlockedExecutable executable)
    {
        var settings = _configManager.LoadUserSettings();

        // Check if already exists
        if (settings.BlockedExecutables.Any(e =>
            e.ProcessName.Equals(executable.ProcessName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        settings.BlockedExecutables.Add(executable);
        _configManager.SaveUserSettings(settings);
    }

    public void RemoveBlockedExecutable(string processName)
    {
        var settings = _configManager.LoadUserSettings();
        settings.BlockedExecutables.RemoveAll(e =>
            e.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        _configManager.SaveUserSettings(settings);
    }

    public List<BlockedExecutable> GetBlockedExecutables()
    {
        var settings = _configManager.LoadUserSettings();
        return settings.BlockedExecutables;
    }

    private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        // This fires when a window becomes the foreground window
        if (eventType == EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
        {
            try
            {
                IntPtr currentForegroundWindow = hwnd;
                var settings = _configManager.LoadUserSettings();

                // Check if foreground window has changed
                if (currentForegroundWindow == _lastForegroundWindow)
                {
                    return;
                }

                Process? currentProcess = WinProcess.GetForegroundWindowProcess();
                if (currentProcess == null)
                {
                    return;
                }

                string currentProcessName = currentProcess.ProcessName;

                // Check if the new foreground process is in the blocked list
                bool isBlocked = settings.BlockedExecutables.Any(e =>
                    currentProcessName.Contains(e.ProcessName, StringComparison.OrdinalIgnoreCase));

                if (isBlocked)
                {
                    // Check if this was a user-initiated focus change
                    // Allow if either:
                    // 1. User clicked on this specific window recently (within 1 second)
                    // 2. User used keyboard-based window switching (Alt+Tab, Win+Tab, etc.)
                    // 3. User clicked on the taskbar recently (within 1 second)
                    bool wasRecentlyClicked = (currentForegroundWindow == _lastClickedWindow) &&
                                             (DateTime.Now - _lastClickTime).TotalMilliseconds < 1000;

                    bool wasKeyboardSwitch = _userInitiatedWindowSwitch &&
                                            (DateTime.Now - _lastWindowSwitchTime).TotalMilliseconds < 1000;

                    bool wasTaskbarClick = _taskbarClickDetected &&
                                          (DateTime.Now - _lastTaskbarClickTime).TotalMilliseconds < 1000;

                    if (!wasRecentlyClicked && !wasKeyboardSwitch && !wasTaskbarClick)
                    {
                        // This appears to be an automatic focus steal, revert it IMMEDIATELY
                        Console.WriteLine($"[FocusStealingSuppressor] Blocked focus steal attempt by: {currentProcessName}");

                        // Restore focus to the previous window
                        if (_lastForegroundWindow != IntPtr.Zero)
                        {
                            WindowHandler.ForceFocusWindow(_lastForegroundWindow);
                        }

                        // Don't update the last foreground window since we reverted
                        return;
                    }
                    else
                    {
                        if (wasRecentlyClicked)
                            Console.WriteLine($"[FocusStealingSuppressor] Allowing user-clicked focus to: {currentProcessName}");
                        else if (wasKeyboardSwitch)
                            Console.WriteLine($"[FocusStealingSuppressor] Allowing keyboard-switched focus to: {currentProcessName}");
                        else
                            Console.WriteLine($"[FocusStealingSuppressor] Allowing taskbar-clicked focus to: {currentProcessName}");

                        // Reset the switch flags
                        _userInitiatedWindowSwitch = false;
                        _taskbarClickDetected = false;
                    }
                }

                // Update tracking variables
                _lastForegroundWindow = currentForegroundWindow;
                _lastForegroundProcessName = currentProcessName;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "Error in WinEventProc");
            }
        }
    }

    private void MonitorZOrder(object? sender, EventArgs e)
    {
        try
        {
            IntPtr currentForegroundWindow = WinProcess.GetForegroundWindowHandle();
            var settings = _configManager.LoadUserSettings();

            // Check for Z-order violations (windows on top but not focused)
            EnforceZOrder(currentForegroundWindow, settings);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex, "Error in Z-order monitoring");
        }
    }

    private void EnforceZOrder(IntPtr foregroundWindow, UserSettings settings)
    {
        try
        {
            // Get all top-level windows and check if any blocked windows are on top but not focused
            var blockedWindows = new List<IntPtr>();

            EnumWindows((hWnd, lParam) =>
            {
                // Only check visible windows with titles
                if (!IsWindowVisible(hWnd) || GetWindowTextLength(hWnd) == 0)
                {
                    return true;
                }

                // Skip the foreground window (it's allowed to be on top)
                if (hWnd == foregroundWindow)
                {
                    return true;
                }

                // Get the process for this window
                GetWindowThreadProcessId(hWnd, out uint processId);

                try
                {
                    Process process = Process.GetProcessById((int)processId);
                    string processName = process.ProcessName;

                    // Check if this process is blocked
                    bool isBlocked = settings.BlockedExecutables.Any(e =>
                        processName.Contains(e.ProcessName, StringComparison.OrdinalIgnoreCase));

                    if (isBlocked)
                    {
                        blockedWindows.Add(hWnd);
                    }
                }
                catch
                {
                    // Process might have exited, ignore
                }

                return true;
            }, IntPtr.Zero);

            // For each blocked window, check if it's inappropriately positioned above the foreground window
            foreach (IntPtr blockedWindow in blockedWindows)
            {
                if (IsWindowAbove(blockedWindow, foregroundWindow))
                {
                    // Check if user recently clicked on this window or used keyboard switching
                    bool wasRecentlyClicked = (blockedWindow == _lastClickedWindow) &&
                                             (DateTime.Now - _lastClickTime).TotalMilliseconds < 1000;

                    bool wasKeyboardSwitch = _userInitiatedWindowSwitch &&
                                            (DateTime.Now - _lastWindowSwitchTime).TotalMilliseconds < 1000;

                    if (!wasRecentlyClicked && !wasKeyboardSwitch)
                    {
                        GetWindowThreadProcessId(blockedWindow, out uint processId);
                        try
                        {
                            Process process = Process.GetProcessById((int)processId);
                            Console.WriteLine($"[FocusStealingSuppressor] Pushing {process.ProcessName} window back in Z-order");
                        }
                        catch { }

                        // Move window to bottom of Z-order without activating it
                        SetWindowPos(blockedWindow, HWND_BOTTOM, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex, "Error enforcing Z-order");
        }
    }

    private bool IsWindowAbove(IntPtr window1, IntPtr window2)
    {
        // Walk through Z-order from top to bottom
        IntPtr current = window1;

        while (current != IntPtr.Zero)
        {
            if (current == window2)
            {
                // We found window2 before looping back to window1,
                // which means window1 is above window2
                return true;
            }

            current = GetWindow(current, GW_HWNDNEXT);

            // Safety check to prevent infinite loops
            if (current == window1)
            {
                break;
            }
        }

        return false;
    }

    private IntPtr SetHookMouse(int hookType, LowLevelMouseProc proc)
    {
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule? curModule = curProcess.MainModule;
        if (curModule == null) return IntPtr.Zero;

        return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr SetHookKeyboard(int hookType, LowLevelKeyboardProc proc)
    {
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule? curModule = curProcess.MainModule;
        if (curModule == null) return IntPtr.Zero;

        return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();

            // Only track actual mouse button clicks
            if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN || msg == WM_MBUTTONDOWN)
            {
                try
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    POINT clickPoint = hookStruct.pt;

                    // Determine which window was clicked
                    IntPtr clickedWindow = WindowFromPoint(clickPoint);

                    if (clickedWindow != IntPtr.Zero)
                    {
                        // Check if user clicked on the taskbar
                        StringBuilder className = new StringBuilder(256);
                        GetClassName(clickedWindow, className, className.Capacity);
                        string windowClass = className.ToString();

                        if (windowClass.Contains("Shell_TrayWnd") ||
                            windowClass.Contains("TaskListThumbnailWnd") ||
                            windowClass.Contains("MSTaskSwWClass") ||
                            windowClass.Contains("MSTaskListWClass"))
                        {
                            // User clicked on taskbar
                            _taskbarClickDetected = true;
                            _lastTaskbarClickTime = DateTime.Now;
                            Console.WriteLine($"[FocusStealingSuppressor] Taskbar click detected");
                        }
                        else
                        {
                            _lastClickedWindow = clickedWindow;
                            _lastClickTime = DateTime.Now;

                            // Optional: Log for debugging
                            try
                            {
                                GetWindowThreadProcessId(clickedWindow, out uint processId);
                                Process process = Process.GetProcessById((int)processId);
                                Console.WriteLine($"[FocusStealingSuppressor] User clicked on window: {process.ProcessName}");
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "Error in mouse hook callback");
                }
            }
        }
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();

            // Detect key down events
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                try
                {
                    KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    uint vkCode = hookStruct.vkCode;

                    // Check for window-switching keyboard shortcuts
                    bool isAltPressed = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                    bool isWinPressed = (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 ||
                                       (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;

                    // Detect Alt+Tab, Alt+Shift+Tab, Win+Tab, Win+number keys
                    if ((isAltPressed && vkCode == VK_TAB) ||  // Alt+Tab
                        (isWinPressed && vkCode == VK_TAB) ||  // Win+Tab
                        (isWinPressed && vkCode >= 0x30 && vkCode <= 0x39)) // Win+0-9 (taskbar shortcuts)
                    {
                        _userInitiatedWindowSwitch = true;
                        _lastWindowSwitchTime = DateTime.Now;
                        Console.WriteLine($"[FocusStealingSuppressor] Detected user-initiated window switch (keyboard)");
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "Error in keyboard hook callback");
                }
            }
        }
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }
}

