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
        if (e.NameWithoutLocale.IsEquivalentTo(AudioPlayer.AssetName)) {
            AudioPlayer.LoadData();
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges")) {
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

    private static Dictionary<NPC, string> Animations = new();
    private static string semaphore = $"{Main.ModId}_NoAnimation";

    public static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (Game1.player.currentLocation == null) {
            return;
        }
        bool doFilter = false;
        foreach (NPC p in Game1.player.currentLocation.characters) {
            string a = semaphore;
            if (p.doingEndOfRouteAnimation.Value &&
                    !string.IsNullOrEmpty(p.endOfRouteBehaviorName.Value)) {
                a = p.endOfRouteBehaviorName.Value;
            }
            if (Animations.TryGetValue(p, out string ex) && ex != a) {
                doFilter = true;
            }
            Animations[p] = a;
        }
        if (doFilter) {
            Log.Trace("Animation status changed. Refreshing.");
            AudioPlayer.Filter(Game1.player.currentLocation);
        }
        AudioPlayer.TryPlaying();
    }

    public static void OnPlayerWarped(object sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer) {
            return;
        }
        AudioPlayer.Stop();
        Animations.Clear();
        AudioPlayer.Filter(e.NewLocation);
    }

    public static void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        AudioPlayer.Stop();
        AudioPlayer.Filter(Game1.player.currentLocation);
    }
}
