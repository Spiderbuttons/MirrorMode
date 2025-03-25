using System.Collections.Generic;

namespace MirrorMode.Helpers;

public class TasCatcher
{
    public Dictionary<string, bool> Whitelist { get; } = new();

    public bool TryAdd(string identifier, bool value)
    {
        if (!Whitelist.TryAdd(identifier, value)) return false;
        ModEntry.ModHelper.Data.WriteJsonFile("tasCache.json", this);
        return true;
    }

    public bool TryRemove(string identifier)
    {
        if (!Whitelist.Remove(identifier, out _)) return false;
        ModEntry.ModHelper.Data.WriteJsonFile("tasCache.json", this);
        return true;
    }

    public bool WhitelistTas(string identifier)
    {
        if (!Whitelist.TryGetValue(identifier, out var value)) return false;
        if (value) return false;
        Whitelist[identifier] = true;
        ModEntry.ModHelper.Data.WriteJsonFile("tasCache.json", this);
        return true;
    }

    public bool BlacklistTas(string identifier)
    {
        if (!Whitelist.TryGetValue(identifier, out var value)) return false;
        if (!value) return false;
        Whitelist[identifier] = false;
        ModEntry.ModHelper.Data.WriteJsonFile("tasCache.json", this);
        return true;
    }
}