using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using Rectangle = xTile.Dimensions.Rectangle;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(Event))]
public static class EventPatches
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

    private static GameLocation? _currentMapForParsing = null;

    private static GameLocation CurrentMapForParsing => _currentMapForParsing ??= Game1.currentLocation!;
    
    private static Dictionary<NPC, int> showFrameFlips = new();
    private static Dictionary<NPC, HashSet<int>> animateFlips = new();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(Event.exitEvent))]
    static void exitEvent_Postfix()
    {
        _currentMapForParsing = null;
        showFrameFlips.Clear();
        animateFlips.Clear();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Event.InitializeEvent))]
    static void InitializeEvent_Prefix(GameLocation location)
    {
        // Log.Debug("Mirroring second event command: " + Game1.CurrentEvent.eventCommands[1]);
        string[] args = ArgUtility.SplitQuoteAware(Game1.CurrentEvent.eventCommands[1], ' ', keepQuotesAndEscapes: true);
        if (ArgUtility.TryGetPoint(args, 0, out var viewport, out _))
        {
            args[0] = viewport.Mirror(CurrentMapForParsing.Map.TileWidth()).X.ToString();
        }
        Game1.CurrentEvent.eventCommands[1] = string.Join(" ", args);
        
        // Log.Debug("Mirroring third event command: " + Game1.CurrentEvent.eventCommands[2]);
        args = ArgUtility.SplitQuoteAware(Game1.CurrentEvent.eventCommands[2], ' ', keepQuotesAndEscapes: true);
        for (int i = 0; i < args.Length; i += 4)
        {
            if (ArgUtility.TryGetPoint(args, i + 1, out var tile, out _) &&
                ArgUtility.TryGetDirection(args, i + 3, out int dir1, out _))
            {
                args[i + 1] = tile.Mirror(CurrentMapForParsing.Map.TileWidth()).X.ToString();
                args[i + 3] = dir1 switch
                {
                    1 => "3",
                    3 => "1",
                    _ => args[i + 3]
                };
            }
        }
        Game1.CurrentEvent.eventCommands[2] = string.Join(" ", args);
        if (!string.IsNullOrEmpty(Game1.player.locationBeforeForcedEvent.Value)) Game1.player.positionBeforeEvent.Mirror(Game1.getLocationFromName(Game1.player.locationBeforeForcedEvent.Value).Map.TileWidth());
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Event.endBehaviors), [typeof(string[]), typeof(GameLocation)])]
    static void endBehaviors_Prefix(Event __instance, string[] args, GameLocation location)
    {
        if (!string.Join(' ', args).EqualsIgnoreCase(__instance.eventCommands[^1]) || !string.Join(' ', args).Contains("end position", StringComparison.OrdinalIgnoreCase)) return;
        if (args.Length >= 3)
        {
            args[2] = (__instance.exitLocation.Location.Map.TileWidth() - int.Parse(args[2]) - 1).ToString();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.MoveToSoup))]
    static bool MoveToSoup_Prefix(Event @event)
    {
        if (Game1.year % 2 == 1)
        {
            @event.setUpAdvancedMove(new string[9] { "", "Gus", "false", "0", "-1", "-5", "0", "4", "1000" });
            @event.setUpAdvancedMove(new string[5] { "", "Jodi", "false", "0", "-2" });
            @event.setUpAdvancedMove(new string[11]
            {
                "", "Clint", "false", "0", "1", "1", "0", "0", "3", "2", "0"
            });
            @event.setUpAdvancedMove(new string[5] { "", "Emily", "false", "-3", "0" });
            @event.setUpAdvancedMove(new string[7] { "", "Pam", "false", "0", "2", "-7", "0" });
        }
        else
        {
            @event.setUpAdvancedMove(new string[5] { "", "Pierre", "false", "-3", "0" });
            @event.setUpAdvancedMove(new string[9] { "", "Pam", "false", "0", "2", "4", "0", "0", "1" });
            @event.setUpAdvancedMove(new string[9] { "", "Abigail", "false", "-4", "0", "0", "-3", "3", "4000" });
            @event.setUpAdvancedMove(new string[9] { "", "Alex", "false", "5", "0", "0", "-1", "1", "2000" });
            @event.setUpAdvancedMove(new string[5] { "", "Gus", "false", "0", "-1" });
        }
        @event.CurrentCommand++;
        return false;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.LoadActors))]
    static void LoadActors_Postfix(Event @event)
    {
        if (@event.actors is not null)
        {
            foreach (var npc in @event.actors)
            {
                // if (npc.FacingDirection != 0 && npc.FacingDirection != 2) Log.Debug($"{npc.Name} facing {npc.FacingDirection} before");
                npc.faceDirection(npc.FacingDirection switch
                {
                    1 => 3,
                    3 => 1,
                    _ => npc.FacingDirection
                });
                // if (npc.FacingDirection != 0 && npc.FacingDirection != 2) Log.Debug($"{npc.Name} facing {npc.FacingDirection} after");
            }
        }

        // Log.Alert("Loaded actors");
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Event.tryEventCommand))]
    static void tryEventCommand_Prefix(GameLocation location, string[] args)
    {
        _currentMapForParsing = location;
        bool flippedActorThisFrame = false;

        // ModEntry.ModMonitor.LogOnce($"Attempting to mirror command '{string.Join(" ", args)}'", LogLevel.Debug);
        // ModEntry.ModMonitor.LogOnce($"Before: {string.Join(" ", args)}", LogLevel.Warn);
        var split = args;
        switch (split[0].ToLower())
        {
            case "changelocation":
                // Log.Warn("Changing location to " + split[1]);
                var newMap = Game1.getLocationFromName(split[1]);
                if (Game1.CurrentEvent.actors is not null)
                {
                    foreach (var npc in Game1.CurrentEvent.actors)
                    {
                        // Log.Info($"Correcting {npc.Name} tile position from {npc.Tile}");
                        npc.setTilePosition(npc.Tile.Mirror(CurrentMapForParsing.Map.TileWidth()).ToPoint());
                        npc.setTilePosition(npc.Tile.Mirror(newMap.Map.TileWidth()).ToPoint());
                        // Log.Info($"Corrected {npc.Name} tile position to {npc.Tile}");
                        var cont = Game1.CurrentEvent.npcControllers.FindIndex(cont =>
                            cont.puppet.Name.EqualsIgnoreCase(npc.Name));
                        if (cont is not -1)
                        {
                            foreach (var path in Game1.CurrentEvent.npcControllers[cont].path)
                            {
                                path.Mirror(CurrentMapForParsing.Map.TileWidth());
                                path.Mirror(newMap.Map.TileWidth());
                                
                            }

                            Game1.CurrentEvent.npcControllers[cont] = new NPCController(Game1.CurrentEvent.npcControllers[cont].puppet, Game1.CurrentEvent.npcControllers[cont].path, Game1.CurrentEvent.npcControllers[cont].loop, Game1.CurrentEvent.npcControllers[cont].behaviorAtEnd);
                        }
                        npc.FacingDirection = npc.FacingDirection switch
                        {
                            1 => 3,
                            3 => 1,
                            _ => npc.FacingDirection
                        };
                    }
                }
                
                if (Game1.CurrentEvent.farmerActors is not null)
                {
                    foreach (var farmer in Game1.CurrentEvent.farmerActors)
                    {
                        farmer.Position = farmer.Position.Mirror(CurrentMapForParsing.Map.TileWidth() * 64);
                        farmer.Position = farmer.Position.Mirror(newMap.Map.TileWidth() * 64);
                        farmer.FacingDirection = farmer.FacingDirection switch
                        {
                            1 => 3,
                            3 => 1,
                            _ => farmer.FacingDirection
                        };
                    }
                }
                
                if (Game1.CurrentEvent.props is not null)
                {
                    foreach (var obj in Game1.CurrentEvent.props)
                    {
                        obj.TileLocation = obj.TileLocation.Mirror(CurrentMapForParsing.Map.TileWidth());
                        obj.TileLocation = obj.TileLocation.Mirror(newMap.Map.TileWidth());
                    }
                }
                
                if (Game1.CurrentEvent.festivalProps is not null)
                {
                    foreach (var prop in Game1.CurrentEvent.festivalProps)
                    {
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            CurrentMapForParsing.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            newMap.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                    }
                }
                
                Game1.viewport.X = CurrentMapForParsing.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                Game1.viewport.X = newMap.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                _currentMapForParsing = newMap;
                break;
            case "changetotemporarymap":
                // Log.Warn("Changing map to " + split[1]);
                var tempMap = ((split[1] == "Town") ? new Town("Maps\\Town", "Temp") : ((Game1.CurrentEvent.isFestival && split[1].Contains("Town")) ? new Town("Maps\\" + split[1], "Temp") : new GameLocation("Maps\\" + split[1], "Temp")));
                if (Game1.CurrentEvent.actors is not null)
                {
                    foreach (var npc in Game1.CurrentEvent.actors)
                    {
                        // Log.Info($"Correcting {npc.Name} tile position from {npc.Tile}");
                        npc.setTilePosition(npc.Tile.Mirror(CurrentMapForParsing.Map.TileWidth()).ToPoint());
                        npc.setTilePosition(npc.Tile.Mirror(tempMap.Map.TileWidth()).ToPoint());
                        // Log.Info($"Corrected {npc.Name} tile position to {npc.Tile}");
                        var cont = Game1.CurrentEvent.npcControllers.FindIndex(cont =>
                            cont.puppet.Name.EqualsIgnoreCase(npc.Name));
                        if (cont is not -1)
                        {
                            foreach (var path in Game1.CurrentEvent.npcControllers[cont].path)
                            {
                                path.Mirror(CurrentMapForParsing.Map.TileWidth());
                                path.Mirror(tempMap.Map.TileWidth());
                                
                            }

                            Game1.CurrentEvent.npcControllers[cont] = new NPCController(Game1.CurrentEvent.npcControllers[cont].puppet, Game1.CurrentEvent.npcControllers[cont].path, Game1.CurrentEvent.npcControllers[cont].loop, Game1.CurrentEvent.npcControllers[cont].behaviorAtEnd);
                        }
                        npc.FacingDirection = npc.FacingDirection switch
                        {
                            1 => 3,
                            3 => 1,
                            _ => npc.FacingDirection
                        };
                    }
                }
                
                if (Game1.CurrentEvent.farmerActors is not null)
                {
                    foreach (var farmer in Game1.CurrentEvent.farmerActors)
                    {
                        farmer.Position = farmer.Position.Mirror(CurrentMapForParsing.Map.TileWidth() * 64);
                        farmer.Position = farmer.Position.Mirror(tempMap.Map.TileWidth() * 64);
                        farmer.FacingDirection = farmer.FacingDirection switch
                        {
                            1 => 3,
                            3 => 1,
                            _ => farmer.FacingDirection
                        };
                    }
                }
                
                if (Game1.CurrentEvent.props is not null)
                {
                    foreach (var obj in Game1.CurrentEvent.props)
                    {
                        obj.TileLocation = obj.TileLocation.Mirror(CurrentMapForParsing.Map.TileWidth());
                        obj.TileLocation = obj.TileLocation.Mirror(tempMap.Map.TileWidth());
                    }
                }
                
                if (Game1.CurrentEvent.festivalProps is not null)
                {
                    foreach (var prop in Game1.CurrentEvent.festivalProps)
                    {
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            CurrentMapForParsing.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                        prop.drawRect = new Microsoft.Xna.Framework.Rectangle(
                            tempMap.Map.TileWidth() - prop.drawRect.X - prop.drawRect.Width,
                            prop.drawRect.Y, prop.drawRect.Width, prop.drawRect.Height);
                    }
                }
                
                Game1.viewport.X = CurrentMapForParsing.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                Game1.viewport.X = tempMap.Map.TileWidth() - Game1.viewport.X - Game1.viewport.Width;
                _currentMapForParsing = tempMap;
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

                if (!Game1.CurrentEvent.IsFarmerActorId(split[1], out _))
                {
                    var movingNpc = Game1.CurrentEvent.getActorByName(split[1]);
                    if (movingNpc is null) break;
                    movingNpc.flip = showFrameFlips.ContainsKey(movingNpc) && !movingNpc.flip;
                }

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
                    split[defaultsIndex + 1] = defaultPosition.Mirror(CurrentMapForParsing.Map.TileWidth()).X.ToString();
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
                            split[j] = position.Mirror(CurrentMapForParsing.Map.TileWidth()).X.ToString();
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
            case "viewport" when !split[1].EqualsIgnoreCase("move") && !split[1].EqualsIgnoreCase("player"):
                if (split.Length >= 3)
                {
                    split[1] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();
                }

                break;
            case "addfloorprop":
            case "addprop":
                if (split.Length >= 5)
                {
                    split[2] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[2]) - int.Parse(split[5]))
                        .ToString();
                }
                else split[2] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[2]) - 1).ToString();

                break;
            case "makeinvisible":
                if (split.Length >= 3)
                {
                    split[1] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[1]) - int.Parse(split[3]))
                        .ToString();
                }
                else split[1] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();

                break;
            case "addlantern":
            case "changemaptile":
            case "warp":
                if (split.Length >= 3)
                {
                    split[2] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[2]) - 1).ToString();
                }
                break;
            case "end" when split.Length >= 2 && split[1].EqualsIgnoreCase("position"):
                if (split.Length >= 3)
                {
                    split[2] = (Game1.CurrentEvent.exitLocation.Location.Map.TileWidth() - int.Parse(split[2]) - 1).ToString();
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
                    split[4] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[2])).ToString();
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
                    split[1] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[1]) - 1).ToString();
                }

                if (split.Length >= 7)
                {
                    split[6] = (!bool.Parse(split[6])).ToString();
                }

                break;
            case "temporaryanimatedsprite":
                // split[9] = (CurrentMapForParsing.Map.TileWidth() - int.Parse(split[9]) - 1).ToString();
                // split[12] = (!bool.Parse(split[12])).ToString();
                break;
            case "advancedmove":
                for (int i = 3; i < split.Length; i += 2)
                {
                    if (!split[i].Equals("0") && split[i + 1].Equals("0"))
                    {
                        split[i] = (int.Parse(split[i]) * -1).ToString();
                    } else if (!split[i].Equals("0") && !split[i + 1].Equals("0"))
                    {
                        split[i] = split[i] switch 
                        {
                            "1" => "3",
                            "3" => "1",
                            _ => split[i]
                        };
                    }
                }

                break;
            case "showkissframe":
            case "showframe":
                if (bool.TryParse(split[^1], out var flip))
                {
                    split[^1] = (!flip).ToString();
                }
                else
                {
                    split = split.AddItem("true").ToArray();
                }

                if (!Game1.CurrentEvent.IsFarmerActorId(split[1], out _))
                {
                    var npc = Game1.CurrentEvent.getActorByName(split[1]);
                    if (npc is null) break;
                    npc.flip = true;
                    showFrameFlips.TryAdd(npc, int.Parse(split[2]));
                }

                break;
            case "animate":
                if (bool.TryParse(split[2], out var flip2))
                {
                    split[2] = (!flip2).ToString();
                }
                var npc2 = Game1.CurrentEvent.getActorByName(split[1]);
                if (npc2 is null) break;
                if (!animateFlips.ContainsKey(npc2))
                {
                    animateFlips.TryAdd(npc2, new HashSet<int>());
                }
                for (int i = 5; i < split.Length; i++)
                {
                    animateFlips[npc2].Add(int.Parse(split[i]));
                }
                npc2.flip = showFrameFlips.ContainsKey(npc2) && !npc2.flip;
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
        // ModEntry.ModMonitor.LogOnce($"After: {string.Join(" ", split)}", LogLevel.Warn);
    }
}