using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using Rectangle = xTile.Dimensions.Rectangle;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(Event))]
public class EventPatches
{
    /* EVENT COMMANDS

    FIELDS 2 and 3:
        <x> <y>
        [<actor> <x> <y> <direction>]+

        move <actor> <x> <y> <direction>

        warpFarmers [<x> <y> <direction>]+

    GROUP 1:
        addBigProp <x> <y>
        addObject <x> <y>
        doAction <x> <y>
        removeObject <x> <y>
        removeTile <x> <y>
        removeSprite <x> <y>
        viewport <x> <y>

        addFloorProp <index> <x> <y> <w> <h>
        addProp <index> <x> <y> <w> <h>

        makeInvisible <x> <y> <w> <h>

    GROUP 2:
        addLantern <index> <x> <y>
        changeMapTile <index> <x> <y>
        end position <x> <y>
        positionOffset <actor> <x> <y>
        warp <actor> <x> <y>
        viewPort move <x> <y>
        drawOffset <actor> <x> <y>

        addTemporaryActor <asset> <w> <h> <x> <y> <direction>

        temporarySprite <x> <y> <row> <length> <interval> <flip> <depth>

        temporaryAnimatedSprite <texture> <x> <y> <w> <h> <interval> <frames> <loops> <x> <y> <flicker> <flip>

        advancedMove <actor> <loop> [<x> <y>]+ OR <actor> <loop> [<direction> <duration>] or whatever

        animate <actor> <flip>
        showKissFrame <actor> <flip>
        showFrame farmer <frame> <flip>

        faceDirection <actor> <direction>

    */

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Event.InitializeEvent))]
    static void InitializeEvent_Prefix(GameLocation location)
    {
        string[] args = ArgUtility.SplitQuoteAware(Game1.CurrentEvent.eventCommands[1], ' ', keepQuotesAndEscapes: true);
        if (ArgUtility.TryGetPoint(args, 0, out var viewport, out _))
        {
            args[0] = viewport.Mirror(location.Map.TileWidth()).X.ToString();
        }
        Game1.CurrentEvent.eventCommands[1] = string.Join(" ", args);
        
        args = ArgUtility.SplitQuoteAware(Game1.CurrentEvent.eventCommands[2], ' ', keepQuotesAndEscapes: true);
        for (int i = 0; i < args.Length; i += 4)
        {
            if (ArgUtility.TryGetPoint(args, i + 1, out var tile, out _) &&
                ArgUtility.TryGetDirection(args, i + 3, out int dir1, out _))
            {
                args[i + 1] = tile.Mirror(location.Map.TileWidth()).X.ToString();
                args[i + 3] = dir1 switch
                {
                    1 => "3",
                    3 => "1",
                    _ => args[i + 3]
                };
            }
        }
        Game1.CurrentEvent.eventCommands[2] = string.Join(" ", args);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Event.tryEventCommand))]
    static void tryEventCommand_Prefix(GameLocation location, string[] args)
    {
        if (string.Join(" ", args).Equals(Game1.CurrentEvent.eventCommands[1]))
        {
            if (ArgUtility.TryGetPoint(args, 0, out var tile, out _))
            {
                args[0] = tile.Mirror(Game1.currentLocation.Map.TileWidth()).X.ToString();
            }

            return;
        }

        if (string.Join(" ", args).Equals(Game1.CurrentEvent.eventCommands[2]))
        {
            for (int i = 0; i < args.Length; i += 4)
            {
                if (ArgUtility.TryGetPoint(args, i + 1, out var tile, out _) &&
                    ArgUtility.TryGetDirection(args, i + 3, out int dir1, out _))
                {
                    args[i + 1] = tile.Mirror(Game1.currentLocation.Map.TileWidth()).X.ToString();
                    args[i + 3] = dir1 switch
                    {
                        1 => "3",
                        3 => "1",
                        _ => args[i + 3]
                    };
                }
            }

            return;
        }

        // Log.Debug($"Attempting to mirror command '{string.Join(" ", args)}'");
        var split = args;
        switch (split[0].ToLower())
        {
            case "changelocation":
                Log.Warn("Changing location to " + split[1]);
                var newLoc = Game1.getLocationFromName(split[1]);
                if (Game1.CurrentEvent.actors is not null)
                {
                    foreach (var npc in Game1.CurrentEvent.actors)
                    {
                        npc.setTilePosition(npc.Tile.Mirror(location.Map.TileWidth()).ToPoint());
                        npc.setTilePosition(npc.Tile.Mirror(newLoc.Map.TileWidth()).ToPoint());
                    }
                }

                if (Game1.CurrentEvent.farmerActors is not null)
                {
                    foreach (var farmer in Game1.CurrentEvent.farmerActors)
                    {
                        farmer.Position = farmer.Position.Mirror(location.Map.TileWidth() * 64);
                        farmer.Position = farmer.Position.Mirror(newLoc.Map.TileWidth() * 64);
                    }
                }

                if (Game1.CurrentEvent.props is not null)
                {
                    foreach (var obj in Game1.CurrentEvent.props)
                    {
                        obj.TileLocation = obj.TileLocation.Mirror(location.Map.TileWidth());
                        obj.TileLocation = obj.TileLocation.Mirror(newLoc.Map.TileWidth());
                    }
                }

                if (Game1.CurrentEvent.festivalProps is not null)
                {
                    foreach (var prop in Game1.CurrentEvent.festivalProps)
                    {
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            location.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            newLoc.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                    }
                }

                Game1.viewport.X = location.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                Game1.viewport.X = newLoc.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                break;
            case "changetotemporarymap":
                Log.Warn("Changing map to " + split[1]);
                var tempMap = ((split[1] == "Town") ? new Town("Maps\\Town", "Temp") : ((Game1.CurrentEvent.isFestival && split[1].Contains("Town")) ? new Town("Maps\\" + split[1], "Temp") : new GameLocation("Maps\\" + split[1], "Temp")));
                if (Game1.CurrentEvent.actors is not null)
                {
                    foreach (var npc in Game1.CurrentEvent.actors)
                    {
                        npc.setTilePosition(npc.Tile.Mirror(location.Map.TileWidth()).ToPoint());
                        npc.setTilePosition(npc.Tile.Mirror(tempMap.Map.TileWidth()).ToPoint());
                    }
                }

                if (Game1.CurrentEvent.farmerActors is not null)
                {
                    foreach (var farmer in Game1.CurrentEvent.farmerActors)
                    {
                        farmer.Position = farmer.Position.Mirror(location.Map.TileWidth() * 64);
                        farmer.Position = farmer.Position.Mirror(tempMap.Map.TileWidth() * 64);
                    }
                }

                if (Game1.CurrentEvent.props is not null)
                {
                    foreach (var obj in Game1.CurrentEvent.props)
                    {
                        obj.TileLocation = obj.TileLocation.Mirror(location.Map.TileWidth());
                        obj.TileLocation = obj.TileLocation.Mirror(tempMap.Map.TileWidth());
                    }
                }

                if (Game1.CurrentEvent.festivalProps is not null)
                {
                    foreach (var prop in Game1.CurrentEvent.festivalProps)
                    {
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            location.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            tempMap.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                    }
                }

                Game1.viewport.X = location.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                Game1.viewport.X = tempMap.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                break;

            case "move":
                if (split.Length > 2)
                {
                    for (int i = 1; i < split.Length && ArgUtility.HasIndex(split, i + 3); i += 4)
                    {
                        if (ArgUtility.TryGetPoint(split, i + 1, out var tile, out _) &&
                            ArgUtility.TryGetDirection(split, i + 3, out int dir1, out _))
                        {
                            split[i + 1] = (int.Parse(split[i + 1]) * -1).ToString();
                            split[i + 3] = dir1 switch
                            {
                                1 => "3",
                                3 => "1",
                                _ => split[i + 3]
                            };
                        }
                    }
                }

                Log.Warn("Mirrored move command: " + string.Join(" ", split));
                break;
            case "warpfarmers":
                int nonWarpFields = (split.Length - 1) % 3;
                if (split.Length < 5 || nonWarpFields != 1) break;
                int defaultsIndex = split.Length - 4;
                if (ArgUtility.TryGetDirection(split, defaultsIndex, out var offsetDirection, out _) &&
                    ArgUtility.TryGetPoint(split, defaultsIndex + 1, out var defaultPosition, out _) &&
                    ArgUtility.TryGetDirection(split, defaultsIndex + 3, out var defaultFacingDirection, out _))
                {
                    split[defaultsIndex] = offsetDirection switch
                    {
                        1 => "3",
                        3 => "1",
                        _ => split[defaultsIndex]
                    };
                    split[defaultsIndex + 1] = defaultPosition.Mirror(location.Map.TileWidth()).X.ToString();
                    split[defaultsIndex + 3] = defaultFacingDirection switch
                    {
                        1 => "3",
                        3 => "1",
                        _ => split[defaultsIndex + 3]
                    };
                    for (int j = 1; j < defaultsIndex; j += 3)
                    {
                        if (ArgUtility.TryGetPoint(split, j, out var position, out _) &&
                            ArgUtility.TryGetDirection(split, j + 2, out var facingDirection, out _))
                        {
                            split[j] = position.Mirror(location.Map.TileWidth()).X.ToString();
                            split[j + 2] = facingDirection switch
                            {
                                1 => "3",
                                3 => "1",
                                _ => split[j + 2]
                            };
                        }
                    }
                }

                break;
            case "addbigprop":
            case "addobject":
            case "doaction":
            case "removeobject":
            case "removetile":
            case "removesprite":
            case "viewport" when !split[1].EqualsIgnoreCase("move"):
                if (split.Length >= 3)
                {
                    split[1] = (location.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();
                }

                break;
            case "addfloorprop":
            case "addprop":
                if (split.Length >= 5)
                {
                    split[2] = (location.Map.TileWidth() - int.Parse(split[2]) - int.Parse(split[5]))
                        .ToString();
                }
                else split[2] = (location.Map.TileWidth() - int.Parse(split[2]) - 1).ToString();

                break;
            case "makeinvisible":
                if (split.Length >= 3)
                {
                    split[1] = (location.Map.TileWidth() - int.Parse(split[1]) - int.Parse(split[3]))
                        .ToString();
                }
                else split[1] = (location.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();

                break;
            case "addlantern":
            case "changemaptile":
            case "end" when split.Length >= 2 && split[1].EqualsIgnoreCase("position"):
            case "warp":
                if (split.Length >= 3)
                {
                    split[2] = (location.Map.TileWidth() - int.Parse(split[2]) - 1).ToString();
                }

                break;
            case "positionoffset":
            case "viewport" when split[1].EqualsIgnoreCase("move"):
            case "drawoffset":
                if (split.Length >= 3)
                {
                    split[2] = (int.Parse(split[2]) * -1).ToString();
                }

                break;
            case "addtemporaryactor":
                if (split.Length >= 5)
                {
                    split[4] = (location.Map.TileWidth() - int.Parse(split[2])).ToString();
                }

                if (split.Length >= 7)
                {
                    if (ArgUtility.TryGetDirection(split, 6, out int dir2, out _, null))
                    {
                        split[6] = dir2 switch
                        {
                            1 => "3",
                            3 => "1",
                            _ => split[6]
                        };
                    }
                }

                break;
            case "temporarysprite":
                if (split.Length >= 2)
                {
                    split[1] = (location.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();
                }

                if (split.Length >= 7)
                {
                    split[6] = (!bool.Parse(split[6])).ToString();
                }

                break;
            case "temporaryanimatedsprite":
                split[9] = (location.Map.TileWidth() - int.Parse(split[9]) - 1).ToString();
                split[12] = (!bool.Parse(split[12])).ToString();
                break;
            case "advancedmove":
                for (int i = 3; i < split.Length; i += 2)
                {
                    if (!split[i].Equals("0") && split[i + 1].Equals("0"))
                    {
                        split[i] = (int.Parse(split[i]) * -1).ToString();
                    }
                }

                break;
            case "animate":
            case "showkissframe":
            case "showframe":
                if (bool.TryParse(split[^1], out var flip))
                {
                    split[^1] = (!flip).ToString();
                }

                break;
            case "facedirection":
                if (ArgUtility.TryGetDirection(split, 2, out int dir3, out _, null))
                {
                    split[2] = dir3 switch
                    {
                        1 => "3",
                        3 => "1",
                        _ => split[2]
                    };
                }

                break;
        }
    }
}

// [HarmonyPostfix]
// [HarmonyPatch(nameof(Event.ParseCommands))]
// static void ParseCommands_Postfix(string[] __result)
// {
//     var loc = Game1.currentLocation;
//     var setupOne = ArgUtility.SplitBySpaceQuoteAware(__result[1]);
//     var setupTwo = ArgUtility.SplitBySpaceQuoteAware(__result[2]);
//     if (setupOne.Length == 2)
//     {
//         if (ArgUtility.TryGetPoint(setupOne, 0, out var tile, out _))
//         {
//             setupOne[0] = tile.Mirror(Game1.currentLocation.Map.TileWidth()).X.ToString();
//         }
//         __result[1] = string.Join(" ", setupOne);
//     }
//     if (setupTwo.Length > 0)
//     {
//         for (int i = 0; i < setupTwo.Length; i += 4)
//         {
//             if (ArgUtility.TryGetPoint(setupTwo, i + 1, out var tile, out _) &&
//                 ArgUtility.TryGetDirection(setupTwo, i + 3, out int dir1, out _))
//             {
//                 setupTwo[i + 1] = tile.Mirror(Game1.currentLocation.Map.TileWidth()).X.ToString();
//                 setupTwo[i + 3] = dir1 switch
//                 {
//                     1 => "3",
//                     3 => "1",
//                     _ => setupTwo[i + 3]
//                 };
//             }
//         }
//         __result[2] = string.Join(" ", setupTwo);
//     }
//     if (__result.Length <= 3) return;
//     for (int command = 2; command < __result.Length; command++)
//     {
//         Log.Debug($"Attempting to mirror command '{__result[command]}'");
//         var split = ArgUtility.SplitQuoteAware(__result[command], ' ', keepQuotesAndEscapes: true);
//         switch (split[0].ToLower())
//         {
//             case "changelocation":
//                 Log.Warn("Changing location to " + split[1]);
//                 loc = Game1.getLocationFromName(split[1]);
//
//                 if (Game1.CurrentEvent.actors is not null)
//                 {
//                     foreach (var npc in Game1.CurrentEvent.actors)
//                     {
//                         npc.setTilePosition(npc.Tile.Mirror(loc.Map.TileWidth()).ToPoint());
//                     }
//                 }
//
//                 if (Game1.CurrentEvent.farmerActors is not null)
//                 {
//                     foreach (var farmer in Game1.CurrentEvent.farmerActors)
//                     {
//                         farmer.Position = farmer.Position.Mirror(loc.Map.TileWidth() * 64);
//                     }
//                 }
//
//                 if (Game1.CurrentEvent.props is not null)
//                 {
//                     foreach (var obj in Game1.CurrentEvent.props)
//                     {
//                         obj.TileLocation = obj.TileLocation.Mirror(loc.Map.TileWidth());
//                     }
//                 }
//
//                 if (Game1.CurrentEvent.festivalProps is not null)
//                 {
//                     foreach (var prop in Game1.CurrentEvent.festivalProps)
//                     {
//                         prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
//                             loc.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
//                             prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
//                     }
//                 }
//
//                 Game1.viewport = new Rectangle(loc.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width, Game1.viewport.Y,
//                     Game1.viewport.Width, Game1.viewport.Height);
//                 break;
//             case "changetotemporarymap":
//                 Log.Warn("Changing map to " + split[1]);
//                 loc = ((split[1] == "Town") ? new Town("Maps\\Town", "Temp") : ((Game1.CurrentEvent.isFestival && split[1].Contains("Town")) ? new Town("Maps\\" + split[1], "Temp") : new GameLocation("Maps\\" + split[1], "Temp")));
//                 break;
//             
//             case "move":
//                 if (split.Length > 2)
//                 {
//                     for (int i = 1; i < split.Length && ArgUtility.HasIndex(split, i + 3); i += 4)
//                     {
//                         if (ArgUtility.TryGetPoint(split, i + 1, out var tile, out _) &&
//                             ArgUtility.TryGetDirection(split, i + 3, out int dir1, out _))
//                         {
//                             split[i + 1] = (int.Parse(split[i + 1]) * -1).ToString();
//                             split[i + 3] = dir1 switch
//                             {
//                                 1 => "3",
//                                 3 => "1",
//                                 _ => split[i + 3]
//                             };
//                         }
//                     }
//                 }
//                 Log.Warn("Mirrored move command: " + string.Join(" ", split));
//                 break;
//             case "warpfarmers":
//                 int nonWarpFields = (split.Length - 1) % 3;
//                 if (split.Length < 5 || nonWarpFields != 1) break;
//                 int defaultsIndex = split.Length - 4;
//                 if (ArgUtility.TryGetDirection(split, defaultsIndex, out var offsetDirection, out _) &&
//                     ArgUtility.TryGetPoint(split, defaultsIndex + 1, out var defaultPosition, out _) &&
//                     ArgUtility.TryGetDirection(split, defaultsIndex + 3, out var defaultFacingDirection, out _))
//                 {
//                     split[defaultsIndex] = offsetDirection switch
//                     {
//                         1 => "3",
//                         3 => "1",
//                         _ => split[defaultsIndex]
//                     };
//                     split[defaultsIndex + 1] = defaultPosition.Mirror(loc.Map.TileWidth()).X.ToString();
//                     split[defaultsIndex + 3] = defaultFacingDirection switch
//                     {
//                         1 => "3",
//                         3 => "1",
//                         _ => split[defaultsIndex + 3]
//                     };
//                     for (int j = 1; j < defaultsIndex; j += 3)
//                     {
//                         if (ArgUtility.TryGetPoint(split, j, out var position, out _) &&
//                             ArgUtility.TryGetDirection(split, j + 2, out var facingDirection, out _))
//                         {
//                             split[j] = position.Mirror(loc.Map.TileWidth()).X.ToString();
//                             split[j + 2] = facingDirection switch
//                             {
//                                 1 => "3",
//                                 3 => "1",
//                                 _ => split[j + 2]
//                             };
//                         }
//                     }
//                 }
//                 break;
//             case "addbigprop":
//             case "addobject":
//             case "doaction":
//             case "removeobject":
//             case "removetile":
//             case "removesprite":
//             case "viewport" when !split[1].EqualsIgnoreCase("move"):
//                 if (split.Length >= 3)
//                 {
//                     split[1] = (loc.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();
//                 }
//
//                 break;
//             case "addfloorprop":
//             case "addprop":
//                 if (split.Length >= 5)
//                 {
//                     split[2] = (loc.Map.TileWidth() - int.Parse(split[2]) - int.Parse(split[5]))
//                         .ToString();
//                 }
//                 else split[2] = (loc.Map.TileWidth() - int.Parse(split[2]) - 1).ToString();
//
//                 break;
//             case "makeinvisible":
//                 if (split.Length >= 3)
//                 {
//                     split[1] = (loc.Map.TileWidth() - int.Parse(split[1]) - int.Parse(split[3]))
//                         .ToString();
//                 }
//                 else split[1] = (loc.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();
//
//                 break;
//             case "addlantern":
//             case "changemaptile":
//             case "end" when split.Length >= 2 && split[1].EqualsIgnoreCase("position"):
//             case "warp":
//                 if (split.Length >= 3)
//                 {
//                     split[2] = (loc.Map.TileWidth() - int.Parse(split[2]) - 1).ToString();
//                 }
//
//                 break;
//             case "positionoffset":
//             case "viewport" when split[1].EqualsIgnoreCase("move"):
//             case "drawoffset":
//                 if (split.Length >= 3)
//                 {
//                     split[2] = (int.Parse(split[2]) * -1).ToString();
//                 }
//                 break;
//             case "addtemporaryactor":
//                 if (split.Length >= 5)
//                 {
//                     split[4] = (loc.Map.TileWidth() - int.Parse(split[2])).ToString();
//                 }
//
//                 if (split.Length >= 7)
//                 {
//                     if (ArgUtility.TryGetDirection(split, 6, out int dir2, out _, null))
//                     {
//                         split[6] = dir2 switch
//                         {
//                             1 => "3",
//                             3 => "1",
//                             _ => split[6]
//                         };
//                     }
//                 }
//
//                 break;
//             case "temporarysprite":
//                 if (split.Length >= 2)
//                 {
//                     split[1] = (loc.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();
//                 }
//
//                 if (split.Length >= 7)
//                 {
//                     split[6] = (!bool.Parse(split[6])).ToString();
//                 }
//
//                 break;
//             case "temporaryanimatedsprite":
//                 split[9] = (loc.Map.TileWidth() - int.Parse(split[9]) - 1).ToString();
//                 split[12] = (!bool.Parse(split[12])).ToString();
//                 break;
//             case "advancedmove":
//                 for (int i = 3; i < split.Length; i += 2)
//                 {
//                     if (!split[i].Equals("0") && split[i + 1].Equals("0"))
//                     {
//                         split[i] = (int.Parse(split[i]) * -1).ToString();
//                     }
//                 }
//
//                 break;
//             case "animate":
//             case "showkissframe":
//             case "showframe":
//                 if (bool.TryParse(split[^1], out var flip))
//                 {
//                     split[^1] = (!flip).ToString();
//                 }
//
//                 break;
//             case "facedirection":
//                 if (ArgUtility.TryGetDirection(split, 2, out int dir3, out _, null))
//                 {
//                     split[2] = dir3 switch
//                     {
//                         1 => "3",
//                         3 => "1",
//                         _ => split[2]
//                     };
//                 }
//                 break;
//         }
//         __result[command] = string.Join(" ", split);
//     }
// }
// }