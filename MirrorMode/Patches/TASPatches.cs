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

[HarmonyPatch]
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
    [HarmonyPatch(typeof(TemporaryAnimatedSpriteList), nameof(TemporaryAnimatedSpriteList.AddRange))]
    static void AddRange_Postfix(IEnumerable<TemporaryAnimatedSprite> values)
    {
        if (!Context.IsWorldReady) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(2), out var type, out var method)) return;
        var fullName = $"{type}.{method}";
        
        foreach (var item in values)
        {
            var identifier =
                $"_Spiderbuttons.MirrorMode(\"{fullName}\" \"{Game1.currentLocation.Name}\" \"{item.textureName}\" \"{item.sourceRect}\" \"{item.initialParentTileIndex}\")";
            
            if (item.text is null)
            {
                item.text = identifier;
            } else item.text += identifier;
            
            if (ModEntry.TasCatcher.Whitelist.TryGetValue(identifier, out var value) && !value) return;
        
            ModEntry.ModMonitor.LogOnce("TAS Added via '" + fullName + "'", LogLevel.Error);
            if (!ModEntry.TasCatcher.Whitelist.ContainsKey(item.text)) item.HighlightForDebug();
            item.drawAboveAlwaysFront = true;
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

            ModEntry.TasCatcher.Whitelist.TryAdd(identifier, false);
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TemporaryAnimatedSpriteList), nameof(TemporaryAnimatedSpriteList.Add))]
    static void Add_Postfix(TemporaryAnimatedSprite item)
    {
        if (!Context.IsWorldReady || item.positionFollowsAttachedCharacter) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(1), out var type, out var method)) return;
        var fullName = $"{type}.{method}";

        var identifier =
            $"_Spiderbuttons.MirrorMode(\"{fullName}\" \"{Game1.currentLocation.Name}\" \"{item.textureName}\" \"{item.sourceRect}\" \"{item.initialParentTileIndex}\")";
        if (item.text is null)
        {
            item.text = identifier;
        } else item.text += identifier;
        
        if (ModEntry.TasCatcher.Whitelist.TryGetValue(identifier, out var value) && !value) return;
        
        ModEntry.ModMonitor.LogOnce("TAS Added via '" + fullName + "'", LogLevel.Error);
        if (!ModEntry.TasCatcher.Whitelist.ContainsKey(item.text)) item.HighlightForDebug();
        item.drawAboveAlwaysFront = true;
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

        ModEntry.TasCatcher.Whitelist.TryAdd(identifier, false);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(TemporaryAnimatedSprite), nameof(TemporaryAnimatedSprite.draw))]
    static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TemporaryAnimatedSprite), nameof(TemporaryAnimatedSprite.text)))
            ).Repeat(codeMatcher =>
                codeMatcher.Advance(1).Insert(
                    // new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(TASPatches), nameof(StripText)))
                ));

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch TemporaryAnimatedSprite.draw: {ex}");
            return code;
        }
    }

    public static string? StripText(string? text)
    {
        if (text is null) return null;
        var index = text.IndexOf("_Spiderbuttons.MirrorMode", StringComparison.Ordinal);
        if (index == -1) return text;
        return text[..index].Length == 0 ? null : text[..index];
    }

    public static void HighlightForDebug(this TemporaryAnimatedSprite sprite)
    {
        sprite.color = new Color(255 - sprite.color.R, 255 - sprite.color.G, 255 - sprite.color.B);
        // sprite.pulse = true;
        // sprite.pulseTime = 50;
        // Color[] pixels = new Color[sprite.Texture.Width * sprite.Texture.Height];
        // List<Color> newPixels = new List<Color>();
        // sprite.Texture.GetData(0, sprite.sourceRect, pixels, 0, sprite.sourceRect.Width * sprite.sourceRect.Height);
        // foreach (var pixel in pixels)
        // {
        //     newPixels.Add(new Color(255 - pixel.R, 255 - pixel.G, 255 - pixel.B));
        // }
        // sprite.Texture.SetData(newPixels.ToArray());
    }
}