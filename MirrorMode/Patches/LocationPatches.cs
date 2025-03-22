using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace MirrorMode.Patches;

[HarmonyPatch]
public class LocationPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.getWarpPointTarget))]
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
            string[] action = __instance.GetTilePropertySplitBySpaces("Action", "Buildings", warpPointLocation.X, warpPointLocation.Y);
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
                    Game1.log.Warn($"Door in {__instance.NameOrUniqueName} ({v.Key}) has unknown warp property '{string.Join(" ", action)}', parsing with legacy logic.");
                    break;
            }
            if (!ArgUtility.TryGetPoint(action, 1, out var tile, out var error, "Point tile") || !ArgUtility.TryGet(action, 3, out var locationName, out error, allowBlank: true, "string locationName"))
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
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.getWarpFromDoor))]
    static bool getWarpFromDoor_Prefix(GameLocation __instance, ref Warp __result, Point door, Character character = null)
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