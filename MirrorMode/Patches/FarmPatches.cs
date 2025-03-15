using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using StardewValley;

namespace MirrorMode.Patches;

[HarmonyPatch]
[HarmonyPatch(typeof(Farm))]
public static class Vector2Patches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Farm.GetGreenhouseStartLocation))]
    static IEnumerable<CodeInstruction> GetGreenhouseStartLocation_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)]))
            ).Repeat(codeMatcher =>
                codeMatcher.Advance(1).InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(MirroringExtensions), nameof(MirroringExtensions.TileWidth))),
                    new CodeInstruction(OpCodes.Call,
                        AccessTools.FirstMethod(typeof(MirroringExtensions),
                            method => method.Name == nameof(MirroringExtensions.Mirror) &&
                                      method.ReturnType == typeof(Vector2))),
                    new CodeInstruction(OpCodes.Ldc_R4, 6f),
                    new CodeInstruction(OpCodes.Ldc_R4, 0f),
                    new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)])),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), "op_Subtraction"))
                ));

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch Farm.GetGreenhouseStartLocation: {ex}");
            return code;
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Farm.GetStarterPetBowlLocation))]
    static IEnumerable<CodeInstruction> GetStarterPetBowlLocation_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)]))
            ).Advance(1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(MirroringExtensions), nameof(MirroringExtensions.TileWidth))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.FirstMethod(typeof(MirroringExtensions),
                        method => method.Name == nameof(MirroringExtensions.Mirror) &&
                                  method.ReturnType == typeof(Vector2))),
                new CodeInstruction(OpCodes.Ldc_R4, 1f),
                new CodeInstruction(OpCodes.Ldc_R4, 0f),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)])),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), "op_Subtraction"))
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch Farm.GetStarterPetBowlLocation: {ex}");
            return code;
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Farm.GetSpouseOutdoorAreaCorner))]
    static IEnumerable<CodeInstruction> GetSpouseOutdoorAreaCorner_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Call, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)]))
            ).Advance(1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(MirroringExtensions), nameof(MirroringExtensions.TileWidth))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.FirstMethod(typeof(MirroringExtensions),
                        method => method.Name == nameof(MirroringExtensions.Mirror) &&
                                  method.ReturnType == typeof(Vector2))),
                new CodeInstruction(OpCodes.Ldc_R4, 3f),
                new CodeInstruction(OpCodes.Ldc_R4, 0f),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)])),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), "op_Subtraction")),
                new CodeInstruction(OpCodes.Stloc_0)
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch Farm.GetSpouseOutdoorAreaCorner: {ex}");
            return code;
        }
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Farm.GetStarterShippingBinLocation))]
    static IEnumerable<CodeInstruction> GetStarterShippingBinLocation_Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator il)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Call, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)]))
            ).Advance(1);

            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(MirroringExtensions), nameof(MirroringExtensions.TileWidth))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.FirstMethod(typeof(MirroringExtensions),
                        method => method.Name == nameof(MirroringExtensions.Mirror) &&
                                  method.ReturnType == typeof(Vector2))),
                new CodeInstruction(OpCodes.Ldc_R4, 1f),
                new CodeInstruction(OpCodes.Ldc_R4, 0f),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(Vector2), [typeof(float), typeof(float)])),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector2), "op_Subtraction")),
                new CodeInstruction(OpCodes.Stloc_0)
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch Farm.GetStarterShippingBinLocation: {ex}");
            return code;
        }
    }
}

[HarmonyPatch]
[HarmonyPatch(typeof(Farm))]
public static class PointPatches
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Farm), nameof(Farm.GetMainFarmHouseEntry));
        yield return AccessTools.Method(typeof(Farm), nameof(Farm.GetMainMailboxPosition));
        yield return AccessTools.Method(typeof(Farm), nameof(Farm.GetGrandpaShrinePosition));
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator il, MethodBase original)
    {
        var code = instructions.ToList();
        try
        {
            var matcher = new CodeMatcher(code, il);

            matcher.MatchEndForward(
                new CodeMatch(OpCodes.Call, AccessTools.Constructor(typeof(Point), [typeof(int), typeof(int)]))
            ).Advance(1);

            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt,
                    AccessTools.PropertyGetter(typeof(GameLocation), nameof(GameLocation.Map))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(MirroringExtensions), nameof(MirroringExtensions.TileWidth))),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.FirstMethod(typeof(MirroringExtensions),
                        method => method.Name == nameof(MirroringExtensions.Mirror) &&
                                  method.ReturnType == typeof(Point)))
            );
            
            if (original.Name.Equals(nameof(Farm.GetGrandpaShrinePosition)))
            {
                matcher.InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldc_I4_3),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Newobj,
                        AccessTools.Constructor(typeof(Point), [typeof(int), typeof(int)])),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Point), "op_Subtraction"))
                );
            }
            
            matcher.Insert(
                new CodeInstruction(OpCodes.Stloc_0)
            );

            return matcher.InstructionEnumeration();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to patch Farm PointPatches: {ex}");
            return code;
        }
    }
}