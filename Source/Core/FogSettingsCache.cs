using Verse;

namespace FogOfPawn
{
    public static class FogSettingsCache
    {
        public static FogOfPawnSettings Current
        {
            get
            {
                if (FogOfPawnMod.Settings != null)
                    return FogOfPawnMod.Settings;

                var mod = LoadedModManager.GetMod<FogOfPawnMod>();
                return mod?.GetSettings<FogOfPawnSettings>() ?? new FogOfPawnSettings();
            }
        }
    }
} 