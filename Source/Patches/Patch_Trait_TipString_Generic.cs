using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch]
    public static class Patch_Trait_TipString_Generic
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Trait), "TipString");
        }

        public static bool Prefix(Trait __instance, Pawn pawn, ref string __result)
        {
            if (pawn == null) return true;

            var comp = pawn.GetComp<CompPawnFog>();
            if (comp != null && comp.compInitialized && !comp.revealedTraits.Contains(__instance.def))
            {
                __result = "FogOfPawn.UnknownTrait.Tooltip".Translate();
                return false; // suppress vanilla tooltip
            }

            return true; // allow vanilla tooltip
        }
    }
} 