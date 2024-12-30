using AppSummoner.Data;

namespace AppSummoner.Domain.Interfaces;

public interface IConfigManager
{
    public List<HotkeyConfig> GetHotkeys();
    public AppSettings GetAppSettings();
}
