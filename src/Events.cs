using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace ichortower.PositionalAudio;

internal sealed class Events
{
    public static void OnAssetRequested(object sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(AudioPlayer.AssetName)) {
            e.LoadFrom(() => new Dictionary<string, AudioItem>(), AssetLoadPriority.Exclusive);
        }
    }

    public static void OnAssetReady(object sender, AssetReadyEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges")) {
            AudioPlayer.ReplaceCues();
        }
    }

    public static void OnAssetsInvalidated(object sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var name in e.Names) {
            if (name.IsEquivalentTo(AudioPlayer.AssetName)) {
                Log.Trace("Invalidating cache");
                AudioPlayer.Invalidate();
                AudioPlayer.Filter(Game1.player.currentLocation);
            }
        }
    }

    private static string semaphore = $"{Main.ModId}_NoAnimation";
    private static Dictionary<NPC, NPCState> NPCCache = new();
    private const int framesPerScan = 20;
    private static int scanTimer = framesPerScan;
    private static int gameTime = 600;

    /*
     * Mainly, this calls AudioPlayer.TryPlaying. Every <framesPerScan> frames,
     * it also checks all NPCs and determines if any of them have changed
     * motion or animation state. If they have, it calls the filter function,
     * so data items can become (in)active.
     */
    public static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (Game1.player.currentLocation == null) {
            return;
        }
        if (--scanTimer == 0) {
            if (NeedsRefresh()) {
                AudioPlayer.Filter(Game1.player.currentLocation);
            }
            scanTimer = framesPerScan;
        }
        AudioPlayer.TryPlaying();
    }

    /*
     * returns true if any NPC has changed state (started or stopped moving,
     * or changed animations), or if clock time has changed.
     *   side effect: populates NPCCache
     */
    internal static bool NeedsRefresh()
    {
        bool ret = false;
        int etime = Game1.timeOfDay;
        if (etime != gameTime) {
            ret = true;
        }
        gameTime = etime;
        Utility.ForEachCharacter((npc) => {
            string anim = semaphore;
            if (npc.doingEndOfRouteAnimation.Value &&
                    !string.IsNullOrEmpty(npc.endOfRouteBehaviorName.Value)) {
                anim = npc.endOfRouteBehaviorName.Value;
            }
            NPCState s = new NPCState {
                Moving = (npc.controller?.pathToEndPoint?.Count ?? 0) > 0,
                Animation = anim,
            };
            // return true from outer func if an NPC has changed state
            if (NPCCache.TryGetValue(npc, out NPCState ex) && ex != s) {
                ret = true;
            }
            NPCCache[npc] = s;
            // this is for the ForEachCharacter delegate, not this func.
            // always return true to keep iterating (ensure cache is correct)
            return true;
        });
        return ret;
    }

    public static void OnPlayerWarped(object sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer) {
            return;
        }
        AudioPlayer.Stop();
        NPCCache.Clear();
        AudioPlayer.Filter(e.NewLocation);
    }

    public static void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        AudioPlayer.Stop();
        AudioPlayer.Filter(Game1.player.currentLocation);
    }
}

/*
 * Holds an NPC's moving state and animation string. Used for caching to
 * reduce frequency of calling AudioPlayer.Filter.
 */
internal record struct NPCState
{
    public bool Moving;
    public string Animation;
}
