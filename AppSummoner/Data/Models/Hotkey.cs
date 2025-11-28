using Bio.Win32;

namespace AppSummoner.Data.Models;

public class Hotkey
{
    public string? Name { get; init; }
    public VK Keycode { get; init; }
    public ModifierKeyFlags? Modifiers { get; init; }

    public string GetKey()
    {
        return $"{Modifiers}+{Keycode}";
    }
}
