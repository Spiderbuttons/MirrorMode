using HarmonyLib;
using MirrorMode.Helpers;
using StardewValley.Locations;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(BusStop))]
public static class BusStopPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BusStop.resetLocalState))]
    static void resetLocalState_Postfix(BusStop __instance)
    {
        // Log.Error("Warping bus");
        // __instance.busPosition = (__instance.busPosition / 64).Mirror(__instance.Map.TileWidth()) * 64;
    }
}