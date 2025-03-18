using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewValley;

namespace MirrorMode.Patches;

[HarmonyPatch]
[HarmonyPatch(typeof(Game1))]
public static class Game1Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Game1.warpCharacter), [typeof(NPC), typeof(GameLocation), typeof(Vector2)])]
    static void warpCharacter_Prefix(NPC character, GameLocation targetLocation, ref Vector2 position)
    {
        if (Game1.timeOfDay == 600)
        {
            // position = position.Mirror(targetLocation.Map.TileWidth());
            // if (character.Name is "Haley")
            // {
            //     Log.Warn("Haley's Schedule:");
            //     foreach (var kvp in character.Schedule!)
            //     {
            //         Log.Info($"{kvp.Key}: ({kvp.Value?.targetTile}) {kvp.Value?.route?.Join(null, " | ")}");
            //     }
            // }
        }
    }
}