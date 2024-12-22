using System.IO;
using AppSummoner.Data.Models;

namespace AppSummoner.Domain.Handlers;

public static class InputHandler
{
    public static void OnHotkeyPressed(Hotkey hotkey)
    {
        
    }

    public static void OnTestingButtonPressed(FileInfo fileInfo)
    {
        // 1. Log the attempt
        // 2. Set hotkey cooldown (~50ms?) to prevent accidents (make this configurable)
        // 3. Make a call to the main App Summoner, passing the behaviour

        // In the final version of this application, the Hotkey will be known when the event is consumed.
        // That's when the behaviour is acquired, based on the Hotkey.

        var appInfo = new AppInfo
        {
            FriendlyName = fileInfo.Name.ToUpper(),
            ProcessName = Path.GetFileNameWithoutExtension(fileInfo.Name),
            FilePath = fileInfo.FullName
        };

        var behaviour = new Behaviour
        {
            Launch = true,
            Focus = true,
            Minimize = true,
            Hide = false
        };

        Actor.Execute(appInfo, behaviour);
    }
}
