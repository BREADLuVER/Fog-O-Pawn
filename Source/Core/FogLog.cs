#define VERBOSE_LOG
using Verse;

namespace FogOfPawn
{
    public static class FogLog
    {
        public static void Reflect(string key, string message)
        {
#if VERBOSE_LOG
            if (!FogSettingsCache.Current.verboseLogging) return;
            if (!_sentinel.Add(key)) return;
            Log.Message("[FogOfPawn REFLECT] " + message);
#endif
        }

        public static void Verbose(string message)
        {
#if VERBOSE_LOG
            if (!FogSettingsCache.Current.verboseLogging) return;
            Log.Message("[FogOfPawn DEBUG] " + message);
#endif
        }

        public static void Fail(string key, string message)
        {
#if VERBOSE_LOG
            if (!FogSettingsCache.Current.verboseLogging) return;
            if (!_sentinel.Add("FAIL:" + key)) return;
            Log.Warning("[FogOfPawn FAIL] " + message);
#else
            Log.Warning("[FogOfPawn FAIL] " + message);
#endif
        }

        private static readonly System.Collections.Generic.HashSet<string> _sentinel = new();
    }
} 