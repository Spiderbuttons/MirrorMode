using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewValley;
using StardewValley.Pathfinding;

namespace MirrorMode.Patches;

[HarmonyPatch]
[HarmonyPatch(typeof(NPC))]
public static class NpcPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    static void parseMasterSchedule_Prefix(NPC __instance, ref string rawData)
    {
        if (__instance.Name is "Haley") Log.Warn("Haley:" + __instance.DefaultPosition);
        // __instance.DefaultPosition = (__instance.DefaultPosition / 64).Mirror(Game1.getLocationFromName(__instance.DefaultMap).Map.TileWidth()) * 64;
        if (__instance.Name is "Haley") Log.Warn("Haley:" + __instance.DefaultPosition);
        
        var splits = ArgUtility.SplitQuoteAware(rawData, '/');
        Dictionary<string, int> mapWidthCache = new();
        string newData = "";
        for (int i = 0; i < splits.Length; i++)
        {
            // Format: <time> <locationId> <x> <y> <facingDirection>
            var parts = splits[i].Split(' ');
            if (parts.Length < 5)
            {
                newData += splits[i];
                if (i != splits.Length - 1)
                    newData += "/";
                continue;
            }
            if (!int.TryParse(parts[2], out int x) || !int.TryParse(parts[3], out int y) ||
                !int.TryParse(parts[4], out int facingDirection)) continue;
            
            if (!mapWidthCache.TryGetValue(parts[1], out int mapWidth))
            {
                var location = Game1.getLocationFromName(parts[1]);
                if (location == null) continue;
                mapWidth = location.Map.TileWidth();
                mapWidthCache[parts[1]] = mapWidth;
            }
            parts[2] = ((mapWidth) - x - 1).ToString();
            if (facingDirection == 1)
                parts[4] = "3";
            else if (facingDirection == 3)
                parts[4] = "1";
            newData += string.Join(" ", parts);
            if (i != splits.Length - 1)
                newData += "/";
        }
        rawData = newData;
        if (rawData.Contains("HaleyHouse")) Log.Alert(rawData);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), nameof(Character.Halt))]
    static void Halt_Postfix(Character __instance)
    {
        // if (__instance.Name is "Haley")
        // {
        //     Log.Warn("Haley halted.");
        //     if (!Utils.TryGetCallingMethod(new StackFrame(2), out var type, out var method)) return;
        //     Log.Warn("Calling function: " + type + "." + method);
        // }
    }
}