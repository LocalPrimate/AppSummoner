using System.Drawing;
using System.IO;

namespace AppSummoner.Utils;

public static class IconGenerator
{
    public static Icon CreateAppIcon()
    {
        try
        {
            // Get the executable directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var iconPath = Path.Combine(appDir, "Resources", "AppSummonerIcon.png");

            if (File.Exists(iconPath))
            {
                // Load the PNG and convert to Icon
                using var image = Image.FromFile(iconPath);
                using var bitmap = new Bitmap(image);
                IntPtr hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load custom icon: {ex.Message}");
        }

        // Fallback to system icon if file not found
        return System.Drawing.SystemIcons.Application;
    }
}
