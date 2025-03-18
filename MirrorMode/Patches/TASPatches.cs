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
                "Farmer.showSwordSwipe",
                "Farmer.showToolSwipeEffect",
                "Farmer.showHoldingItem",
                "Farmer.showNutPickup",
                "Farmer.showEatingItem",
                "Farmer.OnItemReceived",
                "Farmer.moveRaft",
                "Farmer.drinkGlug",
                "Farmer.showItemIntake",
                "Farmer.toolPowerIncrease",
                "Farmer.takeDamage",
                "Farmer.doneEating",
                "FarmerSprite.checkForFootstep",
                "Game1.showSwordswipeAnimation",
                "Game1.pressUseToolButton",
                "Game1.performTouchAction",
                "Farm.showShipment",
                "Farm.doLightningStrike",
                "GameLocation.performTouchAction",
                "GameLocation.UpdateWhenCurrentLocation",
                "GameLocation.playTerrainSound",
                "GameLocation.CheckGarbage",
                "GameLocation.explode",
                "Fence.performToolAction",
                "Object.ApplySprinklerAnimation",
                "Object.playCustomMachineLoadEffects",
                "Object.CheckForActionOnMiniObelisk",
                "Object.CheckForActionOnBlessedStatue",
                "Utility.spawnObjectAround",
                "Utility.makeTemporarySpriteJucier",
                "FishingRod.DoFunction",
                "FishingRod.doPullFishFromWater",
                "FishingRod.draw",
                "FishingRod.tickUpdate",
                "FishingRod.doneHoldingFish",
                "FishingRod.doStartCasting",
                "FishingRod.openChestEndFunction",
                "Tent.onDestroy",
                "Tree.performToolAction",
                "BasicProjectile.explosionAnimation",
                "DebuffingProjectile.update",
                "Chest.dumpContents",
                "CrabPot.draw",
                "Furniture.addCauldronBubbles",
                "Mannequin.emitGhost",
                "MiniJukebox.updateWhenCurrentLocation",
                "WoodChipper.addWorkingAnimation",
                "WoodChipper.performObjectDropInAction",
                "AngryRoger.localDeathAnimation",
                "Bat.takeDamage",
                "Bat.updateAnimation",
                "BigSlime.localDeathAnimation",
                "DustSpirit.localDeathAnimation",
                "DwarvishSentry.localDeathAnimation",
                "Ghost.localDeathAnimation",
                "Ghost.updateAnimation",
                "MetalHead.localDeathAnimation",
                "RockGolem.localDeathAnimation",
                "Serpent.localDeathAnimation",
                "ShadowBrute.localDeathAnimation",
                "ShadowGirl.localDeathAnimation",
                "ShadowGuy.localDeathAnimation",
                "ShadowShaman.localDeathAnimation",
                "Shooter.localDeathAnimation",
                "SquidKid.localDeathAnimation",
                "ForgeMenu.receiveLeftClick",
                "ForgeMenu.update",
                "GeodeMenu.update",
                "Bundle.shake",
                "Bundle.ingredientDepositAnimation",
                "LevelUpMenu.update",
                "MasteryTrackerMenu.addSpiritCandles",
                "MasteryTrackerMenu.addCandle",
                "MasteryTrackerMenu.addSkillFlairPlaque",
                "RenovateMenu.AnimateRenovation",
                "ShippingMenu..ctor",
                "ShippingMenu.update",
                "ShopMenu.receiveLeftClick",
                "ShopMenu.receiveRightClick",
                "Beach.performTenMinuteUpdate",
                "BoatTunnel.UpdateWhenCurrentLocation",
                "BusStop.resetLocalState",
                "Caldera.performToolAction",
                "Desert.UpdateWhenCurrentLocation",
                "FarmHouse.resetLocalState",
                "SocializeQuest.OnNpcSocialized",
                "Buff.OnAdded"
                // I honestly can't be fucked to add any more. I left off at IslandEast for Add.
            };
        }
    }

    // TODO: Consider postfixing broadcastSprites to add the calling member into its ID

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Multiplayer), nameof(Multiplayer.broadcastSprites), [typeof(GameLocation), typeof(TemporaryAnimatedSpriteList)])]
    static void Prefix_broadcastSprites(TemporaryAnimatedSpriteList sprites)
    {
        var method = new StackFrame(2).GetMethod();
        var type = method?.DeclaringType?.Name;
        var fullName = $"{type}.{method?.Name}";

        foreach (var sprite in sprites)
        {
            sprite.textureName = "Spiderbuttons.MirrorMode_(" + fullName + ")_" + sprite.id;
        }
    }
    
    // [HarmonyTranspiler]
    // [HarmonyPatch(typeof(Multiplayer), nameof(Multiplayer.broadcastSprites), [typeof(GameLocation), typeof(TemporaryAnimatedSprite[])])]
    // static IEnumerable<CodeInstruction> Transpiler_broadcastSprites(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    // {
    //     var code = instructions.ToList();
    //     try
    //     {
    //         var matcher = new CodeMatcher(code, il);
    //
    //         matcher.MatchEndForward(
    //             new CodeMatch(OpCodes.Callvirt,
    //                 AccessTools.Method(typeof(TemporaryAnimatedSpriteList),
    //                     nameof(TemporaryAnimatedSpriteList.AddRange)))
    //         ).Advance(1);
    //
    //         matcher.Insert(
    //             new CodeInstruction(OpCodes.Ldarg_2),
    //             new CodeInstruction(OpCodes.Call,
    //                 AccessTools.Method(typeof(TASPatches), nameof(FixSpriteTextureNames)))
    //         );
    //
    //         return matcher.InstructionEnumeration();
    //     }
    //     catch (Exception ex)
    //     {
    //         Log.Error("Failed to transpile Multiplayer.broadcastSprites: " + ex);
    //         return code;
    //     }
    // }

    // static void FixSpriteTextureNames(TemporaryAnimatedSprite[] sprites)
    // {
    //     foreach (var sprite in sprites)
    //     {
    //         if (sprite.textureName.StartsWith("Spiderbuttons.MirrorMode_("))
    //         {
    //             sprite.textureName = sprite.textureName.Split(')')[1].TrimStart('_');
    //         }
    //     }
    // }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TemporaryAnimatedSpriteList.AddRange))]
    static void AddRange_Postfix(IEnumerable<TemporaryAnimatedSprite> values)
    {
        if (!Context.IsWorldReady) return;

        MethodBase? method = null;
        string? type = null;
        
        foreach (var item in values)
        {
            string? fullName = null;
            if (item.textureName is not null && item.textureName.Contains("Spiderbuttons.MirrorMode_("))
            {
                // extract the full name we encoded above. the full name is between the parenthesis
                fullName = item.textureName.Split('(')[1].Split(')')[0];
                item.textureName = item.textureName.Split(')')[1].TrimStart('_');
            }
            else
            {
                method = new StackFrame(1).GetMethod();
                type = method?.DeclaringType?.Name;
                fullName = $"{type}.{method?.Name}";
            }

            if (item.sourceRect.X == 666 && Game1.currentLocation.Name.EqualsIgnoreCase("Sewer"))
            {
                item.Position -= new Vector2(32, 0);
                goto mirror;
            }

            if (fullName.Contains("GameLocation.CheckGarbage"))
            {
                item.flipped = !item.flipped;
                continue;
            }
            
            if (method is null || invalidFunctions.Any(inv => inv.Contains($"{fullName}"))) continue;
            
            mirror:
            ModEntry.ModMonitor.LogOnce("TAS Added via '" + type + "." + method!.Name + "'", LogLevel.Error);
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
        if (!Context.IsWorldReady) return;
        
        var method = new StackFrame(1).GetMethod();
        var type = method?.DeclaringType?.Name;
        var fullName = $"{type}.{method?.Name}";

        if (item.textureName.EqualsIgnoreCase("Characters\\\\asldkfjsquaskutanfsldk")) goto mirror;
        
        if (method is null || invalidFunctions.Any(inv => inv.Contains($"{fullName}"))) return;
        mirror:
        ModEntry.ModMonitor.LogOnce("TAS Added via '" + type + "." + method!.Name + "'", LogLevel.Error);
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