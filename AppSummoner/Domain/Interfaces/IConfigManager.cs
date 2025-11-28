using AppSummoner.Data;
using AppSummoner.Data.Models;

namespace AppSummoner.Domain.Interfaces;

public interface IConfigManager
{
    public List<HotkeyConfig> GetHotkeys();
    public void SaveHotkeys(List<HotkeyConfig> hotkeys);
    public void AddHotkey(HotkeyConfig hotkey);
    public void RemoveHotkey(HotkeyConfig hotkey);
    public void UpdateHotkey(HotkeyConfig oldHotkey, HotkeyConfig newHotkey);
    public UserSettings GetUserSettings();
    public UserSettings LoadUserSettings();
    public void SaveUserSettings(UserSettings settings);
}
