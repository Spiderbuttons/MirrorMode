using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace MirrorMode.Patches;

[HarmonyPatch]
[HarmonyPatch(typeof(FarmHouse))]
public static class FarmHousePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(FarmHouse.AddStarterFurniture))]
    static void AddStarterFurniture_Postfix(FarmHouse __instance, Farm farm)
    {
        if (farm.GetMapPropertySplitBySpaces("FarmHouseFurniture").Any()) return;
        foreach (var furniture in __instance.furniture)
        {
            furniture.SetPlacement(furniture.TileLocation.Mirror(__instance.Map.TileWidth() - furniture.getTilesWide() + 1));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(FarmHouse.AddStarterGiftBox))]
    static void AddStarterGiftBox_Postfix(FarmHouse __instance, Farm farm)
    {
        if (farm.GetMapPropertySplitBySpaces("FarmHouseStarterGift").Any()) return;
        Chest? chest = __instance.Objects.Values.First(x => x is Chest c && c.giftboxIsStarterGift.Value) as Chest;
        if (chest is null) return;
        
        Vector2 oldLocation = chest.TileLocation;
        chest.TileLocation = oldLocation.Mirror(__instance.Map.TileWidth());
        __instance.Objects.Remove(oldLocation);
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(FarmHouse.getEntryLocation))]
    static IEnumerable<CodeInstruction> getEntryLocation_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(Point), [typeof(int), typeof(int)]))
            ).Repeat(codeMatcher =>
                codeMatcher.Advance(1).Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(MirroringExtensions), nameof(MirroringExtensions.TileWidth))),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.FirstMethod(typeof(MirroringExtensions),
                            method => method.Name == nameof(MirroringExtensions.Mirror) &&
                                      method.ReturnType == typeof(Point)))
                ));

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch Farm.getEntryLocation: {ex}");
            return code;
        }
    }
}