using AppSummoner.Data;
using AppSummoner.Data.Models;
using AppSummoner.Domain.Handlers;

namespace AppSummoner.Domain;

public static class Actor
{
    public static void Execute(HotkeyConfig hotkeyConfig)
    {
        Execute(hotkeyConfig.AppInfo, hotkeyConfig.Behaviour);
    }
    
    private static void Execute(AppInfo appInfo, Behaviour behaviour)
    {
        Console.WriteLine($"[Actor] Executing for {appInfo.ProcessName}, Dismiss={behaviour.Dismiss}, Focus={behaviour.Focus}, Launch={behaviour.Launch}");

        if (behaviour.Dismiss || behaviour.Focus)
        {
            bool isInForeground = WindowHandler.IsProcessInForeground(appInfo.ProcessName);
            Console.WriteLine($"[Actor] {appInfo.ProcessName} is in foreground: {isInForeground}");

            if (isInForeground)
            {
                if (behaviour.Dismiss)
                {
                    Console.WriteLine($"[Actor] Dismissing {appInfo.ProcessName} (type: {behaviour.DismissType})");
                    if (behaviour.DismissType == DismissType.MINIMIZE)
                    {
                        WindowHandler.MinimizeForegroundWindow();
                    }
                    else
                    {
                        WindowHandler.HideForegroundWindow();
                    }

                    return;
                }
            }
            else if (behaviour.Focus)
            {
                bool isRunning = WindowHandler.IsProcessRunning(appInfo.ProcessName);
                Console.WriteLine($"[Actor] {appInfo.ProcessName} is running: {isRunning}");

                if (isRunning)
                {
                    Console.WriteLine($"[Actor] Focusing {appInfo.ProcessName}");
                    WindowHandler.FocusWindow(appInfo.ProcessName);
                    return;
                }
                else
                {
                    Console.WriteLine($"[Actor] {appInfo.ProcessName} is not running, cannot focus");
                }
            }
        }

        if (behaviour.Launch)
        {
            if (WindowHandler.IsProcessRunning(appInfo.ProcessName))
            {
                Console.WriteLine($"[Actor] {appInfo.ProcessName} already running, not launching");
                return;
            }

            Console.WriteLine($"[Actor] Launching {appInfo.ProcessName}");
            WindowHandler.LaunchApplication(appInfo.FilePath);
        }
    }
}
