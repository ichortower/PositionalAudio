using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.Triggers;
using System.Collections.Generic;

namespace ichortower.PositionalAudio;

internal sealed class Hooks
{
    public static void Register()
    {
        TriggerActionManager.RegisterAction($"{Main.ModId}_Refresh", Refresh);
        GameStateQuery.Register($"{Main.ModId}_NPC_ANIMATING", NPC_ANIMATING);
        GameStateQuery.Register($"{Main.ModId}_NPC_POSITION", NPC_POSITION);
        GameStateQuery.Register($"{Main.ModId}_NPC_POSITION_RECT", NPC_POSITION_RECT);
    }

    public static bool Refresh(string[] args,
            TriggerActionContext context,
            out string error)
    {
        AudioPlayer.Filter(Game1.player.currentLocation);
        error = null;
        return true;
    }

    public static bool NPC_ANIMATING(string[] query, GameStateQueryContext context)
    {
        string error;
        GameLocation location = context.Location;
        if (!ArgUtility.TryGet(query, 1, out string name, out error) ||
                !GameStateQuery.Helpers.TryGetLocationArg(query, 2, ref location, out error) ||
                !ArgUtility.TryGet(query, 3, out string anim, out error)) {
            Log.Error($"Query '{string.Join(" ", query)}' failed parsing: " + error);
            return false;
        }
        NPC who = Game1.getCharacterFromName(name);
        if (who is null) {
            Log.Error($"Query '{string.Join(" ", query)}' requested nonexistent NPC '{name}'");
            return false;
        }
        if (who.currentLocation == location && who.doingEndOfRouteAnimation.Value &&
                who.endOfRouteBehaviorName.Value.EqualsIgnoreCase(anim)) {
            return true;
        }
        return false;
    }

    public static bool NPC_POSITION(string[] query, GameStateQueryContext context)
    {
        string error;
        GameLocation location = context.Location;
        if (!ArgUtility.TryGet(query, 1, out string name, out error) ||
                !GameStateQuery.Helpers.TryGetLocationArg(query, 2, ref location, out error)) {
            Log.Error($"Query '{string.Join(" ", query)}' failed parsing: " + error);
            return false;
        }
        if (query.Length <= 3) {
            Log.Error($"Query '{string.Join(" ", query)}' failed parsing: at least one coordinate pair is required");
            return false;
        }
        List<Point> candidates = new();
        for (int i = 3; i < query.Length; i += 2) {
            if (!ArgUtility.TryGetPoint(query, i, out Point pos, out error)) {
                Log.Error($"Query '{string.Join(" ", query)}' failed parsing: " + error);
                return false;
            }
            candidates.Add(pos);
        }
        NPC who = Game1.getCharacterFromName(name);
        if (who is null) {
            Log.Error($"Query '{string.Join(" ", query)}' requested nonexistent NPC '{name}'");
            return false;
        }
        if (who.currentLocation != location) {
            return false;
        }
        foreach (Point p in candidates) {
            if (who.TilePoint.Equals(p)) {
                return true;
            }
        }
        return false;
    }

    public static bool NPC_POSITION_RECT(string[] query, GameStateQueryContext context)
    {
        string error;
        GameLocation location = context.Location;
        if (!ArgUtility.TryGet(query, 1, out string name, out error) ||
                !GameStateQuery.Helpers.TryGetLocationArg(query, 2, ref location, out error) ||
                !ArgUtility.TryGetRectangle(query, 3, out Rectangle rect, out error)) {
            Log.Error($"Query '{string.Join(" ", query)}' failed parsing: " + error);
            return false;
        }
        NPC who = Game1.getCharacterFromName(name);
        if (who is null) {
            Log.Error($"Query '{string.Join(" ", query)}' requested nonexistent NPC '{name}'");
            return false;
        }
        if (who.currentLocation == location && rect.Contains(who.TilePoint)) {
            return true;
        }
        return false;
    }
}
