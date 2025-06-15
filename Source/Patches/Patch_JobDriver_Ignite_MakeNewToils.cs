using HarmonyLib;
using RimWorld;
using Verse;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(JobDriver_Ignite), "MakeNewToils")]
    public static class Patch_JobDriver_Ignite_MakeNewToils
    {
        public static void Postfix(JobDriver_Ignite __instance)
        {
            var pawn = __instance.pawn;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null) return;
            
            // A pawn igniting something is a dead giveaway for a pyromaniac
            var pyroTrait = pawn.story.traits.GetTrait(TraitDefOf.Pyromaniac);
            if (pyroTrait != null)
            {
                comp.RevealTrait(pyroTrait);
            }
        }
    }
} 