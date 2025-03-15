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
            Log.Error($"Query '{string.Join(" ", query)}' failed:" + error);
            return false;
        }
        NPC who = Game1.getCharacterFromName(name);
        if (who == null) {
            Log.Error($"Query '{string.Join(" ", query)}' requested nonexistent NPC '{name}'");
            return false;
        }
        if (who.currentLocation == location && who.doingEndOfRouteAnimation.Value &&
                who.endOfRouteBehaviorName.Value.EqualsIgnoreCase(anim)) {
            return true;
        }
        return false;
    }
}
