namespace AppSummoner.Data.Models;

public class Behaviour
{
    public bool Launch { get; init; }
    public bool Focus { get; init; }
    public bool Minimize { get; init; } // TODO: Couple this to Hide, so that only 1 option is available
    public bool Hide { get; init; }     // TODO: Couple this to Minimize, so that only 1 option is available
}
