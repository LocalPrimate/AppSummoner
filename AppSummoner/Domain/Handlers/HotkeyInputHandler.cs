using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using AppSummoner.Data;
using AppSummoner.Data.Models;
using AppSummoner.Domain.Interfaces;
using Bio.Win32;

namespace AppSummoner.Domain.Handlers;

// Todo: Inject this whole class on application startup, instead of instantiating it in a WPF window.

public class HotkeyInputHandler : IHotkeyInputHandler
{
    private readonly IConfigManager _configManager;
    private readonly Dictionary<string, HotkeyConfig> _hotkeys;
    private readonly ModifierKeyFlags _modifiersTracker;
    private readonly HashSet<VK> _keyTracker;
    private readonly HashSet<VK> _blockedKeys; // Track which keys we've blocked so we can block their release too
    private DateTime _lastHotkeyExecutionTime = DateTime.MinValue;
    private const int HOTKEY_COOLDOWN_MS = 50;
    private string _lastTriggeredHotkey = string.Empty;

    // Low-level keyboard hook
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookCallback;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public HotkeyInputHandler(IConfigManager configManager)
    {
        _configManager = configManager;
        _modifiersTracker = new ModifierKeyFlags();
        _hotkeys = new Dictionary<string, HotkeyConfig>();
        _keyTracker = [];
        _blockedKeys = [];
        SetupKeyboardHook();
    }

    public void Init()
    {
        LoadHotkeys();
    }

    public void Reload()
    {
        _hotkeys.Clear();
        LoadHotkeys();
    }

    private void LoadHotkeys()
    {
        var hotkeyConfigs = _configManager.GetHotkeys();
        hotkeyConfigs.ForEach(hotkeyConfig =>
        {
            _hotkeys.TryAdd(hotkeyConfig.Hotkey.GetKey(), hotkeyConfig);
        });
    }


    //--- PRIVATE METHODS
    //---------------------------------------------------------------------------------------------------

    private void SetupKeyboardHook()
    {
        _hookCallback = HookCallback;
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule? curModule = curProcess.MainModule;
        if (curModule != null)
        {
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private void DisposeKeyboardHook()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        _hookCallback = null;
    }
    
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            VK vkCode = (VK)hookStruct.vkCode;

            try
            {
                // Handle key down events
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    var keyIsBeingHeldDown = !_keyTracker.Add(vkCode);

                    if (keyIsBeingHeldDown)
                    {
                        // Key is being held - don't process but pass through
                        Console.WriteLine($"[HotkeyHandler] Key held down, ignoring: {vkCode}");
                        return CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    Console.WriteLine($"[HotkeyHandler] Key down: {vkCode}, Modifiers: Ctrl={_modifiersTracker.LeftControl || _modifiersTracker.RightControl}, Alt={_modifiersTracker.LeftAlt || _modifiersTracker.RightAlt}");

                    // Update modifier tracking
                    switch (vkCode)
                    {
                        case VK.LCONTROL:
                            _modifiersTracker.LeftControl = true;
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);

                        case VK.RCONTROL:
                            _modifiersTracker.RightControl = true;
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);

                        case VK.LSHIFT:
                            _modifiersTracker.LeftShift = true;
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);

                        case VK.RSHIFT:
                            _modifiersTracker.RightShift = true;
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);

                        case VK.LMENU:
                            _modifiersTracker.LeftAlt = true;
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);

                        case VK.RMENU:
                            _modifiersTracker.RightAlt = true;
                            return CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    // Check if this forms a hotkey
                    var eventHotkey = new Hotkey
                    {
                        Keycode = vkCode,
                        Modifiers = _modifiersTracker
                    };

                    if (_hotkeys.TryGetValue(eventHotkey.GetKey(), out HotkeyConfig? targetHotkeyConfig))
                    {
                        if (targetHotkeyConfig.Enabled)
                        {
                            string hotkeyKey = eventHotkey.GetKey();

                            // Check cooldown
                            var timeSinceLastHotkey = (DateTime.Now - _lastHotkeyExecutionTime).TotalMilliseconds;
                            if (timeSinceLastHotkey < HOTKEY_COOLDOWN_MS)
                            {
                                Console.WriteLine($"[HotkeyHandler] BLOCKED: {hotkeyKey} for {targetHotkeyConfig.AppInfo.FriendlyName} (cooldown: {timeSinceLastHotkey:F0}ms)");
                                // Block the event from reaching the application
                                return (IntPtr)1;
                            }

                            Console.WriteLine($"[HotkeyHandler] TRIGGERING: {hotkeyKey} for {targetHotkeyConfig.AppInfo.FriendlyName}");

                            _lastHotkeyExecutionTime = DateTime.Now;
                            _lastTriggeredHotkey = hotkeyKey;

                            // Mark this key as blocked so we also block its release
                            _blockedKeys.Add(vkCode);

                            Actor.Execute(targetHotkeyConfig);

                            Console.WriteLine($"[HotkeyHandler] COMPLETED: {hotkeyKey}");

                            // Block this hotkey keystroke from reaching the application
                            return (IntPtr)1;
                        }
                    }
                }
                // Handle key up events
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    Console.WriteLine($"[HotkeyHandler] Key up: {vkCode}");
                    _keyTracker.Remove(vkCode);

                    // If we blocked this key's down event, also block its up event
                    bool wasBlocked = _blockedKeys.Remove(vkCode);
                    if (wasBlocked)
                    {
                        Console.WriteLine($"[HotkeyHandler] Blocking key up for: {vkCode}");
                    }

                    switch (vkCode)
                    {
                        case VK.LCONTROL:
                            _modifiersTracker.LeftControl = false;
                            break;

                        case VK.RCONTROL:
                            _modifiersTracker.RightControl = false;
                            break;

                        case VK.LSHIFT:
                            _modifiersTracker.LeftShift = false;
                            break;

                        case VK.RSHIFT:
                            _modifiersTracker.RightShift = false;
                            break;

                        case VK.LMENU:
                            _modifiersTracker.LeftAlt = false;
                            break;

                        case VK.RMENU:
                            _modifiersTracker.RightAlt = false;
                            break;
                    }

                    // Block the key up event if we blocked the key down
                    if (wasBlocked)
                    {
                        return (IntPtr)1;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[HotkeyHandler] Exception in hook: {e}");
            }
        }

        // Pass the event through to next hook/application
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}
