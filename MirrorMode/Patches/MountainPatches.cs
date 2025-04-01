using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(Mountain))]
public static class MountainPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Mountain.restoreBridge))]
    static bool restoreBridge_Prefix(Mountain __instance)
    {
        LocalizedContentManager temp = Game1.content.CreateTemporary();
        Map obj = temp.Load<Map>("Maps\\Mountain-BridgeFixed");
        Point vec = new Point(92, 24).Mirror(__instance.map.TileWidth());
        int xOffset = vec.X - 7;
        int yOffset = vec.Y;
        Layer curBackLayer = __instance.map.RequireLayer("Back");
        Layer curBuildingsLayer = __instance.map.RequireLayer("Buildings");
        Layer curFrontLayer = __instance.map.RequireLayer("Front");
        Layer fixedBackLayer = obj.RequireLayer("Back");
        Layer fixedBuildingsLayer = obj.RequireLayer("Buildings");
        Layer fixedFrontLayer = obj.RequireLayer("Front");
        TileSheet tileSheet = __instance.map.RequireTileSheet(0, "outdoors");
        for (int x = 0; x < fixedBackLayer.LayerWidth; x++)
        {
            for (int y = 0; y < fixedBackLayer.LayerHeight; y++)
            {
                curBackLayer.Tiles[x + xOffset, y + yOffset] = ((fixedBackLayer.Tiles[x, y] == null) ? null : new StaticTile(curBackLayer, tileSheet, BlendMode.Alpha, fixedBackLayer.Tiles[x, y].TileIndex));
                curBuildingsLayer.Tiles[x + xOffset, y + yOffset] = ((fixedBuildingsLayer.Tiles[x, y] == null) ? null : new StaticTile(curBuildingsLayer, tileSheet, BlendMode.Alpha, fixedBuildingsLayer.Tiles[x, y].TileIndex));
                curFrontLayer.Tiles[x + xOffset, y + yOffset] = ((fixedFrontLayer.Tiles[x, y] == null) ? null : new StaticTile(curFrontLayer, tileSheet, BlendMode.Alpha, fixedFrontLayer.Tiles[x, y].TileIndex));
            }
        }
        __instance.bridgeRestored = true;
        temp.Unload();
        return false;
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Mountain.quarryDayUpdate))]
    static IEnumerable<CodeInstruction> quarryDayUpdate_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Call,
                    AccessTools.Constructor(typeof(Rectangle), [typeof(int), typeof(int), typeof(int), typeof(int)]))
            ).ThrowIfNotMatch("Unable to find entry point in Mountain.quarryDayUpdate transpiler.");
            
            matcher.Advance(1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(MirroringExtensions), nameof(MirroringExtensions.TileWidth))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.FirstMethod(typeof(MirroringExtensions),
                        method => method.Name == nameof(MirroringExtensions.Mirror) &&
                                  method.ReturnType == typeof(Rectangle))),
                new CodeInstruction(OpCodes.Stloc_0)
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch Mountain.quarryDayUpdate: {ex}");
            return code;
        }
    }
}