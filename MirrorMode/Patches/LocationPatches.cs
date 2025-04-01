using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(GameLocation))]
public class LocationPatches
{
    private static readonly HashSet<string> setMapTileBlacklist = new HashSet<string>
    {
        "CommunityCenter.doShowMissedRewardsChest",
        "DecoratableLocation.UpdateFloor",
        "DecoratableLocation.UpdateWallpaper",
        "MineShaft.calicoStatueActivated",
        "MineShaft.setElevatorLit",
        "MineShaft.checkForMapAlterations",
        "MineShaft.doCreateLadderAt",
        "MineShaft.prepareElevator",
    };

    private static readonly HashSet<string> setTilePropertyBlacklist = new HashSet<string>
    {
        "InteriorDoor.openDoorTiles",
        "BoatTunnel.UpdateGateTileProperty",
        "FarmHouse.updateCellarWarps",
        "FarmHouse.loadSpouseRoom",
    };
    
    private static readonly HashSet<string> removeTileBlacklist = new HashSet<string>
    {
        "DelayedAction.ApplyRemoveMapTile",
        "Event.setUpPlayerControlSequence",
        "InteriorDoor.openDoorTiles",
        "FarmHouse.MakeMapModifications",
        "DefaultCommands.RemoveTile"
    };
    
