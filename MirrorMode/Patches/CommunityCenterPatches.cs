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
using xTile;

namespace MirrorMode.Patches;

[HarmonyPatch]
[HarmonyPatch(typeof(CommunityCenter))]
public static class CommunityCenterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CommunityCenter.getAreaBounds))]
    static void getAreaBounds_Postfix(CommunityCenter __instance, ref Rectangle __result)
    {
        __result = __result.Mirror(__instance.Map.TileWidth());
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CommunityCenter.getNotePosition))]
    static void getNotePosition_Postfix(CommunityCenter __instance, ref Point __result)
    {
        __result = __result.Mirror(__instance.Map.TileWidth());
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(CommunityCenter.addFishTank))]
    static void addFishTank_Postfix(CommunityCenter __instance)
    {
        foreach (Furniture f2 in __instance.furniture)
        {
            if (f2.QualifiedItemId == "(F)CCFishTank" && !f2.modData.ContainsKey("MirrorMode"))
            {
                f2.SetPlacement(f2.TileLocation.Mirror(__instance.Map.TileWidth()) - new Vector2(f2.getTilesWide() - 1, 0));
                f2.Flipped = true;
                f2.modData["MirrorMode"] = "true";
                break;
            }
        }

        var v = new Vector2(1, 1);
        var test = v * 4;
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(CommunityCenter.draw))]
    static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)]))
            ).Repeat(codeMatcher =>
                codeMatcher.Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.PropertyGetter(typeof(Map), nameof(Map.DisplayWidth))),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.FirstMethod(typeof(MirroringExtensions),
                            method => method.Name == nameof(MirroringExtensions.MirrorPixels) &&
                                      method.ReturnType == typeof(Vector2)))
                ));

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch CommunityCenter.draw: {ex}");
            return code;
        }
    }
}