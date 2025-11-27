namespace AppSummoner.Data.Models;

public class UserSettings
{
    public List<BlockedExecutable> BlockedExecutables { get; set; } = new();
}