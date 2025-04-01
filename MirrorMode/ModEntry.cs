using System;
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
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using TMXTile;
using xTile;
using xTile.Dimensions;
using xTile.Display;
using xTile.Format;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;
using Vector2 = Microsoft.Xna.Framework.Vector2;

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

        public static TasCatcher TasCatcher { get; set; } = null!;

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
            // Helper.Events.GameLoop.SaveCreating += this.OnSaveCreating;
            // Helper.Events.GameLoop.Saved += this.OnSaved;
            // Helper.Events.GameLoop.Saving += this.OnSaving;
            
            TasCatcher = Helper.Data.ReadJsonFile<TasCatcher>("tasCache.json") ?? new TasCatcher();
        }
        
        // private void OnSaveCreating(object? sender, SaveCreatingEventArgs e)
        // {
        //     Game1.getFarm().modData["Spiderbuttons.MirrorMode"] = "save";
        // }
        //
        // private void OnSaving(object? sender, SavingEventArgs e)
        // {
        //     if (!Game1.getFarm().modData.ContainsKey("Spiderbuttons.MirrorMode")) return;
        //     MirrorEverything();
        //     Game1.getFarm().modData.Remove("Spiderbuttons.MirrorMode");
        // }
        //
        // private void OnSaved(object? sender, SavedEventArgs e)
        // {
        //     if (Game1.getFarm().modData.ContainsKey("Spiderbuttons.MirrorMode")) return;
        //     MirrorEverything();
        //     Game1.getFarm().modData["Spiderbuttons.MirrorMode"] = "true";
        // }
        //
        // private void MirrorEverything()
        // {
        //     Utility.ForEachLocation(loc =>
        //     {
        //         foreach (var building in loc.buildings)
        //         {
        //             Vector2 mirroredLocation =
        //                 new Vector2(building.tileX.Value, building.tileY.Value).Mirror(building.GetParentLocation().Map
        //                     .TileWidth()) - new Vector2(building.tilesWide.Value - 1, 0);
        //             building.modData["Spiderbuttons.MirrorMode"] = $"{mirroredLocation.X},{mirroredLocation.Y}";
        //             building.isMoving = true;
        //             building.GetParentLocation().buildStructure(building,
        //                 new Vector2(building.tileX.Value - 1000, building.tileY.Value), Game1.player);
        //         }
        //
        //         foreach (var building in loc.buildings)
        //         {
        //             if (!building.modData.ContainsKey("Spiderbuttons.MirrorMode")) continue;
        //             string[] split = building.modData["Spiderbuttons.MirrorMode"].Split(',');
        //             Vector2 target = new Vector2(float.Parse(split[0]), float.Parse(split[1]));
        //             building.GetParentLocation().buildStructure(building, target, Game1.player);
        //             building.modData.Remove("Spiderbuttons.MirrorMode");
        //             building.isMoving = false;
        //         }
        //
        //         foreach (var tf in loc.terrainFeatures.Values)
        //         {
        //             if (tf is HoeDirt { Pot: not null }) continue;
        //             Vector2 mirroredLocation = new Vector2(tf.Tile.X, tf.Tile.Y).Mirror(loc.Map.TileWidth());
        //             tf.modData["Spiderbuttons.MirrorMode"] = $"{mirroredLocation.X},{mirroredLocation.Y}";
        //             tf.Tile = new Vector2(tf.Tile.X - 1000, tf.Tile.Y);
        //         }
        //
        //         foreach (var tf in loc.terrainFeatures.Values)
        //         {
        //             if (tf is HoeDirt { Pot: not null }) continue;
        //             if (!tf.modData.ContainsKey("Spiderbuttons.MirrorMode")) continue;
        //             string[] split = tf.modData["Spiderbuttons.MirrorMode"].Split(',');
        //             Vector2 target = new Vector2(float.Parse(split[0]), float.Parse(split[1]));
        //             tf.Tile = target;
        //             tf.modData.Remove("Spiderbuttons.MirrorMode");
        //         }
        //         
        //         foreach (var tf in loc.largeTerrainFeatures)
        //         {
        //             Vector2 mirroredLocation = new Vector2(tf.Tile.X, tf.Tile.Y).Mirror(loc.Map.TileWidth()) -
        //                                        new Vector2(1, 0);
        //             tf.modData["Spiderbuttons.MirrorMode"] = $"{mirroredLocation.X},{mirroredLocation.Y}";
        //             tf.Tile = new Vector2(tf.Tile.X - 1000, tf.Tile.Y);
        //         }
        //         
        //         foreach (var tf in loc.largeTerrainFeatures)
        //         {
        //             if (!tf.modData.ContainsKey("Spiderbuttons.MirrorMode")) continue;
        //             string[] split = tf.modData["Spiderbuttons.MirrorMode"].Split(',');
        //             Vector2 target = new Vector2(float.Parse(split[0]), float.Parse(split[1]));
        //             tf.Tile = target;
        //             tf.modData.Remove("Spiderbuttons.MirrorMode");
        //         }
        //         
        //         foreach (var obj in loc.Objects.Values)
        //         {
        //             Vector2 mirroredLocation = new Vector2(obj.TileLocation.X, obj.TileLocation.Y).Mirror(loc.Map.TileWidth());
        //             obj.modData["Spiderbuttons.MirrorMode"] = $"{mirroredLocation.X},{mirroredLocation.Y}";
        //             obj.TileLocation = new Vector2(obj.TileLocation.X - 1000, obj.TileLocation.Y);
        //         }
        //         
        //         foreach (var obj in loc.Objects.Values)
        //         {
        //             if (!obj.modData.ContainsKey("Spiderbuttons.MirrorMode")) continue;
        //             string[] split = obj.modData["Spiderbuttons.MirrorMode"].Split(',');
        //             Vector2 target = new Vector2(float.Parse(split[0]), float.Parse(split[1]));
        //             obj.TileLocation = target;
        //             obj.modData.Remove("Spiderbuttons.MirrorMode");
        //         }
        //         
        //         foreach (var furniture in loc.furniture)
        //         {
        //             furniture.SetPlacement(furniture.TileLocation.Mirror(loc.Map.TileWidth() - furniture.getTilesWide() + 1));
        //         }
        //
        //         // foreach (var chara in loc.characters)
        //         // {
        //         //     if (!chara.IsVillager) continue;
        //         //     chara.setTilePosition(chara.DefaultPosition.ToPoint());
        //         // }
        //
        //         return true;
        //     });
        // }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.F2)
            {
                var tile = Utility.PointToVector2(Game1.getMousePosition()) +
                           new Vector2(Game1.viewport.X, Game1.viewport.Y);
                TemporaryAnimatedSprite? sprite = null;
                if (Context.IsWorldReady)
                {
                    sprite = Game1.currentLocation.TemporarySprites.FirstOrDefault(s =>
                    {
                        if (new Microsoft.Xna.Framework.Rectangle((int)s.initialPosition.X, (int)s.initialPosition.Y,
                                s.sourceRect.Width * 4,
                                s.sourceRect.Height * 4).Contains(tile))
                        {
                            return true;
                        }

                        return false;
                    });
                }
                else if (Game1.activeClickableMenu is TitleMenu tm)
                {
                    sprite = tm.tempSprites.FirstOrDefault(s =>
                    {
                        if (new Microsoft.Xna.Framework.Rectangle((int)s.initialPosition.X, (int)s.initialPosition.Y,
                                s.sourceRect.Width * 4,
                                s.sourceRect.Height * 4).Contains(tile))
                        {
                            return true;
                        }

                        return false;
                    });
                }

                if (sprite is not null)
                {
                    Game1.playSound("breakingGlass");
                    Log.Warn(sprite.text);
                    sprite.HighlightForDebug();
                    if (Context.IsWorldReady)
                    {
                        sprite.Position = (sprite.Position / 64).Mirror(Game1.currentLocation.Map.TileWidth()) * 64;
                        sprite.initialPosition =
                            (sprite.initialPosition / 64).Mirror(Game1.currentLocation.Map.TileWidth()) * 64;
                    }

                    TasCatcher.Blacklist(sprite.text);
                }
            }

            if (e.Button is SButton.F5)
            {
                // var mon = ModMonitor as Monitor;
                // mon.LogOnceCache.Clear();
                // Helper.GameContent.InvalidateCache(ass => true);

                Game1.netWorldState.Value.canDriveYourselfToday.Value = true;
            }

            if (e.Button is SButton.F6)
            {
                var tile = Utility.PointToVector2(Game1.getMousePosition()) +
                           new Vector2(Game1.viewport.X, Game1.viewport.Y);
                var sprite = Game1.currentLocation.TemporarySprites.FirstOrDefault(s =>
                {
                    if (new Microsoft.Xna.Framework.Rectangle((int)s.initialPosition.X, (int)s.initialPosition.Y, s.sourceRect.Width * 4,
                            s.sourceRect.Height * 4).Contains(tile))
                    {
                        return true;
                    }
                    return false;
                });
                if (sprite is not null)
                {
                    Game1.playSound("breakingGlass");
                    Log.Warn(sprite.text);
                    sprite.flicker = !sprite.flicker;
                    sprite.Position = (sprite.Position / 64).Mirror(Game1.currentLocation.Map.TileWidth()) * 64;
                    sprite.initialPosition = (sprite.initialPosition / 64).Mirror(Game1.currentLocation.Map.TileWidth()) * 64;
                    // TasCatcher.Blacklist(sprite.text);
                }
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Minigames/TitleButtons"))
            {
                e.LoadFromModFile<Texture2D>("assets/Minigames/TitleButtons", AssetLoadPriority.High);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("VolcanoLayouts/Layouts"))
            {
                e.LoadFromModFile<Texture2D>("assets/VolcanoLayouts/Layouts", AssetLoadPriority.High);
            }
            
            if (typeof(Map).IsAssignableFrom(e.DataType) && !e.NameWithoutLocale.BaseName.Contains("Volcano"))
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
                                    home.Tile = home.Tile.Mirror(width);
                                }
                                else CharactersReady = false;
                            }
                        }
                        
                        if (chara.SpouseRoom is not null && chara.SpouseRoom.MapAsset is null)
                        {
                            chara.SpouseRoom.MapSourceRect = chara.SpouseRoom.MapSourceRect.Mirror(29);
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
            // Log.Alert(e.Player.Tile);
            if (e.NewLocation.Name.Equals("Caldera") && e.Player.Tile.X is 11)
            {
                e.Player.Position = new Vector2(36, e.Player.Tile.Y) * 64;
            } else if (e.NewLocation.Name.Equals("Caldera"))
            {
                e.Player.Position = new Vector2(26, e.Player.Tile.Y) * 64;
            } else if (e.NewLocation.Name.Equals("IslandNorth") && e.OldLocation.Name.Contains("Volcano"))
            {
                Vector2 vec = new Vector2(e.Player.Tile.X, e.Player.Tile.Y).Mirror(e.NewLocation.Map.TileWidth());
                e.Player.Position = new Vector2(vec.X, e.Player.Tile.Y) * 64;
            } else if (e.OldLocation.Name.Equals("IslandNorth") && e.NewLocation.Name.Contains("Volcano") && e.Player.Tile.Y is not 52)
            {
                Vector2 vec = new Vector2(e.Player.Tile.X, e.Player.Tile.Y).Mirror(e.NewLocation.Map.TileWidth());
                e.Player.Position = new Vector2(vec.X, e.Player.Tile.Y) * 64;
            }
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

        public static Vector2 MirrorPixels(this Vector2 vector, int mapWidth)
        {
            // This 28 hardcoding might come back to bite me later. Don't know why it's not perfect if I do 64.
            return new Vector2(mapWidth - vector.X - 28, vector.Y);
        }
        
        public static Vector2 MirrorForUI(this Vector2 vector, int screenWidth, int boundWidth)
        {
            return new Vector2(screenWidth - vector.X - boundWidth, vector.Y);
        }

        public static Vector2 Mirror(this Vector2 vector, string location)
        {
            return new Vector2(Game1.getLocationFromName(location).Map.TileWidth() - vector.X - 1, vector.Y);
        }

        public static Point Mirror(this Point point, int mapWidth)
        {
            return new Point(mapWidth - point.X - 1, point.Y);
        }
        
        public static Point Mirror(this Point point, string location)
        {
            return new Point(Game1.getLocationFromName(location).Map.TileWidth() - point.X - 1, point.Y);
        }

        public static Microsoft.Xna.Framework.Rectangle Mirror(this Microsoft.Xna.Framework.Rectangle rect, int mapWidth)
        {
            return new Microsoft.Xna.Framework.Rectangle(mapWidth - rect.X - (rect.Width - 1), rect.Y, rect.Width, rect.Height);
        }
        
        public static Microsoft.Xna.Framework.Rectangle Mirror(this Microsoft.Xna.Framework.Rectangle rect, string location)
        {
            return new Microsoft.Xna.Framework.Rectangle(Game1.getLocationFromName(location).Map.TileWidth() - rect.X - (rect.Width - 1), rect.Y, rect.Width, rect.Height);
        }
        
        public static Microsoft.Xna.Framework.Rectangle MirrorForUI(this Microsoft.Xna.Framework.Rectangle rect, int screenWidth, int boundWidth)
        {
            return new Microsoft.Xna.Framework.Rectangle(screenWidth - rect.X - boundWidth, rect.Y, rect.Width, rect.Height);
        }

        public static Warp Mirror(this Warp warp)
        {
            var warpVector = new Vector2(warp.TargetX, warp.TargetY).Mirror(warp.TargetName);
            return new Warp(warp.X, warp.Y, warp.TargetName, (int)warpVector.X, (int)warpVector.Y, flipFarmer: warp.flipFarmer.Value);
        }
    }
}