using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using Sickhead.Engine.Util;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(TemporaryAnimatedSpriteList))]
public static class TASPatches
{
    static HashSet<string> invalidFunctions
    {
        get
        {
            // This is so fuckin dumb lmao.
            return new HashSet<string>
            {
                "Buff.OnAdded()",
                "Debris.updateChunks",
                "Event.addSpecificTemporarySprite",
                "Farmer.*",
                "Game1.showSwordswipeAnimation",
                "Game1.pressUseToolButton",
                "Farm.resetLocalState",
                "Farm.addGrandpaCandles", // Might need adjustments.
                "Farm.doLightningStrike", // Double check this.
                "GameLocation.performTouchAction", // EXCEPTIONS DONE
                "GameLocation.UpdateWhenCurrentLocation",
                "GameLocation.resetLocalState",
                "GameLocation.playTerrainSound",
                "GameLocation.CheckGarbage", // EXCEPTIONS DONE
                "GameLocation.performAction", // Needs exceptions: 270
            };
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TemporaryAnimatedSpriteList.AddRange))]
    static void AddRange_Postfix(IEnumerable<TemporaryAnimatedSprite> values)
    {
        var string2 = TokenParser.ParseText("Hello, World!");
        if (string2 != null) return;
        return;
        if (!Context.IsWorldReady) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(1), out var type, out var method)) return;
        if ($"{type}.{method}".EqualsIgnoreCase("Multiplayer.broadcastSprites") &&
            !Utils.TryGetCallingMethod(new StackFrame(2), out type, out method)) return;
        var fullName = $"{type}.{method}";
        
        foreach (var item in values)
        {
            if (item.local) continue;
            
            if (item.sourceRect.X == 666 && Game1.currentLocation.Name.EqualsIgnoreCase("Sewer"))
            {
                item.Position -= new Vector2(32, 0);
                goto mirror;
            }

            if (fullName.EqualsIgnoreCase("GameLocation.CheckGarbage"))
            {
                item.flipped = !item.flipped;
                continue;
            }
            
            if (invalidFunctions.Any(inv => inv.Contains($"{fullName}"))) continue;
            
            mirror:
            ModEntry.ModMonitor.LogOnce("TAS Added via '" + fullName + "'", LogLevel.Error);
            if (Game1.currentLocation.TemporarySprites.Contains(item))
            {
                item.Position = (item.Position / 64).Mirror(item.parent is null
                    ? Game1.currentLocation.Map.TileWidth()
                    : item.parent.Map.TileWidth()) * 64;
                item.initialPosition = (item.initialPosition / 64).Mirror(item.parent is null
                    ? Game1.currentLocation.Map.TileWidth()
                    : item.parent.Map.TileWidth()) * 64;
                item.flipped = !item.flipped;
                item.rotation = -item.rotation;
                item.rotationChange = -item.rotationChange;
                item.motion.X = -item.motion.X;
                
                if (fullName.EqualsIgnoreCase("GameLocation.setFireplace"))
                {
                    item.Position += new Vector2(16, 0);
                }
            }
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TemporaryAnimatedSpriteList.Add))]
    static void Add_Postfix(TemporaryAnimatedSprite item)
    {
        return;
        if (!Context.IsWorldReady || item.local) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(1), out var type, out var method)) return;
        var fullName = $"{type}.{method}";

        if (item.textureName.EqualsIgnoreCase("Characters\\\\asldkfjsquaskutanfsldk")) goto mirror;
        
        if (invalidFunctions.Any(inv => inv.Contains($"{fullName}"))) return;
        
        mirror:
        ModEntry.ModMonitor.LogOnce("TAS Added via '" + fullName + "'", LogLevel.Error);
        if (Game1.currentLocation.TemporarySprites.Contains(item))
        {
            item.Position = (item.Position / 64).Mirror(item.parent is null
                ? Game1.currentLocation.Map.TileWidth()
                : item.parent.Map.TileWidth()) * 64;
            item.initialPosition = (item.initialPosition / 64).Mirror(item.parent is null
                ? Game1.currentLocation.Map.TileWidth()
                : item.parent.Map.TileWidth()) * 64;
            item.flipped = !item.flipped;
            item.rotation = -item.rotation;
            item.rotationChange = -item.rotationChange;
            item.motion.X = -item.motion.X;
            
            if (fullName.EqualsIgnoreCase("GameLocation.setFireplace"))
            {
                item.Position += new Vector2(16, 0);
            }
        }
    }
}