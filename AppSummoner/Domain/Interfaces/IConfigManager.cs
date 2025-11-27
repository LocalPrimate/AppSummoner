using AppSummoner.Data;
using AppSummoner.Data.Models;

namespace AppSummoner.Domain.Interfaces;

public interface IConfigManager
{
    public List<HotkeyConfig> GetHotkeys();
    public UserSettings GetUserSettings();
    public UserSettings LoadUserSettings();
    public void SaveUserSettings(UserSettings settings);
}
