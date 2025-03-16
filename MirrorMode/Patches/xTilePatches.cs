using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using MirrorMode.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Framework.Rendering;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Locations;
using xTile;
using xTile.Dimensions;
using xTile.Format;
using xTile.Layers;
using xTile.Tiles;

namespace MirrorMode.Patches;

[HarmonyPatch]
public static class xTilePatches
{
    /* MAP PROPERTIES TO MIRROR

     GROUP 1:
        ValidBuildRect <x> <y> <w> <h>
        SpawnMountainFarmOreRect <x> <y> <w> <h>
        ProduceArea <x> <y> <w> <h>
        ViewportClamp <x> <y> <w> <h>

    GROUP 2:
        BrookSounds [<x> <y> <type>]
        Light [<x> <y> <type>]+
        WindowLight [<x> <y> <type>]+
        Stumps [<x> <y> <unused>]+
        Trees [<x> <y> <type>]+

    GROUP 3:
        BackwoodsEntry [<x> <y>]
        BusStopEntry [<x> <y>]
        DefaultWarpLocation <x> <y>
        EntryLocation <x> <y>
        FarmCaveEntry [<x> <y>]
        FarmHouseEntry [<x> <y>]
        ForestEntry [<x> <y>]
        GrandpaShrineLocation [<x> <y>]
        GreenhouseLocation [<x> <y>]
        KitchenStandingLocation [<x> <y>]
        MailboxLocation [<x> <y>]
        PetBowlLocation <x> <y>
        ShippingBinLocation [<x> <y>]
        SpouseAreaLocation [<x> <y>]
        SpouseRoomPosition <x> <y>
        TravelingCartPosition <x> <y>
        WarpTotemEntry [<x> <y>]
        FarmHouseStarterSeedsPosition <x> <y>

    GROUP 4:
        Warp [<fromX> <fromY> <toArea> <toX> <toY>]+
        NPCWarp [<fromX> <fromY> <toArea> <toX> <toY>]+

    GROUP 5:
        DayTiles [<layer> <x> <y> <tilesheetIndex>]+
        NightTiles [<layer> <x> <y> <tilesheetIndex>]+
        FarmHouseFurniture [<id> <x> <y> <rotations>]+

    GROUP 6:
        Doors [<x> <y> <sheetId> <tileId>]+
    */

    /* TILE PROPERTIES TO MIRROR

    GROUP 1:
        TouchAction: MagicWarp <destination> <x> <y>
        TouchAction: Warp <destination> <x> <y>
        Action: ObeliskWarp <destination> <x> <y>

    GROUP 2:
        Action: Warp <x> <y> <destination>
        Action: LockedDoorWarp [<x> <y> <destination>]

    GROUP 3:
        Action: OpenShop <id> [direction] [open] [close] [<x> <y> <w> <h>]
    */

    public static Layer MirrorHorizontal(this Layer layer, IAssetName name)
    {
        TileArray tiles = layer.Tiles;
        for (int y = 0; y < layer.LayerHeight; y++)
        {
            for (int x = 0; x < layer.LayerWidth / 2; x++)
            {
                Location loc1 = new Location(x, y);
                Location loc2 = new Location(layer.LayerWidth - x - 1, y);
                (tiles[loc1], tiles[loc2]) = (tiles[loc2], tiles[loc1]);
                if (tiles[loc1] != null)
                {
                    tiles[loc1].MirrorTileData(name);
                }
                if (tiles[loc2] != null)
                {
                    tiles[loc2].MirrorTileData(name);
                }
            }
        }

        return layer;
    }

