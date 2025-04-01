using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MirrorMode.Helpers;
using MonoMod.Utils;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MirrorMode.Patches;

[HarmonyPatch(typeof(TitleMenu))]
public class TitleMenuPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TitleMenu.setUpIcons))]
    static void setUpIcons_Postfix(TitleMenu __instance)
    {
        foreach (var button in __instance.buttons)
        {
            button.setPosition(button.getVector2().MirrorForUI(Game1.viewport.Width, button.bounds.Width));
        }
        
        // __instance.aboutButton.setPosition(__instance.aboutButton.getVector2().MirrorForUI(Game1.viewport.Width, __instance.aboutButton.bounds.Width));
        // __instance.languageButton.setPosition(__instance.languageButton.getVector2().MirrorForUI(Game1.viewport.Width, __instance.languageButton.bounds.Width));
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(TitleMenu.update))]
    static void Update_Postfix(TitleMenu __instance)
    {
        // int zoom = (__instance.ShouldShrinkLogo() ? 2 : TitleMenu.pixelZoom);
        // __instance.eRect = new Rectangle(__instance.width / 2 - 200 * zoom + 251 * zoom, -300 * zoom - (int)(__instance.viewportY / 3f) * zoom + 26 * zoom, 42 * zoom, 68 * zoom);
        
        // __instance.clicksOnE = 0;
        if (__instance.eRect.X <= __instance.width / 2) return;
        __instance.eRect = __instance.eRect.MirrorForUI(__instance.width, __instance.eRect.Width);
        __instance.r_hole_rect = __instance.r_hole_rect.MirrorForUI(__instance.width, __instance.r_hole_rect.Width);
        __instance.r_hole_rect2 = __instance.r_hole_rect2.MirrorForUI(Game1.viewport.Width, __instance.r_hole_rect2.Width);
        __instance.cornerRect = __instance.cornerRect.MirrorForUI(Game1.viewport.Width, __instance.cornerRect.Width);
        __instance.screwRect = __instance.screwRect.MirrorForUI(Game1.viewport.Width, __instance.screwRect.Width);
        foreach (var rect in __instance.leafRects)
        {
            rect.MirrorForUI(Game1.viewport.Width, rect.Width * 2);
        }

        
    }

    [HarmonyPostfix]
    [HarmonyPatch("GenericModConfigMenu.Mod", "SetupTitleMenuButton")]
    static void GMCM_SetupTitleMenuButton_Postfix(IMod __instance)
    {
        // if (gmcmMirror is not null) return;
        //
        // if (Game1.activeClickableMenu is TitleMenu tm)
        // {
        //     var c = tm.allClickableComponents?.Find(comp => comp.myID is 509800);
        //     if (c is not null)
        //     {
        //         c?.bounds.MirrorForUI(Game1.viewport.Width, c.bounds.Width);
        //     }
        // }
        //
        // var modType = __instance.GetType();
        // var configButtonProperty = modType.GetField("ConfigButton", BindingFlags.NonPublic | BindingFlags.Instance);
        // var configButton = configButtonProperty?.GetValue(__instance);
        // var localPositionProperty = configButton?.GetType().BaseType?.GetProperty("LocalPosition");
        // var widthProperty = configButton?.GetType().BaseType?.GetProperty("Width");
        // var localPosition = localPositionProperty?.GetValue(configButton) as Vector2?;
        // var width = widthProperty?.GetValue(configButton) as int?;
        // if (localPosition is not null) localPosition = localPosition.Value.MirrorForUI(Game1.viewport.Width, width ?? 0);
        // localPositionProperty?.SetValue(configButton, gmcmMirror ?? localPosition);
        // if (gmcmMirror is null)
        // {
        //     gmcmMirror = localPosition;
        //     Log.Alert($"GMCM Mirror: {gmcmMirror}");
        // }
    }
}