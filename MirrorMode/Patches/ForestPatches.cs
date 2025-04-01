using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using StardewValley.Locations;
using xTile.Layers;

namespace MirrorMode.Patches;

[HarmonyPatch]
public static class ForestPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Forest), nameof(Forest.GetTravelingMerchantCartTile))]
    static void GetTravelingMerchantCartTile_Postfix(Forest __instance, ref Point __result)
    {
        if (!__instance.TryGetMapPropertyAs("TravelingCartPosition", out Point tile, required: false))
        {
            __result = __result.Mirror(__instance.Map.TileWidth()) - new Point(8, 0);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Woods), nameof(Woods.updateStatueEyes))]
    static bool updateStatueEyes_Prefix(Woods __instance)
    {
        Layer frontLayer = __instance.map.RequireLayer("Front");
        Point vec1 = new Point(8, 6).Mirror(__instance.Map.TileWidth());
        Point vec2 = new Point(9, 6).Mirror(__instance.Map.TileWidth());
        if (__instance.hasUnlockedStatue.Value && !__instance.localPlayerHasFoundStardrop())
        {
            
            frontLayer.Tiles[vec1.X, 6].TileIndex = 1117;
            frontLayer.Tiles[vec2.X, 6].TileIndex = 1118;
        }
        else
        {
            frontLayer.Tiles[vec1.X, 6].TileIndex = 1115;
            frontLayer.Tiles[vec2.X, 6].TileIndex = 1116;
        }

        return false;
    }
}