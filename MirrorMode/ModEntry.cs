﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml;
using Force.DeepCloner;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using MirrorMode.Helpers;
using MirrorMode.Patches;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Rendering;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Characters;
using StardewValley.Locations;
using TMXTile;
using xTile;
using xTile.Dimensions;
using xTile.Display;
using xTile.Format;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace MirrorMode
{
    /* TODO: FUNCTIONS WITH HARDCODED COORDINATES

        Farm.doDailyMountainFarmUpdate()

        FarmHouse.getFireplacePoint()
        FarmHouse.GetCribBounds()
        FarmHouse.getEntryLocation()
        FarmHouse.getForbiddenPetWarpTiles

    */

    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;

        internal static HashSet<string> WarpEdits { get; } = new();

        internal static Dictionary<string, string> LocationToMapLookup { get; } = new();
        internal static Dictionary<string, int> MapToWidthLookup { get; } = new();
        internal static HashSet<string> MapsToRetry = new();
        internal static bool CharactersReady = true;

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            ModMonitor = Monitor;
            Harmony = new Harmony(ModManifest.UniqueID);

            Harmony.PatchAll();

            Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            Helper.Events.Player.Warped += this.OnWarped;
            Helper.Events.Content.AssetRequested += this.OnAssetRequested;
            Helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
            Helper.Events.Content.AssetReady += this.OnAssetReady;
            Helper.Events.Specialized.LoadStageChanged += this.OnLoadStageChanged;
            Helper.Events.GameLoop.ReturnedToTitle += (_, _) =>
            {
                Helper.GameContent.InvalidateCache(asset => true);
            };
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.F2)
            {
                foreach (var prop in Game1.currentLocation.Map.Properties)
                {
                    Log.Warn(prop.Key + ": " + prop.Value);
                }
            }

            if (e.Button is SButton.F5)
            {
                var mon = ModMonitor as Monitor;
                mon?.LogOnceCache.Clear();
            }

            if (e.Button is SButton.F6)
            {
                Log.Alert("Repatching...");
                Harmony.PatchAll();
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (typeof(Map).IsAssignableFrom(e.DataType))
            {
                MapsToRetry.Remove(PathUtilities.NormalizeAssetName(e.NameWithoutLocale.BaseName));
                e.Edit((asset) =>
                {
                    var map = asset.AsMap().Data;
                    foreach (var layer in map.m_layers)
                    {
                        layer.MirrorHorizontal(asset.NameWithoutLocale);
                    }
                    map.MirrorProperties(asset.NameWithoutLocale);
                    MapToWidthLookup.TryAdd(PathUtilities.NormalizeAssetName(asset.NameWithoutLocale.BaseName), map.TileWidth());
                }, AssetEditPriority.Late + 696969);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Characters"))
            {
                e.Edit(asset =>
                {
                    var charData = asset.AsDictionary<string, CharacterData>().Data;
                    foreach (var chara in charData.Values)
                    {
                        if (chara.Home is not null)
                        {
                            foreach (var home in chara.Home)
                            {
                                if (LocationToMapLookup.TryGetValue(home.Location, out var loca) &&
                                    MapToWidthLookup.TryGetValue(loca, out var width))
                                {
                                    Monitor.LogOnce("Mirroring home tile for " + chara.DisplayName + " in " + loca, LogLevel.Alert);
                                    home.Tile = home.Tile.Mirror(width);
                                }
                                else CharactersReady = false;
                            }
                        }
                    }
                });
            }
        }

        private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(asset => asset.IsEquivalentTo("Data/Locations")))
            {
                LocationToMapLookup.Clear();
            }

            foreach (var mapAsset in e.NamesWithoutLocale.Where(asset => MapToWidthLookup.ContainsKey(asset.BaseName)))
            {
                MapToWidthLookup.Remove(mapAsset.BaseName);
                MapsToRetry.Remove(mapAsset.BaseName);
            }
        }

        private void OnLoadStageChanged(object? sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage is LoadStage.Loaded or LoadStage.Ready)
            {
                if (MapsToRetry.Any())
                {
                    Log.Alert("Retrying " + MapsToRetry.Count + " maps");
                    foreach (var map in MapsToRetry)
                    {
                        MapToWidthLookup.Remove(map);
                    }
                    Helper.GameContent.InvalidateCache(asset => MapsToRetry.Contains(PathUtilities.NormalizeAssetName(asset.NameWithoutLocale.BaseName)));
                }
            }
        }

        private void OnAssetReady(object? sender, AssetReadyEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Locations"))
            {
                var locationData = DataLoader.Locations(Game1.content);
                foreach (var location in locationData)
                {
                    if (location.Value.CreateOnLoad is not null) LocationToMapLookup.TryAdd(location.Key, PathUtilities.NormalizeAssetName(location.Value.CreateOnLoad.MapPath));
                }

                if (!CharactersReady) Helper.GameContent.InvalidateCache("Data/Characters");
                Helper.GameContent.InvalidateCache(asset =>
                {
                    if (MapsToRetry.Contains(PathUtilities.NormalizeAssetName(asset.NameWithoutLocale.BaseName)))
                    {
                        Log.Error("Retrying " + asset.NameWithoutLocale.BaseName);
                        return true;
                    }
                
                    return false;
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Characters"))
            {
                Utility.ForEachVillager(chara =>
                {
                    chara.reloadDefaultLocation();
                    return true;
                });
            }

            if (e.NameWithoutLocale.BaseName.Contains("Maps/"))
            {
                if (!CharactersReady) Helper.GameContent.InvalidateCache("Data/Characters");
            }
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // Helper.GameContent.InvalidateCache("Data/Locations");
            // if (!e.IsLocalPlayer || e.OldLocation is FarmHouse || e.NewLocation is FarmHouse ||
            //     e.OldLocation.Name is "Greenhouse" || e.NewLocation.Name is "Greenhouse") return;
            // e.Player.setTileLocation(e.Player.Tile.Mirror(e.NewLocation.Map.TileWidth()));
            Log.Alert(e.Player.Tile);
            return;
        }
    }

    static class MirroringExtensions
    {
        public static int TileWidth(this Map map)
        {
            return map.DisplayWidth / Game1.tileSize;
        }

        public static Vector2 Mirror(this Vector2 vector, int mapWidth)
        {
            return new Vector2(mapWidth - vector.X - 1, vector.Y);
        }

        public static Point Mirror(this Point point, int mapWidth)
        {
            return new Point(mapWidth - point.X - 1, point.Y);
        }

        public static Microsoft.Xna.Framework.Rectangle Mirror(this Microsoft.Xna.Framework.Rectangle rect, int mapWidth)
        {
            return new Microsoft.Xna.Framework.Rectangle(mapWidth - rect.X - (rect.Width - 1), rect.Y, rect.Width, rect.Height);
        }
    }
}