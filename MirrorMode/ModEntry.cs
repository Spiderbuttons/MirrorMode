using System;
using System.Collections.Generic;
using System.Linq;
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
using StardewModdingAPI.Framework.Rendering;
using StardewValley.Extensions;
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
    internal sealed class ModEntry : Mod
    {
        internal static IModHelper ModHelper { get; set; } = null!;
        internal static IMonitor ModMonitor { get; set; } = null!;
        internal static Harmony Harmony { get; set; } = null!;

        internal static HashSet<string> MapWarpCache = new();

        public override void Entry(IModHelper helper)
        {
            ModHelper = helper;
            ModMonitor = Monitor;
            Harmony = new Harmony(ModManifest.UniqueID);

            Harmony.PatchAll();

            Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            Helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            Helper.Events.Player.Warped += this.OnWarped;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.F2)
            {
                MapWarpCache.Clear();
            }
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer) return;
            e.Player.Position = new Vector2((e.NewLocation.Map.DisplayWidth - e.Player.Position.X) - 64,
                e.Player.Position.Y);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // var renderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            // foreach (var target in renderTargets) {
            //     var tex = target.RenderTarget as Texture2D;
            //     if (tex == null) continue;
            //     // if (ModEntry.TexDataCache.ContainsKey(tex.Name)) return;
            //     // horizontally mirror the pixels
            //     var data = new Color[tex.Width * tex.Height];
            //     tex.GetData(data);
            //     var newData = new Color[data.Length];
            //     for (int y = 0; y < tex.Height; y++)
            //     {
            //         for (int x = 0; x < tex.Width; x++)
            //         {
            //             newData[y * tex.Width + x] = data[y * tex.Width + (tex.Width - x - 1)];
            //         }
            //     }
            //     tex.SetData(newData);
            //     ModEntry.TexDataCache.TryAdd(tex.Name, tex.GetHashCode());
            // }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            // // Log.Debug("ggg");
            // var renderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            // foreach (var target in renderTargets) {
            //     Log.Warn("hhhh");
            //     var tex = target.RenderTarget as Texture2D;
            //     // Log.Debug("Waaaaaa");
            //     if (tex == null) continue;
            //     // Log.Debug("waaa");
            //     // if (ModEntry.TexDataCache.ContainsKey(tex.Name)) return;
            //     // horizontally mirror the pixels
            //     var data = new Color[tex.Width * tex.Height];
            //     tex.GetData(data);
            //     var newData = new Color[data.Length];
            //     for (int y = 0; y < tex.Height; y++)
            //     {
            //         for (int x = 0; x < tex.Width; x++)
            //         {
            //             newData[y * tex.Width + x] = data[y * tex.Width + (tex.Width - x - 1)];
            //         }
            //     }
            //     tex.SetData(newData);
            //     ModEntry.TexDataCache.TryAdd(tex.Name, tex.GetHashCode());
            // }
        }
    }

    [HarmonyPatch]
    public static class TiledPatches
    {
        /* TODO: Map Properties
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
            NPCWarp [<fromX> <fromY> <toArea> <toX> <toY>]+ ! IMPORTANT ! Can reuse "Warp"

        GROUP 5:
            DayTiles [<layer> <x> <y> <tilesheetIndex>]+
            NightTiles [<layer> <x> <y> <tilesheetIndex>]+
            FarmHouseFurniture [<id> <x> <y> <rotations>]+

        GROUP 6:
            Doors [<x> <y> <sheetId> <tileId>]+
        */

        public static Layer MirrorHorizontal(this Layer layer)
        {
            TileArray tiles = layer.Tiles;
            for (int y = 0; y < layer.LayerHeight; y++)
            {
                for (int x = 0; x < layer.LayerWidth / 2; x++)
                {
                    Location loc1 = new Location(x, y);
                    Location loc2 = new Location(layer.LayerWidth - x - 1, y);
                    (tiles[loc1], tiles[loc2]) = (tiles[loc2], tiles[loc1]);
                }
            }

            return layer;
        }

        public static void MirrorProperties(this Map map)
        {
            foreach (var prop in map.Properties)
            {
                try
                {
                    switch (prop.Key)
                    {
                        case "ValidBuildRect":
                        case "SpawnMountainFarmOreRect":
                        case "ProduceArea":
                        case "ViewportClamp":
                            if (string.IsNullOrEmpty(prop.Value)) break;
                            var group1Props = prop.Value.ToString().Split(' ');
                            group1Props[0] = ((map.DisplayWidth / Game1.tileSize) - int.Parse(group1Props[0]) -
                                              int.Parse(group1Props[2])).ToString(); // Gotta subtract the width. Can't have a negatively width'd rectangle, after all.
                            prop.Value.m_value = string.Join(" ", group1Props);
                            break;
                        case "BrookSounds":
                        case "Light":
                        case "WindowLight":
                        case "Stumps":
                        case "Trees":
                            if (string.IsNullOrEmpty(prop.Value)) break;
                            var group2Props = prop.Value.ToString().Split(' ');
                            for (int i = 0; i < group2Props.Length; i += 3)
                            {
                                group2Props[i] = ((map.DisplayWidth / Game1.tileSize) - int.Parse(group2Props[i]) - 1)
                                    .ToString();
                            }
                            prop.Value.m_value = string.Join(" ", group2Props);
                            break;
                        case "BackwoodsEntry":
                        case "BusStopEntry":
                        case "DefaultWarpLocation":
                        case "EntryLocation":
                        case "FarmCaveEntry":
                        case "FarmHouseEntry":
                        case "ForestEntry":
                        case "GrandpaShrineLocation":
                        case "GreenhouseLocation":
                        case "KitchenStandingLocation":
                        case "MailboxLocation":
                        case "PetBowlLocation":
                        case "ShippingBinLocation":
                        case "SpouseAreaLocation":
                        case "SpouseRoomPosition":
                        case "TravelingCartPosition":
                        case "WarpTotemEntry":
                        case "FarmHouseStarterSeedsPosition":
                            if (string.IsNullOrEmpty(prop.Value)) break;
                            var group3Props = prop.Value.ToString().Split(' ');
                            group3Props[0] = ((map.DisplayWidth / Game1.tileSize) - int.Parse(group3Props[0]) - 1)
                                .ToString();
                            prop.Value.m_value = string.Join(" ", group3Props);
                            break;
                        case "Warp":
                        case "NPCWarp":
                            var group4Props = prop.Value.ToString().Split(' ');
                            for (int i = 0; i < group4Props.Length; i += 5)
                            {
                                group4Props[i] = ((map.DisplayWidth / Game1.tileSize) - int.Parse(group4Props[i]) - 1)
                                    .ToString();
                            }
                            prop.Value.m_value = string.Join(" ", group4Props);
                            break;
                        case "FarmHouseFurniture":
                        case "DayTiles":
                        case "NightTiles":
                            var group5Props = prop.Value.ToString().Split(' ');
                            for (int i = 0; i < group5Props.Length; i += 4)
                            {
                                group5Props[i + 1] =
                                    ((map.DisplayWidth / Game1.tileSize) - int.Parse(group5Props[i + 1]) - 1)
                                    .ToString();
                                if (prop.Key == "FarmHouseFurniture")
                                {
                                    group5Props[i + 3] += (int.Parse(group5Props[i + 3]) + 2).ToString(); // I think this mirrors it horizontally?
                                }
                            }
                            prop.Value.m_value = string.Join(" ", group5Props);
                            break;
                        case "Doors":
                            var group6Props = prop.Value.ToString().Split(' ');
                            for (int i = 0; i < group6Props.Length; i += 4)
                            {
                                group6Props[i] = ((map.DisplayWidth / Game1.tileSize) - int.Parse(group6Props[i]) - 1)
                                    .ToString();
                            }
                            prop.Value.m_value = string.Join(" ", group6Props);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Error when mirroring property '{prop.Key}' with value '{prop.Value}': {e.Message}");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TbinFormat), nameof(TbinFormat.Load))]
        static void TbinLoad_Postfix(ref Map __result)
        {
            __result.MirrorProperties();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TideFormat), nameof(TideFormat.Load))]
        static void TideLoad_Postfix(ref Map __result)
        {
            __result.MirrorProperties();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Map), nameof(Map.AddLayer))]
        static void LoadTile_Prefix(ref Layer layer)
        {
            layer = layer.MirrorHorizontal();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SDisplayDevice), nameof(SDisplayDevice.GetSpriteEffects))]
        static void GetSpriteEffects_Prefix(ref SpriteEffects __result)
        {
            if (__result == SpriteEffects.None) __result = SpriteEffects.FlipHorizontally;
            else if (__result == SpriteEffects.FlipHorizontally) __result = SpriteEffects.None;
        }
    }

    [HarmonyPatch(typeof(SpriteBatcher), nameof(SpriteBatcher.DrawBatch))]
    public class Patches
    {
        static void Prefix(SpriteBatcher __instance, ref Effect effect)
        {
            // foreach (var item in __instance._batchItemList)
            // {
            //     VertexPositionColorTexture TR = item.vertexTR;
            //     VertexPositionColorTexture TL = item.vertexTL;
            //     VertexPositionColorTexture BR = item.vertexBR;
            //     VertexPositionColorTexture BL = item.vertexBL;
            //
            //     var temp = TR.Position.X;
            //     TR.Position.X = TL.Position.X;
            //     TL.Position.X = temp;
            //     temp = BR.Position.X;
            //     BR.Position.X = BL.Position.X;
            //     BL.Position.X = temp;
            //
            //     temp = TR.Position.Y;
            //     TR.Position.Y = BR.Position.Y;
            //     BR.Position.Y = temp;
            //     temp = TL.Position.Y;
            //     TL.Position.Y = BL.Position.Y;
            //     BL.Position.Y = temp;
            //
            //     item.vertexTR = TR;
            //     item.vertexTL = TL;
            //     item.vertexBR = BR;
            //     item.vertexBL = BL;
            //
            //     try
            //     {
            //         // if our tex is in the cache, return
            //         if (ModEntry.TexDataCache.ContainsKey(item.Texture.Name))
            //         {
            //             return;
            //         }
            //         var texData = item.Texture;
            //         // mirror the pixels in the texture, too
            //         var data = new Color[texData.Width * texData.Height];
            //     
            //         texData.GetData(data);
            //         var newData = new Color[data.Length];
            //         for (int y = 0; y < texData.Height; y++)
            //         {
            //             for (int x = 0; x < texData.Width; x++)
            //             {
            //                 newData[y * texData.Width + x] = data[y * texData.Width + (texData.Width - x - 1)];
            //             }
            //         }
            //     
            //         texData.SetData(newData);
            //         item.Texture = texData;
            //         // add the texData.Name to our TexDataCache with its hash code
            //         ModEntry.TexDataCache.TryAdd(texData.Name, texData.GetHashCode());
            //     }
            //     catch (Exception e)
            //     {
            //         // Log.Debug(e.Message);
            //     }
            // }

            // if (effect is not SpriteEffect efx) return;
            // Log.Debug("Waa");
            // var spriteEffect = new SpriteEffect(efx);
            // var matrix = Matrix.Invert(spriteEffect.TransformMatrix.GetValueOrDefault());
            // spriteEffect.TransformMatrix = matrix;
            // effect = spriteEffect;
        }
    }
}