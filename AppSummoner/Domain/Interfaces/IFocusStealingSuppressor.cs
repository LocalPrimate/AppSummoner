using AppSummoner.Data.Models;

namespace AppSummoner.Domain.Interfaces;

public interface IFocusStealingSuppressor
{
    void Start();
    void Stop();
    void AddBlockedExecutable(BlockedExecutable executable);
    void RemoveBlockedExecutable(string processName);
    List<BlockedExecutable> GetBlockedExecutables();
}
