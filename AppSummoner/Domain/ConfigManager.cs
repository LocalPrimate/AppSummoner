using System.IO;
using System.Text;
using AppSummoner.Data;
using AppSummoner.Data.Models;
using AppSummoner.Domain.Interfaces;
using Newtonsoft.Json;

namespace AppSummoner.Domain;

// Todo: Decide if all config files should be in the executable's location instead.
// Todo: Handle exceptions with crash logs

public class ConfigManager : IConfigManager
{
    private const string APP_DATA_FOLDER_NAME = "AppSummoner";
    private const string HOTKEY_CONFIG_FILE_NAME = "hotkeys.json";
    private const string SETTINGS_FILE_NAME = "settings.json";
    
    private readonly string _hotkeyConfigFile;
    private readonly string _settingsFile;

    public ConfigManager()
    {
        (_hotkeyConfigFile, _settingsFile) = ValidateAndCreateDirectories();
    }
    
    public List<HotkeyConfig> GetHotkeys()
    {
        try
        {
            using var reader = new StreamReader(_hotkeyConfigFile);
            var json = reader.ReadToEnd();
            
            return JsonConvert.DeserializeObject<List<HotkeyConfig>>(json) 
                      ?? throw new Exception("hotkey config failed to deserialize");
        }
        catch (Exception e)
        {
            Console.WriteLine($"exception thrown while getting hotkeys: {e}");
            throw;
        }
    }

    public UserSettings GetUserSettings()
    {
        try
        {
            using var reader = new StreamReader(_settingsFile);
            var json = reader.ReadToEnd();
            
            return JsonConvert.DeserializeObject<UserSettings>(json) 
                   ?? throw new Exception("user settings failed to deserialize");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private (string hotkeyConfigFile, string settingsFile) ValidateAndCreateDirectories()
    {
        try
        {
            var windowsAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var thisAppDataDir = Path.Combine(windowsAppDataDir, APP_DATA_FOLDER_NAME);
            var hotkeyConfigPath = Path.Combine(thisAppDataDir, HOTKEY_CONFIG_FILE_NAME);
            var userSettingsPath = Path.Combine(thisAppDataDir, SETTINGS_FILE_NAME);
            
            if (!Path.Exists(thisAppDataDir))
                Directory.CreateDirectory(thisAppDataDir);

            if (!Path.Exists(hotkeyConfigPath))
            {
                const string emptyJsonArray = "[]";
                using var fs = File.Create(hotkeyConfigPath);
                var info = new UTF8Encoding(true).GetBytes(emptyJsonArray);
                fs.Write(info, 0, info.Length);
            }

            if (!Path.Exists(userSettingsPath))
            {
                const string emptyJson = "{}";
                using var fs = File.Create(userSettingsPath);
                var info = new UTF8Encoding(true).GetBytes(emptyJson);
                fs.Write(info, 0, info.Length);
            }

            return (hotkeyConfigPath, userSettingsPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
