using Microsoft.Xna.Framework.Audio;
using StardewValley;
using System.Reflection;

namespace ichortower.PositionalAudio;

/*
 * These extension methods allow me to ask a Cue what category it belongs to,
 * a task which requires a worrying amount of reflection.
 */
internal static class CueExtensions
{
    internal static FieldInfo cueField = null;
    internal static FieldInfo cueDefField = null;
    internal static AudioCategory[] cachedCategories = null;

    public static int GetCategoryIndex(this ICue cue)
    {
        cueField ??= typeof(CueWrapper).GetField("cue",
                BindingFlags.NonPublic | BindingFlags.Instance);
        cueDefField ??= typeof(Cue).GetField("_cueDefinition",
                BindingFlags.NonPublic | BindingFlags.Instance);
        Cue temp = (Cue)cueField.GetValue(cue);
        CueDefinition def = (CueDefinition)cueDefField.GetValue(temp);
        return (int)def.sounds[0].categoryID;
    }

    public static string GetCategoryName(this ICue cue)
    {
        int idx = GetCategoryIndex(cue);
        if (cachedCategories is null) {
            PropertyInfo cats = typeof(AudioEngine).GetProperty("Categories",
                BindingFlags.NonPublic | BindingFlags.Instance);
            cachedCategories = (AudioCategory[])cats.GetValue(Game1.audioEngine.Engine);
        }
        return cachedCategories[idx].Name;
    }
}