    public static void MirrorTileData(this Tile tile, IAssetName name)
    {
        foreach (var prop in tile.Properties)
        {
            var split = prop.Value.ToString().Split(' ');
            try
            {
                switch (split[0].ToLower())
                {
                    case "magicwarp":
                    case "warp" when prop.Key is "touchaction":
                    case "obeliskwarp":
                        if (Context.IsWorldReady)
                        {
                            string locName = split[1].EqualsIgnoreCase("VolcanoEntrance") ? VolcanoDungeon.GetLevelName(0) : split[1];
                            var guaranteedWidth = Game1.getLocationFromName(locName).Map.TileWidth();
                            split[2] = ((guaranteedWidth) - int.Parse(split[2]) - 1)
                                .ToString();
                            prop.Value.m_value = string.Join(" ", split);
                        }
                        else if (ModEntry.LocationToMapLookup.TryGetValue(split[1], out var mapName) &&
                            ModEntry.MapToWidthLookup.TryGetValue(mapName, out var width))
                        {
                            split[2] = ((width) - int.Parse(split[2]) - 1)
                                .ToString();
                            prop.Value.m_value = string.Join(" ", split);
                        }
                        else
                        {
                            ModEntry.MapsToRetry.Add(PathUtilities.NormalizeAssetName(name.BaseName));
                        }
                        break;
                    case "warp" when prop.Key.ToLower() is "action":
                    case "lockeddoorwarp":
                        if (split.Length < 4) break;
                        if (Context.IsWorldReady)
                        {
                            string locName = split[3].EqualsIgnoreCase("VolcanoEntrance") ? VolcanoDungeon.GetLevelName(0) : split[3];
                            var guaranteedWidth = Game1.getLocationFromName(locName).Map.TileWidth();
                            split[1] = ((guaranteedWidth) - int.Parse(split[1]) - 1)
                                .ToString();
                            prop.Value.m_value = string.Join(" ", split);
                        }
                        else if (ModEntry.LocationToMapLookup.TryGetValue(split[3], out var mapName2) &&
                                 ModEntry.MapToWidthLookup.TryGetValue(mapName2, out var width2))
                        {
                            split[1] = ((width2) - int.Parse(split[1]) - 1)
                                .ToString();
                            prop.Value.m_value = string.Join(" ", split);
                        }
                        else
                        {
                            ModEntry.MapsToRetry.Add(PathUtilities.NormalizeAssetName(name.BaseName));
                        }


                        break;
                    case "openshop":
                        if (split.Length < 6) break;
                        if (Context.IsWorldReady)
                        {
                            string locName = split[5].EqualsIgnoreCase("VolcanoEntrance") ? VolcanoDungeon.GetLevelName(0) : split[5];
                            var guaranteedWidth = Game1.getLocationFromName(locName).Map.TileWidth();
                            split[2] = ((guaranteedWidth) - int.Parse(split[2]) - int.Parse(split[4]) - 1)
                                .ToString();
                            prop.Value.m_value = string.Join(" ", split);
                        }
                        else if (ModEntry.LocationToMapLookup.TryGetValue(split[5], out var mapName3) &&
                                 ModEntry.MapToWidthLookup.TryGetValue(mapName3, out var width3))
                        {
                            split[5] = ((width3) - int.Parse(split[5]) - int.Parse(split[7])).ToString();
                            prop.Value.m_value = string.Join(" ", split);
                        }
                        else
                        {
                            ModEntry.MapsToRetry.Add(PathUtilities.NormalizeAssetName(name.BaseName));
                        }
                        break;
                    default:
                        ModEntry.ModMonitor.LogOnce($"Unable to mirror TileData: {prop.Key} {prop.Value}", LogLevel.Debug);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error when mirroring tile property '{prop.Key}' with value '{prop.Value}' in asset '{name}': {ex.Message}");
                ModEntry.MapsToRetry.Add(PathUtilities.NormalizeAssetName(name.BaseName));
            }
        }
    }

    public static void MirrorProperties(this Map map, IAssetName name)
    {
        foreach (var prop in map.Properties)
        {
            try
            {
                switch (prop.Key.ToLower())
                {
                    case "validbuildrect":
                    case "spawnmountainfarmorerect":
                    case "producearea":
                    case "viewportclamp":
                        if (string.IsNullOrEmpty(prop.Value)) break;
                        var group1Props = prop.Value.ToString().Split(' ');
                        group1Props[0] = (map.TileWidth() - int.Parse(group1Props[0]) -
                                          int.Parse(group1Props[2]))
                            .ToString(); // Gotta subtract the width. Can't have a negatively width'd rectangle, after all.
                        prop.Value.m_value = string.Join(" ", group1Props);
                        break;
                    case "brooksounds":
                    case "light":
                    case "windowlight":
                    case "stumps":
                    case "trees":
                        if (string.IsNullOrEmpty(prop.Value)) break;
                        var group2Props = prop.Value.ToString().Split(' ');
                        for (int i = 0; i < group2Props.Length; i += 3)
                        {
                            group2Props[i] = ((map.TileWidth()) - int.Parse(group2Props[i]) - 1)
                                .ToString();
                        }

                        prop.Value.m_value = string.Join(" ", group2Props);
                        break;
                    case "backwoodsentry":
                    case "busstopentry":
                    case "defaultwarplocation":
                    case "entrylocation":
                    case "farmcaveentry":
                    case "farmhouseentry":
                    case "forestentry":
                    case "grandpashrinelocation":
                    case "greenhouselocation":
                    case "kitchenstandinglocation":
                    case "mailboxlocation":
                    case "petbowllocation":
                    case "shippingbinlocation":
                    case "spousearealocation":
                    case "spouseroomposition":
                    case "travelingcartposition":
                    case "warptotementry":
                    case "farmhousestarterseedsposition":
                        if (string.IsNullOrEmpty(prop.Value)) break;
                        var group3Props = prop.Value.ToString().Split(' ');
                        group3Props[0] = ((map.TileWidth()) - int.Parse(group3Props[0]) - 1)
                            .ToString();
                        prop.Value.m_value = string.Join(" ", group3Props);
                        break;
                    case "warp":
                    case "npcwarp":
                        var group4Props = prop.Value.ToString().Split(' ');
                        for (int i = 0; i < group4Props.Length; i += 5)
                        {
                            group4Props[i] = ((map.TileWidth()) - int.Parse(group4Props[i]) - 1)
                                .ToString();
                            if (Context.IsWorldReady)
                            {
                                string locName = group4Props[i+2].EqualsIgnoreCase("VolcanoEntrance") ? VolcanoDungeon.GetLevelName(0) : group4Props[i + 2];
                                var guaranteedWidth = Game1.getLocationFromName(locName).Map.TileWidth();
                                group4Props[i + 3] = ((guaranteedWidth) - int.Parse(group4Props[i + 3]) - 1)
                                    .ToString();
                            }
                            else if (ModEntry.LocationToMapLookup.TryGetValue(group4Props[i + 2], out var mapName) &&
                                ModEntry.MapToWidthLookup.TryGetValue(mapName, out var width))
                            {
                                group4Props[i + 3] = ((width) - int.Parse(group4Props[i + 3]) - 1)
                                    .ToString();
                            }
                            else
                            {
                                ModEntry.MapsToRetry.Add(PathUtilities.NormalizeAssetName(name.BaseName));
                            }
                        }

                        prop.Value.m_value = string.Join(" ", group4Props);
                        break;
                    case "farmhousefurniture":
                    case "daytiles":
                    case "nighttiles":
                        var group5Props = prop.Value.ToString().Split(' ');
                        for (int i = 0; i < group5Props.Length; i += 4)
                        {
                            group5Props[i + 1] =
                                ((map.TileWidth()) - int.Parse(group5Props[i + 1]) - 1)
                                .ToString();
                            if (prop.Key.ToLower() == "farmhousefurniture")
                            {
                                group5Props[i + 3] +=
                                    (int.Parse(group5Props[i + 3]) + 2)
                                    .ToString(); // I think this mirrors it horizontally?
                            }
                        }

                        prop.Value.m_value = string.Join(" ", group5Props);
                        break;
                    case "doors":
                        var group6Props = prop.Value.ToString().Split(' ');
                        for (int i = 0; i < group6Props.Length; i += 4)
                        {
                            group6Props[i] = ((map.TileWidth()) - int.Parse(group6Props[i]) - 1)
                                .ToString();
                        }

                        prop.Value.m_value = string.Join(" ", group6Props);
                        break;
                    default:
                        ModEntry.ModMonitor.LogOnce($"Unable to mirror map property: {prop.Key} {prop.Value}", LogLevel.Debug);
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error when mirroring property '{prop.Key}' with value '{prop.Value}' in asset '{name}': {e.Message}");
                ModEntry.MapsToRetry.Add(PathUtilities.NormalizeAssetName(name.BaseName));
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TbinFormat), nameof(TbinFormat.Load))]
    static void TbinLoad_Postfix(ref Map __result)
    {
        // __result.MirrorProperties();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TideFormat), nameof(TideFormat.Load))]
    static void TideLoad_Postfix(ref Map __result)
    {
        // __result.MirrorProperties();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Map), nameof(Map.AddLayer))]
    static void LoadTile_Prefix(ref Layer layer)
    {
        // layer = layer.MirrorHorizontal();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SDisplayDevice), nameof(SDisplayDevice.GetSpriteEffects))]
    static void GetSpriteEffects_Prefix(ref SpriteEffects __result)
    {
        if (__result == SpriteEffects.None) __result = SpriteEffects.FlipHorizontally;
        else if (__result == SpriteEffects.FlipHorizontally) __result = SpriteEffects.None;
    }
}