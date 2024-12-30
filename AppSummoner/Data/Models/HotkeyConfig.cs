using AppSummoner.Data.Models;

namespace AppSummoner.Data;

public class HotkeyConfig
{
    public required Hotkey Hotkey { get; init; }
    public required Behaviour Behaviour { get; init; }
    public required AppInfo AppInfo { get; init; }
}
