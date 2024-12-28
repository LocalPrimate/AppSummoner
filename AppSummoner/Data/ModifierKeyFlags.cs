namespace AppSummoner.Data;

public class ModifierKeyFlags
{
    public bool LeftControl { get; set; }
    public bool RightControl { get; set; }
    public bool LeftAlt { get; set; }
    public bool RightAlt { get; set; }
    public bool LeftShift { get; set; }
    public bool RightShift { get; set; }

    public override string ToString()
    {
        var modString = "";

        if (LeftControl) modString += 'c';
        if (RightControl) modString += 'C';
        if (LeftAlt) modString += 'a';
        if (RightAlt) modString += 'A';
        if (LeftShift) modString += 's';
        if (RightShift) modString += 'S';

        return modString;
    }
}
