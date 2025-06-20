using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;

namespace FogOfPawn
{
    [StaticConstructorOnStartup]
    internal static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("FogOfPawn");
	    Harmony.DEBUG = true;
            harmony.PatchAll();
            FogLog.Reflect("HarmonyPatched", "Harmony patches applied.");

            // DebugActionsUtility not available in release API; dev spawning via Sleeper gizmo remains.
        }

        // DevJoiner helper kept for possible future debug builds
        private static void DevJoiner(bool sleeper) { }
    }

    // Empty Mod subclass so we show up in mod settings list (settings added later)
    public class FogOfPawnMod : Mod
    {
        public static FogOfPawnSettings Settings;

        public FogOfPawnMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<FogOfPawnSettings>();
        }

        public override string SettingsCategory() => "Fog of Pawn";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }
    }
} 