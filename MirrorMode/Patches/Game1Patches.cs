using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewValley;
using StardewValley.Extensions;

namespace MirrorMode.Patches;

[HarmonyPatch]
[HarmonyPatch(typeof(Game1))]
public static class Game1Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Game1.warpFarmer), [typeof(string), typeof(int), typeof(int), typeof(bool)])]
    static void warpFarmer_Prefix(string locationName, ref int tileX, ref int tileY)
    {
        return;
        if (!locationName.EqualsIgnoreCase("CommunityCenter")) return;

        if (!Utils.TryGetCallingMethod(new StackFrame(2), out var type, out var method)) return;

        if ($"{type}.{method}".EqualsIgnoreCase("GameLocation.performAction"))
        {
            var newVector = new Vector2(tileX, tileY).Mirror("CommunityCenter");
            tileX = (int) newVector.X;
            tileY = (int) newVector.Y;
        }
    }
    
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