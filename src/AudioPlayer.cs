using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ichortower.PositionalAudio;

internal sealed class AudioPlayer
{
    public static string AssetName = $"Mods/{Main.ModId}/Data";

    private static Dictionary<string, AudioItem> ActiveItems = new();
    // items that were previously active but have become inactive.
    // they go here and fade out before being disposed.
    private static Dictionary<string, AudioItem> DoomedItems = new();
    private static Vector2 CachedPlayerPosition = Vector2.Zero;
    internal static float FadeStep = 0.015f;
    private static float BgmStartVolume = 0f;
    internal static float BgmGoalVolume = 0f;
    private static HashSet<string> MissingCues = new();

    public static Dictionary<string, AudioItem> Data {
        get {
            _Data ??= Game1.content.Load<Dictionary<string, AudioItem>>(AssetName);
            return _Data;
        }
    }
    private static Dictionary<string, AudioItem> _Data = null;

    public static void ClearActive(bool maintainPlaying = false)
    {
        Dictionary<string, AudioItem> saved = new();
        foreach (var pair in ActiveItems) {
            if (maintainPlaying && pair.Value.IsPlaying()) {
                saved[pair.Key] = pair.Value;
            }
            else {
                pair.Value.Dispose();
            }
        }
        ActiveItems = saved;
    }

    public static void Invalidate()
    {
        _Data = null;
        ClearActive(maintainPlaying: true);
        MissingCues.Clear();
    }

    public static void Filter(GameLocation gl = null)
    {
        if (gl == null) {
            ClearActive();
            return;
        }
        string n = gl.NameOrUniqueName;
        foreach (var pair in Data) {
            if (!pair.Value.LocationName.Equals(n) ||
                    !GameStateQuery.CheckConditions(pair.Value.Condition)) {
                if (ActiveItems.Remove(pair.Key, out AudioItem ex)) {
                    DoomedItems[pair.Key] = ex;
                }
                continue;
            }
            string cue = pair.Value.CueName;
            // skip if known missing or found missing
            if (MissingCues.Contains(cue)) {
                continue;
            }
            if (!Game1.soundBank.Exists(cue)) {
                Log.Warn($"Skipping audio item '{pair.Key}': cue name '{cue}' not found.");
                MissingCues.Add(cue);
                continue;
            }

            // maintain existing cue if already valid and not changed
            if (ActiveItems.ContainsKey(pair.Key) &&
                    ActiveItems[pair.Key].CueName.Equals(cue) &&
                    ActiveItems[pair.Key].Cue != null) {
                AudioItem cpy = pair.Value.Clone();
                cpy.Cue = ActiveItems[pair.Key].Cue;
                ActiveItems[pair.Key].Cue = null;
                ActiveItems[pair.Key] = cpy;
            }
            else {
                // if CueName did change, must remove/dispose old entry
                if (ActiveItems.Remove(pair.Key, out AudioItem ex)) {
                    ex.Dispose();
                }
                ActiveItems[pair.Key] = pair.Value.Clone();
                ActiveItems[pair.Key].Cue = Game1.soundBank.GetCue(cue);
            }
        }
        if (Game1.currentSong != null) {
            BgmStartVolume = Game1.currentSong.Volume;
        }
        // effectively setting force to true for the next TryPlaying
        CachedPlayerPosition = Vector2.Zero;
    }

    public static void TryPlaying(bool force = false)
    {
        Vector2 pos = Game1.player.Position;
        // compute target volumes for everything only if needed (or forced)
        if (force || (pos != CachedPlayerPosition)) {
            CachedPlayerPosition = pos;
            BgmGoalVolume = 1f;
            foreach (AudioItem sound in ActiveItems.Values) {
                sound.Tick(Game1.currentGameTime);
            }
        }
        // but always set the volume as appropriate (these handle the fade behavior)
        // note that items which became inactive live elsewhere just to fade out
        foreach (AudioItem sound in ActiveItems.Values) {
            sound.StepVolume(Game1.currentGameTime);
        }
        StepBgmVolume(Game1.currentGameTime);
        FadeDoomedItems(Game1.currentGameTime);
    }

    public static void StepBgmVolume(GameTime gt)
    {
        if (Game1.currentSong is null) {
            return;
        }
        float vol = Game1.currentSong.Volume;
        if (vol < BgmGoalVolume) {
            vol = MathF.Min(vol + FadeStep, BgmGoalVolume);
        }
        else {
            vol = MathF.Max(vol - FadeStep, BgmGoalVolume);
        }
        Game1.currentSong.Volume = vol;
    }

