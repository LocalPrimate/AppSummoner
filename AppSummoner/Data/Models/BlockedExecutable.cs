namespace AppSummoner.Data.Models;

public class BlockedExecutable
{
    public required string ProcessName { get; init; }
    public required string FilePath { get; init; }
    public DateTime AddedDate { get; init; } = DateTime.Now;
}
