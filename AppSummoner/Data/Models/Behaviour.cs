namespace AppSummoner.Data.Models;

public class Behaviour
{
    public bool Launch { get; init; }
    public bool Focus { get; init; }
    public bool Dismiss { get; init; }
    public DismissType DismissType { get; init; }
}

public enum DismissType { MINIMIZE, HIDE }
