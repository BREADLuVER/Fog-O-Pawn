using HarmonyLib;
using Verse;

namespace FogOfPawn
{
    // RimWorld loads any subclass of Verse.Mod that exists in the mod's assembly.
    public class FogOfPawnMod : Mod
    {
        public const string HarmonyId = "FogOfPawn";

        public FogOfPawnMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll();
            Log.Message("[FogOfPawn] Harmony patches applied");
        }
    }

    // Example of a Harmony patch to prove the mod is active.
    // This will write a log entry when any map is generated (safe operation for now).
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeLoading))]
    internal static class Map_FinalizeLoading_Patch
    {
        private static void Postfix(Map __instance)
        {
            Log.Message($"[FogOfPawn] Map loaded: {__instance} (tile {__instance.Tile})");
        }
    }
} 