using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Mono.Reflection;
using MonoGame.OpenGL;
using MonoMod.Utils;

namespace MirrorMode.Helpers;

public static class Utils
{
    public static bool TryGetCallingMethod(StackFrame frame, [NotNullWhen(true)] out string? type, [NotNullWhen(true)] out string? method)
    {
        method = null;
        type = null;
        if (!frame.HasMethod()) return false;

        var caller = frame.GetMethod();
        if (caller is null) return false;
        method = caller.Name;

        type = caller.DeclaringType?.Name;

        var qualifiedName = type is null ? $"{caller.Name}" : $"{type}.{caller.Name}";

        if (qualifiedName.Contains("_PatchedBy"))
        {
            fix:
            // first we remove everything after and including _PatchedBy
            qualifiedName = qualifiedName[..qualifiedName.IndexOf("_PatchedBy", StringComparison.Ordinal)];
            // then we remove everything before and including the second to last .
            qualifiedName = qualifiedName[(qualifiedName.LastIndexOf('.', qualifiedName.LastIndexOf('.') - 1) + 1)..];
            var split = qualifiedName.Split('.');
            type = split[0];
            method = split[1];
        }

        Log.Warn(type);

        if (type is null)
        {
            type = method[..method.IndexOf('.')];
        }

        Type[] paras = caller.GetParameters().Select(p => p.ParameterType).ToArray();
        var aType = AccessTools.TypeByName(type);
        if (aType.GetMethod(method, paras) is null && paras.Length > 0) paras = paras[1..];
        Log.Alert($"Type: {aType?.FullName} | Method: {method} | Paras: {paras.Join(null, ", ")}");
        var name = method;
        var meth = AccessTools.Method(aType, name, paras);
        var body = meth.GetMethodBody();
        if (body is not null)
        {
            var inst = meth.GetInstructions();
            for (int i = 0; i < inst.Count; i++)
            {
                var ins = inst[i];
                if (ins.OpCode == OpCodes.Newobj && ins.Operand is ConstructorInfo ctor && ctor.DeclaringType == typeof(Vector2))
                {
                    // check if the previous two instructions were Ldc.R4
                    if (i < 2) continue;
                    if (inst[i - 1].OpCode == OpCodes.Ldc_R4 && inst[i - 2].OpCode == OpCodes.Ldc_R4)
                    {
                        Log.Alert($"Found Vector2: {ins.Previous?.Operand} | {ins.Previous?.Previous?.Operand}");
                    }
                }
            }
        }

        return true;
    }
}