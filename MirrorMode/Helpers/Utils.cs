using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

        if (type is null)
        {
            type = method[..method.IndexOf('.')];
        }


        return true;
    }
}