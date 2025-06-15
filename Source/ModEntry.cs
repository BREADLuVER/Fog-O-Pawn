using Verse;
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
        public FogOfPawnMod(ModContentPack content) : base(content)
        {
        }

        public override string SettingsCategory() => "Fog of Pawn";
    }
} 