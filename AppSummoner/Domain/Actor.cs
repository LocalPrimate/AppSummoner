using AppSummoner.Data.Models;
using AppSummoner.Domain.Handlers;

namespace AppSummoner.Domain;

public static class Actor
{
    public static void Execute(AppInfo appInfo, Behaviour behaviour)
    {
        if (behaviour.Minimize || behaviour.Hide || behaviour.Focus)
        {
            if (WindowHandler.IsProcessInForeground(appInfo.ProcessName))
            {
                if (behaviour.Minimize)
                {
                    WindowHandler.MinimizeForegroundWindow();
                    return;
                }
                if (behaviour.Hide)
                {
                    WindowHandler.HideForegroundWindow();
                    return;
                }
            }
            else if (behaviour.Focus)
            {
                if (WindowHandler.IsProcessRunning(appInfo.ProcessName))
                {
                    WindowHandler.FocusWindow(appInfo.ProcessName);
                    return;
                }
            }
        }

        if (behaviour.Launch)
        {
            if (WindowHandler.IsProcessRunning(appInfo.ProcessName))
            {
                // TODO: Log that the flow reached the Launch condition, but the process is already running.
                // TODO: Implement behaviour allowing user to create multiple instances of the target application.
                //       If that behaviour is enabled, then another instance should be launched here.
                
                // For now, end the flow if the process is already running.
                return;
            }

            WindowHandler.LaunchApplication(appInfo.FilePath);
        }
    }
}
