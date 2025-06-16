#define VERBOSE_LOG
using Verse;

namespace FogOfPawn
{
    /// <summary>
    /// Thin wrapper around RimWorld Log utility that honours developer-mode and an
    /// optional compile-time VERBOSE_LOG symbol so that detailed reflection traces
    /// can be left in production builds without bothering regular players.
    /// </summary>
    public static class FogLog
    {
        /// <summary>
        /// Dev-only informational message printed only once per session based on a string key.
        /// </summary>
        public static void Reflect(string key, string message)
        {
#if VERBOSE_LOG
            if (!Prefs.DevMode) return;
            if (!_sentinel.Add(key)) return;
            Log.Message("[FogOfPawn REFLECT] " + message);
#endif
        }

        /// <summary>
        /// Verbose log gated behind both DevMode and the VERBOSE_LOG symbol.
        /// </summary>
        public static void Verbose(string message)
        {
#if VERBOSE_LOG
            if (!Prefs.DevMode) return;
            Log.Message("[FogOfPawn DEBUG] " + message);
#endif
        }

        /// <summary>
        /// Semi-fatal path – printed once, still allows mod to continue.
        /// </summary>
        public static void Fail(string key, string message)
        {
#if VERBOSE_LOG
            if (!_sentinel.Add("FAIL:" + key)) return;
            Log.Warning("[FogOfPawn FAIL] " + message);
#else
            Log.Warning("[FogOfPawn FAIL] " + message);
#endif
        }

        private static readonly System.Collections.Generic.HashSet<string> _sentinel = new();
    }
} 