    public static void FadeDoomedItems(GameTime gt)
    {
        foreach (var pair in DoomedItems) {
            pair.Value.Cue.Volume -= FadeStep;
            if (pair.Value.Cue.Volume <= 0f) {
                pair.Value.Dispose();
                DoomedItems.Remove(pair.Key);
            }
        }
    }

    // grr AudioChanges invalidating resets volume
    public static void ReplaceCues()
    {
        foreach (AudioItem sound in ActiveItems.Values) {
            if (Game1.soundBank.Exists(sound.CueName)) {
                sound.Stop();
                sound.Cue.Dispose();
                sound.Cue = Game1.soundBank.GetCue(sound.CueName);
                sound.Tick(Game1.currentGameTime);
                CachedPlayerPosition = Vector2.Zero;
            }
        }
    }

    public static void Stop()
    {
        foreach (AudioItem sound in Data.Values) {
            sound.Stop();
        }
    }
}

internal sealed class AudioRadius
{
    private float _floor = 2f;
    public float Floor {
        get {
            return _floor;
        }
        set {
            _floor = MathF.Max(0f, value);
        }
    }

    private float _shelf = 4f;
    public float Shelf {
        get {
            return _shelf;
        }
        set {
            _shelf = MathF.Max(0f, value);
        }
    }

    private float _maximum = 8f;
    public float Maximum {
        get {
            return _maximum;
        }
        set {
            _maximum = MathF.Max(0f, value);
        }
    }
}

internal sealed class AudioItem
{
    public string Condition = "";
    public string CueName = "";
    public string LocationName = "";
    public float MaximumIntensity = 1.0f;
    public float MinimumBgmVolume = 0.0f;
    public AudioRadius Radius = new();
    //public List<int> RepeatDelay = new();
    public Point TilePosition = new(-1, -1);

    internal ICue Cue { get; set; }
    internal float TargetVolume = 0.0f;

    // deliberately does not clone the Cue
    public AudioItem Clone()
    {
        AudioItem ret = new() {
            Condition = this.Condition,
            CueName = this.CueName,
            LocationName = this.LocationName,
            MaximumIntensity = this.MaximumIntensity,
            Radius = new AudioRadius {
                Floor = this.Radius.Floor,
                Shelf = this.Radius.Shelf,
                Maximum = this.Radius.Maximum,
            },
            TilePosition = this.TilePosition,
        };
        return ret;
    }

    public void Tick(GameTime time)
    {
        if (Cue == null) {
            return;
        }
        // part of the volume faffing on cue reload
        // FIXME not great for allowing SFX down the road
        if (!Cue.IsPlaying) {
            Cue.Play();
            Cue.Volume = 0f;
        }
        Vector2 playerPos = Game1.player.getStandingPosition() / 64f;
        Vector2 myPos = new(TilePosition.X + 0.5f, TilePosition.Y + 0.5f);
        float dist = Vector2.Distance(myPos, playerPos);
        // clamp radius and max/min values for sanity
        float maxi = MathF.Max(0f, MathF.Min(MaximumIntensity, 1f));
        float mini = MathF.Max(0f, MathF.Min(MinimumBgmVolume, 1f));
        float floor = MathF.Min(Radius.Floor, Radius.Maximum);
        float shelf = MathF.Min(Radius.Shelf, Radius.Maximum);

        Func<float, float, float, float> Curve = (min, max, t) => {
            if (t <= min) {
                return 0f;
            }
            if (t >= max) {
                return 1f;
            }
            return MathF.Sqrt((t - min) / (max - min));
        };

        TargetVolume = maxi * (1f - Curve(floor, Radius.Maximum, dist));
        if (Game1.currentSong != null) {
            // curve is 0f to 1f, so scale it down and add the minimum
            float cand = Curve(shelf, Radius.Maximum, dist) * (1f - mini) + mini;
            AudioPlayer.BgmGoalVolume = MathF.Min(AudioPlayer.BgmGoalVolume, cand);
        }
    }

    public void StepVolume(GameTime time)
    {
        if (Cue.Volume == TargetVolume) {
            return;
        }
        else if (Cue.Volume < TargetVolume) {
            Cue.Volume = MathF.Min(Cue.Volume + AudioPlayer.FadeStep, TargetVolume);
        }
        else {
            Cue.Volume = MathF.Max(Cue.Volume - AudioPlayer.FadeStep, TargetVolume);
        }
    }

    public void Stop()
    {
        Cue?.Stop(AudioStopOptions.Immediate);
    }

    public bool IsPlaying()
    {
        return Cue.IsPlaying;
    }

    public void Dispose()
    {
        Stop();
        Cue?.Dispose();
        Cue = null;
    }
}
