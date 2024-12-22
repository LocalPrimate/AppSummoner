using System.Diagnostics;
using System.Runtime.InteropServices;
using AppSummoner.Data;
using AppSummoner.Domain.Loggers;

// ReSharper disable InconsistentNaming

namespace AppSummoner.Domain.Handlers;

public static class WindowHandler
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOWMINIMIZED = 2;


    //*** PUBLIC METHODS
    //*******************************************************************

    // TODO: Don't just check for "Contains", but make a direct comparison. Determine if that works in 100% of cases or not.
    public static bool IsProcessInForeground(string processName)
    {
        var foregroundProcess = WinProcess.GetForegroundWindowProcess();
        return foregroundProcess is not null 
               && foregroundProcess.ProcessName.Contains(processName, StringComparison.CurrentCultureIgnoreCase);
    }

    // TODO: Don't just check for "Contains", but make a direct comparison. Determine if that works in 100% of cases or not.
    public static bool IsProcessRunning(string processName)
    {
        Process[] processList = Process.GetProcesses();
        return processList.Any(process => process.ProcessName.Contains(processName, StringComparison.CurrentCultureIgnoreCase));
    }

    public static void MinimizeForegroundWindow()
    { 
        Process? foregroundProcess = WinProcess.GetForegroundWindowProcess();
        
        if (foregroundProcess is null) return;
        
        ShowWindows(foregroundProcess.Id, SW_SHOWMINIMIZED);
    }

    public static void HideForegroundWindow()
    {
        Process? foregroundProcess = WinProcess.GetForegroundWindowProcess();
        
        if (foregroundProcess is null) return;
        
        ShowWindows(foregroundProcess.Id, SW_HIDE);
    }

    public static void FocusWindow(string processName)
    {
        Process[] processList = Process.GetProcesses();
        foreach (Process process in processList)
        {
            if (!process.ProcessName.Contains(processName, StringComparison.CurrentCultureIgnoreCase)) continue;
            SetForegroundWindow(process.MainWindowHandle);
            return;
        }   
    }

    public static void LaunchApplication(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath });
            // TODO: Add delay before this method can be called again for the same filepath
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
        }
    }

    
    //*** PRIVATE METHODS
    //*******************************************************************

    private static void ShowWindows(int processId, int nCmdShow)
    {
        IEnumerable<nint>? windowHandles = WinProcess.EnumerateProcessWindowHandles(processId);
        
        if (windowHandles is null) return;
        
        foreach (nint windowHandle in windowHandles)
            ShowWindow(windowHandle, nCmdShow);
    }
}
