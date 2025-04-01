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
        __result *= 2;
    }
}