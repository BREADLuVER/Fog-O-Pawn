using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace FogOfPawn.Patches
{
    [HarmonyPatch(typeof(JobDriver_Ignite), "MakeNewToils")]
    public static class Patch_JobDriver_Ignite_MakeNewToils
    {
        public static void Postfix(JobDriver_Ignite __instance)
        {
            var pawn = __instance.pawn;
            var comp = pawn.GetComp<CompPawnFog>();
            if (comp == null || !comp.compInitialized) return;
            if (comp.tier == DeceptionTier.Truthful || comp.fullyRevealed) return; 
            
            var pyroTrait = pawn.story.traits.GetTrait(TraitDefOf.Pyromaniac);
            if (pyroTrait != null)
            {
                comp.RevealTrait(pyroTrait);
            }
        }
    }
} 