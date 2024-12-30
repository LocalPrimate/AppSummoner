using System.IO;
using AppSummoner.Data;
using AppSummoner.Data.Models;
using AppSummoner.Domain.Interfaces;
using Bio;
using Bio.Win32;

namespace AppSummoner.Domain.Handlers;

// Todo: Inject this whole class on application startup, instead of instantiating it in a WPF window.

public class HotkeyInputHandler : IHotkeyInputHandler
{
    private readonly IConfigManager _configManager;
    private readonly Dictionary<string, HotkeyConfig> _hotkeys;
    private readonly KeyboardHook _keyboardHook;
    private readonly ModifierKeyFlags _modifiersTracker;
    private readonly HashSet<VK> _keyTracker;

    public HotkeyInputHandler(IConfigManager configManager)
    {
        _configManager = configManager;
        _keyboardHook = InitializeKeyboardHook();
        _modifiersTracker = new ModifierKeyFlags();
        _hotkeys = new Dictionary<string, HotkeyConfig>();
        _keyTracker = [];
    }

    public void Init()
    {
        // Todo: Move to func so it can be re-executed when a new hotkey is configured
        var hotkeyConfigs = _configManager.GetHotkeys();
        hotkeyConfigs.ForEach(hotkeyConfig =>
        {
            _hotkeys.TryAdd(hotkeyConfig.Hotkey.GetKey(), hotkeyConfig);
        });
    }

    
    //--- PRIVATE METHODS
    //---------------------------------------------------------------------------------------------------
    
    private KeyboardHook InitializeKeyboardHook()
    {
        var hook = new KeyboardHook();
        hook.Set();
        hook.KeyDown += OnKeyboardHookKeyDown;
        hook.KeyUp += OnKeyboardHookKeyUp;
        
        return hook;
    }

    private void DisposeKeyboardHook()
    {
        _keyboardHook.Dispose();
    }
    
    private void OnKeyboardHookKeyDown(object? sender, KeyboardInputMessage msg)
    {
        try
        {
            var keyInfo = msg.KBDLLHOOKSTRUCT;
            var keyIsBeingHeldDown = !_keyTracker.Add(keyInfo.vkCode);
            
            if (keyIsBeingHeldDown) return;

            switch (keyInfo.vkCode)
            {
                case VK.LCONTROL:
                    _modifiersTracker.LeftControl = true;
                    return;
                
                case VK.RCONTROL:
                    _modifiersTracker.RightControl = true;
                    return;
                
                case VK.LSHIFT:
                    _modifiersTracker.LeftShift = true;
                    return;
                
                case VK.RSHIFT:
                    _modifiersTracker.RightShift = true;
                    return;
                
                case VK.LMENU:
                    _modifiersTracker.LeftAlt = true;
                    return;
                
                case VK.RMENU:
                    _modifiersTracker.RightAlt = true;
                    return;
            }

            var eventHotkey = new Hotkey
            {
                Keycode = keyInfo.vkCode,
                Modifiers = _modifiersTracker
            };

            if (_hotkeys.TryGetValue(eventHotkey.GetKey(), out HotkeyConfig? targetHotkeyConfig))
            {
                Actor.Execute(targetHotkeyConfig);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"exception thrown while handling keyboard down event: {e}");
        }
    }
    
    private void OnKeyboardHookKeyUp(object? sender, KeyboardInputMessage msg)
    {
        try
        {
            var keyInfo = msg.KBDLLHOOKSTRUCT;

            _keyTracker.Remove(keyInfo.vkCode);
            
            switch (keyInfo.vkCode)
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
        }
        catch (Exception e)
        {
            Console.WriteLine($"exception thrown while handling keyboard up event: {e}");
        }
    }
}