    private static readonly HashSet<string> removeTilePropertyBlacklist = new HashSet<string>
    {
        "InteriorDoor.openDoorTiles",
        "InteriorDoor.closeDoorTiles"
    };

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.setMapTile))]
    static void setMapTile_Prefix(GameLocation __instance, ref int tileX)
    {
        if (__instance.Name.Contains("Volcano")) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(1), out var type, out var method)) return;
        Utils.TryGetCallingMethod(new StackFrame(2), out var type2, out var method2);
        if (!setMapTileBlacklist.Contains($"{type}.{method}") && !setMapTileBlacklist.Contains($"{type2}.{method2}")) tileX = __instance.Map.TileWidth() - tileX - 1;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.setTileProperty))]
    static void setTileProperty_Prefix(GameLocation __instance, ref int tileX)
    {
        if (__instance.Name.Contains("Volcano")) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(1), out var type, out var method)) return;
        Utils.TryGetCallingMethod(new StackFrame(2), out var type2, out var method2);
        if (!setTilePropertyBlacklist.Contains($"{type}.{method}") && !setTilePropertyBlacklist.Contains(type2 is not null ? $"{type2}.{method2}" : "NULL")) tileX = __instance.Map.TileWidth() - tileX - 1;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.removeTile), [typeof(int), typeof(int), typeof(string)])]
    static void removeTile_Prefix(GameLocation __instance, ref int x)
    {
        if (__instance.Name.Contains("Volcano")) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(1), out var type, out var method)) return;
        Utils.TryGetCallingMethod(new StackFrame(2), out var type2, out var method2);
        if (!removeTileBlacklist.Contains($"{type}.{method}") && !removeTileBlacklist.Contains(type2 is not null ? $"{type2}.{method2}" : "NULL")) x = __instance.Map.TileWidth() - x - 1;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.removeTile), [typeof(Location), typeof(string)])]
    static void removeTile_Prefix(GameLocation __instance, ref Location tileLocation)
    {
        if (__instance.Name.Contains("Volcano")) return;
        tileLocation.X = __instance.Map.TileWidth() - tileLocation.X - 1;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.removeTileProperty))]
    static void removeTileProperty_Prefix(GameLocation __instance, ref int tileX)
    {
        if (__instance.Name.Contains("Volcano")) return;
        if (!Utils.TryGetCallingMethod(new StackFrame(1), out var type, out var method)) return;
        Utils.TryGetCallingMethod(new StackFrame(2), out var type2, out var method2);
        if (!removeTilePropertyBlacklist.Contains($"{type}.{method}") && !removeTilePropertyBlacklist.Contains(type2 is not null ? $"{type2}.{method2}" : "NULL")) tileX = __instance.Map.TileWidth() - tileX - 1;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.setAnimatedMapTile))]
    static void setAnimatedMapTile_Prefix(GameLocation __instance, ref int tileX)
    {
        if (__instance.Name.Contains("Volcano")) return;
        tileX = __instance.Map.TileWidth() - tileX - 1;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.setObject))]
    static void setObject_Prefix(GameLocation __instance, ref Vector2 v)
    {
        if (__instance.Name.Contains("Volcano")) return;
        v = v.Mirror(__instance.Map.TileWidth());
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.ApplyMapOverride),
        [typeof(Map), typeof(string), typeof(Rectangle?), typeof(Rectangle?), typeof(Action<Point>)])]
    static bool ApplyMapOverride_Prefix(GameLocation __instance, Map override_map, string override_key,
        Rectangle? source_rect, ref Rectangle? dest_rect, Action<Point> perTileCustomAction)
    {
        if (__instance.Name.Contains("Volcano")) return true;
        if (__instance.Name.Contains("Volcano") && __instance.Name is not "VolcanoDungeon0" and not "VolcanoDungeon5") return true;
        
        if (override_key is "spouse_room") return true;
        
        Rectangle? oriSource = source_rect is null ? null : new Rectangle(source_rect.Value.X, source_rect.Value.Y,
            source_rect.Value.Width, source_rect.Value.Height);
        Rectangle? oriDest = dest_rect is null ? null : new Rectangle(dest_rect.Value.X, dest_rect.Value.Y, 
            dest_rect.Value.Width, dest_rect.Value.Height);
        
        if (dest_rect is not null)
        {
            dest_rect = dest_rect.Value.Mirror(__instance.Map.TileWidth());
            // return true;
        }

        if (source_rect is not null)
        {
            source_rect = source_rect.Value.Mirror(override_map.TileWidth());
        }

        if (__instance._appliedMapOverrides.Contains(override_key))
        {
            return true;
        }

        __instance._appliedMapOverrides.Add(override_key);
        __instance.updateSeasonalTileSheets(override_map);
        Dictionary<TileSheet, TileSheet> tilesheet_lookup = new Dictionary<TileSheet, TileSheet>();
        foreach (TileSheet override_tile_sheet in override_map.TileSheets)
        {
            TileSheet map_tilesheet = __instance.map.GetTileSheet(override_tile_sheet.Id);
            string source_image_source = "";
            string dest_image_source = "";
            if (map_tilesheet != null)
            {
                source_image_source = map_tilesheet.ImageSource;
            }

            if (dest_image_source != null)
            {
                dest_image_source = override_tile_sheet.ImageSource;
            }

            if (map_tilesheet == null || dest_image_source != source_image_source)
            {
                map_tilesheet =
                    new TileSheet(GameLocation.GetAddedMapOverrideTilesheetId(override_key, override_tile_sheet.Id),
                        __instance.map, override_tile_sheet.ImageSource, override_tile_sheet.SheetSize,
                        override_tile_sheet.TileSize);
                for (int j = 0; j < override_tile_sheet.TileCount; j++)
                {
                    map_tilesheet.TileIndexProperties[j].CopyFrom(override_tile_sheet.TileIndexProperties[j]);
                }

                __instance.map.AddTileSheet(map_tilesheet);
            }
            else if (map_tilesheet.TileCount < override_tile_sheet.TileCount)
            {
                int tileCount = map_tilesheet.TileCount;
                map_tilesheet.SheetWidth = override_tile_sheet.SheetWidth;
                map_tilesheet.SheetHeight = override_tile_sheet.SheetHeight;
                for (int k = tileCount; k < override_tile_sheet.TileCount; k++)
                {
                    map_tilesheet.TileIndexProperties[k].CopyFrom(override_tile_sheet.TileIndexProperties[k]);
                }
            }

            tilesheet_lookup[override_tile_sheet] = map_tilesheet;
        }

        Dictionary<Layer, Layer> layer_lookup = new Dictionary<Layer, Layer>();
        int map_width = 0;
        int map_height = 0;
        for (int layer_index4 = 0; layer_index4 < override_map.Layers.Count; layer_index4++)
        {
            map_width = Math.Max(map_width, override_map.Layers[layer_index4].LayerWidth);
            map_height = Math.Max(map_height, override_map.Layers[layer_index4].LayerHeight);
        }

        if (!source_rect.HasValue)
        {
            source_rect = new Rectangle(0, 0, map_width, map_height);
        }

        map_width = 0;
        map_height = 0;
        for (int layer_index3 = 0; layer_index3 < __instance.map.Layers.Count; layer_index3++)
        {
            map_width = Math.Max(map_width, __instance.map.Layers[layer_index3].LayerWidth);
            map_height = Math.Max(map_height, __instance.map.Layers[layer_index3].LayerHeight);
        }

        bool layersDirty = false;
        for (int layer_index2 = 0; layer_index2 < override_map.Layers.Count; layer_index2++)
        {
            Layer original_layer = __instance.map.GetLayer(override_map.Layers[layer_index2].Id);
            if (original_layer == null)
            {
                original_layer = new Layer(override_map.Layers[layer_index2].Id, __instance.map,
                    new Size(map_width, map_height), override_map.Layers[layer_index2].TileSize);
                __instance.map.AddLayer(original_layer);
                layersDirty = true;
            }

            layer_lookup[override_map.Layers[layer_index2]] = original_layer;
        }

        if (layersDirty)
        {
            __instance.SortLayers();
        }

        if (!dest_rect.HasValue)
        {
            dest_rect = new Rectangle(0, 0, map_width, map_height);
        }
        
        int source_rect_x = source_rect.Value.X;
        int source_rect_y = source_rect.Value.Y;
        int dest_rect_x = dest_rect.Value.X;
        int dest_rect_y = dest_rect.Value.Y;
        for (int x = oriSource is null ? source_rect.Value.Width : 0; oriSource is null ? (x >= 0) : (x < source_rect.Value.Width); x += oriSource is null ? -1 : 1)
        {
            for (int y = 0; y < source_rect.Value.Height; y++)
            {
                Point source_tile_pos = new Point(oriSource is null ? source_rect.Value.Width - x + 1 : source_rect_x - x - 1, source_rect_y + y);
                Point dest_tile_pos = new Point(oriSource is null ? dest_rect_x - x + dest_rect.Value.Width + (oriDest is null ? 1 : 0) : dest_rect_x - x - 1, dest_rect_y + y);
                perTileCustomAction?.Invoke(dest_tile_pos);
                bool lower_layer_overridden = false;
                for (int layer_index = 0; layer_index < override_map.Layers.Count; layer_index++)
                {
                    Layer override_layer = override_map.Layers[layer_index];
                    Layer target_layer = layer_lookup[override_layer];
                    if (target_layer == null || dest_tile_pos.X >= target_layer.LayerWidth ||
                        dest_tile_pos.Y >= target_layer.LayerHeight || 
                        (!lower_layer_overridden && override_map.Layers[layer_index].Tiles[source_tile_pos.X, source_tile_pos.Y] == null))
                    {
                        continue;
                    }

                    lower_layer_overridden = true;
                    if (source_tile_pos.X >= override_layer.LayerWidth ||
                        source_tile_pos.Y >= override_layer.LayerHeight)
                    {
                        continue;
                    }

                    if (override_layer.Tiles[source_tile_pos.X, source_tile_pos.Y] == null)
                    {
                        target_layer.Tiles[dest_tile_pos.X, dest_tile_pos.Y] = null;
                        continue;
                    }

                    Tile override_tile = override_layer.Tiles[source_tile_pos.X, source_tile_pos.Y];
                    Tile new_tile = null;
                    if (!(override_tile is StaticTile))
                    {
                        if (override_tile is AnimatedTile override_animated_tile)
                        {
                            StaticTile[] tiles = new StaticTile[override_animated_tile.TileFrames.Length];
                            for (int i = 0; i < override_animated_tile.TileFrames.Length; i++)
                            {
                                StaticTile frame_tile = override_animated_tile.TileFrames[i];
                                tiles[i] = new StaticTile(target_layer, tilesheet_lookup[frame_tile.TileSheet],
                                    frame_tile.BlendMode, frame_tile.TileIndex);
                            }

                            new_tile = new AnimatedTile(target_layer, tiles, override_animated_tile.FrameInterval);
                        }
                    }
                    else
                    {
                        new_tile = new StaticTile(target_layer, tilesheet_lookup[override_tile.TileSheet],
                            override_tile.BlendMode, override_tile.TileIndex);
                    }

                    new_tile?.Properties.CopyFrom(override_tile.Properties);
                    target_layer.Tiles[dest_tile_pos.X, dest_tile_pos.Y] = new_tile;
                }
            }
        }

        __instance.map.LoadTileSheets(Game1.mapDisplayDevice);
        if (Game1.IsMasterGame || __instance.IsTemporary)
        {
            __instance._mapSeatsDirty = true;
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.performAction), [typeof(string[]), typeof(Farmer), typeof(Location)])]
    static bool performAction_Prefix(GameLocation __instance, string[] action, Location tileLocation, ref bool __result)
    {
        if (action[0] is "WarpCommunityCenter")
        {
            if (Game1.MasterPlayer.mailReceived.Contains("ccDoorUnlock") ||
                Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
            {
                __instance.playSound("doorClose", new Vector2(tileLocation.X, tileLocation.Y));
                Point commWarp = new Point(32, 23).Mirror("CommunityCenter");
                Game1.warpFarmer("CommunityCenter", commWarp.X, commWarp.Y, flip: false);
            }
            else
            {
                Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8175"));
            }

            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.getWarpPointTarget))]
    static bool getWarpPointTarget_Prefix(GameLocation __instance, ref Point __result, Point warpPointLocation,
        Character character = null)
    {
        foreach (Warp w in __instance.warps)
        {
            if (w.X == warpPointLocation.X && w.Y == warpPointLocation.Y)
            {
                __result = new Point(w.TargetX, w.TargetY);
                return false;
            }
        }

        foreach (var v in __instance.doors.Pairs)
        {
            if (!v.Key.Equals(warpPointLocation))
            {
                continue;
            }

            string[] action =
                __instance.GetTilePropertySplitBySpaces("Action", "Buildings", warpPointLocation.X,
                    warpPointLocation.Y);
            string propertyName = ArgUtility.Get(action, 0, "");
            switch (propertyName)
            {
                case "WarpCommunityCenter":
                    __result = new Point(32, 23).Mirror("CommunityCenter");
                    return false;
                case "Warp_Sunroom_Door":
                    __result = new Point(5, 13).Mirror("Sunroom");
                    return false;
                case "WarpBoatTunnel":
                    __result = new Point(17, 43).Mirror("BoatTunnel");
                    return false;
                case "WarpMensLocker":
                case "LockedDoorWarp":
                case "Warp":
                case "WarpWomensLocker":
                    break;
                default:
                    if (!propertyName.Contains("Warp"))
                    {
                        continue;
                    }

                    Game1.log.Warn(
                        $"Door in {__instance.NameOrUniqueName} ({v.Key}) has unknown warp property '{string.Join(" ", action)}', parsing with legacy logic.");
                    break;
            }

            if (!ArgUtility.TryGetPoint(action, 1, out var tile, out var error, "Point tile") ||
                !ArgUtility.TryGet(action, 3, out var locationName, out error, allowBlank: true, "string locationName"))
            {
                __instance.LogTileActionError(action, warpPointLocation.X, warpPointLocation.Y, error);
                continue;
            }

            if (!(locationName == "BoatTunnel"))
            {
                if (locationName == "Trailer" && Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade"))
                {
                    __result = new Point(13, 24).Mirror("Trailer_Big");
                    return false;
                }

                __result = new Point(tile.X, tile.Y);
                return false;
            }

            __result = new Point(17, 43).Mirror("IslandSouth");
            return false;
        }

        __result = Point.Zero;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.getWarpFromDoor))]
    static bool getWarpFromDoor_Prefix(GameLocation __instance, ref Warp __result, Point door,
        Character character = null)
    {
        try
        {
            foreach (Building building in __instance.buildings)
            {
                if (door == building.getPointForHumanDoor())
                {
                    GameLocation interior = building.GetIndoors();
                    if (interior != null)
                    {
                        __result = new Warp(door.X, door.Y, interior.NameOrUniqueName, interior.warps[0].X,
                            interior.warps[0].Y - 1, flipFarmer: false);
                        return false;
                    }
                }
            }

            string[] split = __instance.GetTilePropertySplitBySpaces("Action", "Buildings", door.X, door.Y);
            string propertyName = ArgUtility.Get(split, 0, "");
            switch (propertyName)
            {
                case "WarpCommunityCenter":
                    __result = new Warp(door.X, door.Y, "CommunityCenter", 32, 23, flipFarmer: false).Mirror();
                    return false;
                case "Warp_Sunroom_Door":
                    __result = new Warp(door.X, door.Y, "Sunroom", 5, 13, flipFarmer: false).Mirror();
                    return false;
                case "WarpBoatTunnel":
                    if (!(character is NPC))
                    {
                        __result = new Warp(door.X, door.Y, "BoatTunnel", 6, 11, flipFarmer: false).Mirror();
                        return false;
                    }

                    __result = new Warp(door.X, door.Y, "IslandSouth", 17, 43, flipFarmer: false).Mirror();
                    return false;
                case "WarpMensLocker":
                case "LockedDoorWarp":
                case "Warp":
                case "WarpWomensLocker":
                {
                    if (!ArgUtility.TryGetPoint(split, 1, out var tile, out var error, "Point tile") ||
                        !ArgUtility.TryGet(split, 3, out var locationName, out error, allowBlank: true,
                            "string locationName"))
                    {
                        __instance.LogTileActionError(split, door.X, door.Y, error);
                        return false;
                    }

                    if (!(locationName == "BoatTunnel") || !(character is NPC))
                    {
                        __result = new Warp(door.X, door.Y, locationName, tile.X, tile.Y, flipFarmer: false);
                    }

                    __result = new Warp(door.X, door.Y, "IslandSouth", 17, 43, flipFarmer: false).Mirror();
                    return false;
                }
                default:
                    if (propertyName.Contains("Warp"))
                    {
                        Game1.log.Warn(
                            $"Door in {__instance.NameOrUniqueName} ({door}) has unknown warp property '{string.Join(" ", split)}', parsing with legacy logic.");
                        goto case "WarpMensLocker";
                    }

                    return false;
            }
        }
        catch
        {
            return true;
        }
    }
}