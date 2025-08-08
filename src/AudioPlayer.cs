using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.Extensions;
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

    /*
     * Given the data from the data asset and a location, determine which audio
     * items should be active (for handling on frame update) and/or refresh
     * them if possible.
     */
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

    /*
     * This function runs during UpdateTicked.
     * Computes playback volume for every active audio item (and, at the same
     * time, the appropriate adjusted bgm volume). Runs volume faders and
     * sfx timers.
     */
    public static void TryPlaying(bool force = false)
    {
        Vector2 pos = Game1.player.Position;
        // compute target volumes for everything only if needed (or forced)
        if (force || (pos != CachedPlayerPosition)) {
            CachedPlayerPosition = pos;
            BgmGoalVolume = 1f;
            foreach (AudioItem sound in ActiveItems.Values) {
                sound.ComputeVolume(Game1.currentGameTime, ref BgmGoalVolume);
            }
        }
        // but always set the volume as appropriate (these handle the fade behavior)
        // note that items which became inactive live elsewhere just to fade out
        foreach (AudioItem sound in ActiveItems.Values) {
            sound.StepVolume(Game1.currentGameTime);
            if (sound.Cue.GetCategoryName() == "Sound") {
                sound.TickDelay(Game1.currentGameTime);
            }
        }
        StepBgmVolume(Game1.currentGameTime);
        FadeDoomedItems(Game1.currentGameTime);
    }

    /*
     * BGM volume fader. Moves one step toward the target volume per frame.
     */
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

    /*
     * Audio fade-out handler specifically for items which became inactive,
     * so that they can fade out gracefully before being trashed.
     */
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

    /*
     * Called whenever the data asset is ready. This replaces any existing
     * active cue with a new one sourced from the sound bank. We can't restore
     * the play position, but we can preserve the volume (by recomputing it).
     *
     * TODO see if we can detect whether a cue's audio stream has not changed,
     *    and if so avoid replacing it entirely so playback doesn't restart
     */
    public static void ReplaceCues()
    {
        float unused = 1f;
        foreach (AudioItem sound in ActiveItems.Values) {
            if (Game1.soundBank.Exists(sound.CueName)) {
                sound.Stop();
                sound.Cue.Dispose();
                sound.Cue = Game1.soundBank.GetCue(sound.CueName);
                // zeroing cached position causes recalc on the next tick, but
                // that leaves this tick with the default volume, which is wrong
                sound.ComputeVolume(Game1.currentGameTime, ref unused);
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
    public List<int> RepeatDelay = new() {800, 1000, 1200};
    public Point TilePosition = new(-1, -1);

    internal ICue Cue { get; set; }
    internal float TargetVolume = 0.0f;
    internal int DelayTimer = 0;

    // deliberately does not clone the Cue
    public AudioItem Clone()
    {
        AudioItem ret = new() {
            Condition = this.Condition,
            CueName = this.CueName,
            LocationName = this.LocationName,
            MaximumIntensity = this.MaximumIntensity,
            MinimumBgmVolume = this.MinimumBgmVolume,
            Radius = new AudioRadius {
                Floor = this.Radius.Floor,
                Shelf = this.Radius.Shelf,
                Maximum = this.Radius.Maximum,
            },
            RepeatDelay = new List<int>(this.RepeatDelay),
            TilePosition = this.TilePosition,

            TargetVolume = this.TargetVolume,
            DelayTimer = this.DelayTimer,
        };
        return ret;
    }

    /*
     * Calculates how loud this audio should be (0.0 to 1.0) based on the player's
     * proximity to its TilePosition. The ref parameter is a float for how quiet
     * the existing background music should get in response; if our volume requests
     * a lower bgm volume, it will be saved there.
     *
     * Does not set the volume; merely sets the TargetVolume for StepVolume to use.
     */
    public void ComputeVolume(GameTime time, ref float bgmTarget)
    {
        if (Cue == null) {
            return;
        }
        // part of the volume faffing on cue reload
        if (Cue.GetCategoryName() == "Music" && !Cue.IsPlaying) {
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
            bgmTarget = MathF.Min(bgmTarget, cand);
        }
    }

    /*
     * If the audio isn't playing, start (or continue) its delay timer. When it
     * hits zero, play it.
     */
    public void TickDelay(GameTime time)
    {
        if (!ShouldTimePassIgnoreFestival()) {
            return;
        }
        if (Cue.IsPlaying) {
            return;
        }
        if (DelayTimer == 0) {
            DelayTimer = Game1.random.ChooseFrom(RepeatDelay);
            return;
        }
        DelayTimer = Math.Max(0, DelayTimer - time.ElapsedGameTime.Milliseconds);
        if (DelayTimer == 0) {
            Cue.Play();
            // just set straight to target volume. it's already computed for this
            // tick and it's sfx so we don't need to let it fade in
            Cue.Volume = TargetVolume;
        }
    }

    /*
     * Cue-specific fader. Moves one step toward TargetVolume per frame.
     */
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

    /*
     * In order to support having positional audio during festival walk-around
     * time, I can't use Game1.shouldTimePass on its own, since it returns
     * false during festivals entirely. To defeat this, this hackjob sets the
     * current location to null and eventUp to false, calls the function, then
     * restores them.
     */
    internal static bool ShouldTimePassIgnoreFestival()
    {
        GameLocation temp = Game1.currentLocation;
        bool euTemp = Game1.eventUp;

        Game1.currentLocation = null;
        Game1.eventUp = false;
        bool ret = Game1.shouldTimePass();
        Game1.currentLocation = temp;
        Game1.eventUp = euTemp;

        return ret;
    }
}
