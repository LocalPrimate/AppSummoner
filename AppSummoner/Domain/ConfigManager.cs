using System.IO;
using AppSummoner.Data;
using AppSummoner.Domain.Interfaces;
using Newtonsoft.Json;

namespace AppSummoner.Domain;

public sealed class ConfigManager : IConfigManager
{
    private const string AppDataFolderName = "AppSummoner";
    private const string SettingsFileName = "settings.json";
    private const string ConfigFileName = "hotkeys.json";

    public List<HotkeyConfig> GetHotkeys()
    {
        List<HotkeyConfig> hotkeys = [];
        
        try
        {
            var dataPath = Path.Combine(AppDataFolderName, ConfigFileName);
            var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), dataPath);
            
            using var reader = new StreamReader(fullPath);
            
            var json = reader.ReadToEnd();
            
            hotkeys = JsonConvert.DeserializeObject<List<HotkeyConfig>>(json) 
                      ?? throw new Exception("hotkey config failed to deserialize");
        }
        catch (Exception e)
        {
            Console.WriteLine($"exception thrown while getting hotkeys: {e}");
        }

        return hotkeys;
    }

    public AppSettings GetAppSettings()
    {
        Console.WriteLine("Hey, getting app settings c:!!!");
        throw new NotImplementedException();
        
        // var appSettingsFileName = Path.Combine(
        //     Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        //     SettingsFileName);
        //
        // return null;
    }
}
