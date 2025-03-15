using HarmonyLib;
using MirrorMode.Helpers;
using StardewModdingAPI;
using StardewValley.Pathfinding;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(PathFindController))]
public class PathFindControllerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PathFindController.getPreferenceValueForTerrainType))]
    static void getPreferenceValueForTerrainType_Postfix(ref int __result)
    {
        ModEntry.ModMonitor.LogOnce("gfgg", LogLevel.Alert);
        // __result = __result switch
        // {
        //     -7 => -1,
        //     -4 => -2,
        //     -2 => -4,
        //     -1 => -7,
        //     _ => 0
        // };
        __result *= 2;
    }
}