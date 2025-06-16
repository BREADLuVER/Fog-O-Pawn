using Verse;

namespace FogOfPawn
{
    /// <summary>
    /// Central lightweight accessor for <see cref="FogOfPawnSettings"/> so that runtime
    /// code (Harmony patches etc.) can read the latest values without needing direct
    /// references to the <see cref="Mod"/> instance. The reference is resolved lazily and
    /// refreshed on every access to support hot-apply from the settings window.
    /// </summary>
    public static class FogSettingsCache
    {
        public static FogOfPawnSettings Current
        {
            get
            {
                // First, try the static field initialised by the Mod subclass (fast path).
                if (FogOfPawnMod.Settings != null)
                    return FogOfPawnMod.Settings;

                // Fallback – defensive lookup via the mod manager (the settings window may
                // not have been opened yet during early loading stages).
                var mod = LoadedModManager.GetMod<FogOfPawnMod>();
                return mod?.GetSettings<FogOfPawnSettings>() ?? new FogOfPawnSettings();
            }
        }
    }
} 