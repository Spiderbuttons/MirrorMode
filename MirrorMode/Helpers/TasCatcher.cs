using System.Collections.Generic;

namespace MirrorMode.Helpers;

public class TasCatcher
{
    public HashSet<string> TASToSkip { get; set; } = new();

    public bool Blacklist(string identifier)
    {
        if (TASToSkip.Add(identifier))
        {
            ModEntry.ModHelper.Data.WriteJsonFile("tasCache.json", this);
            return true;
        }
        return false;
    }

    public bool Whitelist(string identifier)
    {
        if (TASToSkip.Remove(identifier))
        {
            ModEntry.ModHelper.Data.WriteJsonFile("tasCache.json", this);
            return true;
        }
        return false;
    }
}