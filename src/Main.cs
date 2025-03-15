using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ichortower.PositionalAudio;

internal sealed class Main : Mod
{
    public static Main instance;
    public static string ModId;

    public override void Entry(IModHelper helper)
    {
        instance = this;
        ModId = instance.ModManifest.UniqueID;

        helper.Events.Content.AssetRequested += Events.OnAssetRequested;
        helper.Events.Content.AssetReady += Events.OnAssetReady;
        helper.Events.Content.AssetsInvalidated += Events.OnAssetsInvalidated;
        helper.Events.GameLoop.UpdateTicked += Events.OnUpdateTicked;
        helper.Events.GameLoop.DayStarted += Events.OnDayStarted;
        helper.Events.Player.Warped += Events.OnPlayerWarped;

        Hooks.Register();
    }
}

internal sealed class Log
{
    public static void Trace(string text) {
        Main.instance.Monitor.Log(text, LogLevel.Trace);
    }
    public static void Debug(string text) {
        Main.instance.Monitor.Log(text, LogLevel.Debug);
    }
    public static void Info(string text) {
        Main.instance.Monitor.Log(text, LogLevel.Info);
    }
    public static void Warn(string text) {
        Main.instance.Monitor.Log(text, LogLevel.Warn);
    }
    public static void Error(string text) {
        Main.instance.Monitor.Log(text, LogLevel.Error);
    }
    public static void Alert(string text) {
        Main.instance.Monitor.Log(text, LogLevel.Alert);
    }
    public static void Verbose(string text) {
        Main.instance.Monitor.VerboseLog(text);
    }
}

internal class TR
{
    public static string Get(string key) {
        return Main.instance.Helper.Translation.Get(key);
    }
}
