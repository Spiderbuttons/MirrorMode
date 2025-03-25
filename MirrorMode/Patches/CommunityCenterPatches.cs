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
[HarmonyPatch(typeof(CommunityCenter))]
public static class CommunityCenterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CommunityCenter.getAreaBounds))]
    static void getAreaBounds_Postfix(CommunityCenter __instance, ref Rectangle __result)
    {
        __result = __result.Mirror(__instance.Map.TileWidth());
    }
}