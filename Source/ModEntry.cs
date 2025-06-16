using Verse;
using UnityEngine;
using HarmonyLib;

namespace FogOfPawn
{
    [StaticConstructorOnStartup]
    internal static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("FogOfPawn");
            harmony.PatchAll();
            Log.Message("[FogOfPawn] Harmony patches applied.");
        }
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